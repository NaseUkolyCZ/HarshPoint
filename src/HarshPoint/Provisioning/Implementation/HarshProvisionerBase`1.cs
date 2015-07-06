﻿using Serilog.Core.Enrichers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace HarshPoint.Provisioning.Implementation
{
    /// <summary>
    /// Provides common initialization and completion logic for 
    /// classes provisioning SharePoint artifacts.
    /// </summary>
    public abstract class HarshProvisionerBase<TContext> : HarshProvisionerBase
        where TContext : HarshProvisionerContextBase
    {
        private readonly HarshScopedValue<TContext> _context;
        private readonly HarshScopedValue<HarshLogger> _logger;
        private readonly HarshScopedValue<ParameterSet> _parameterSet;

        private ICollection<HarshProvisionerBase> _children;
        private ICollection<Func<Object>> _childrenContextStateModifiers;
        private HarshProvisionerMetadata _metadata;

        protected HarshProvisionerBase()
        {
            _context = new HarshScopedValue<TContext>();
            _parameterSet = new HarshScopedValue<ParameterSet>();

            _logger = new HarshScopedValue<HarshLogger>(
                HarshLog.ForContext(GetType())
            );
        }

        public TContext Context
            => _context.Value;

        public ICollection<HarshProvisionerBase> Children
            => HarshLazy.Initialize(ref _children, CreateChildrenCollection);

        public HarshLogger Logger
            => _logger.Value;

        public Boolean MayDeleteUserData
        {
            get;
            set;
        }

        protected String ParameterSetName => ParameterSet?.Name;

        internal Boolean HasChildren
            => (_children != null) && _children.Any();

        internal ParameterSet ParameterSet
            => _parameterSet.Value;

        internal HarshProvisionerMetadata Metadata
            => HarshLazy.Initialize(ref _metadata, () => new HarshProvisionerMetadata(GetType()));

        public void ModifyChildrenContextState(Func<Object> modifier)
        {
            if (modifier == null)
            {
                throw Error.ArgumentNull(nameof(modifier));
            }

            if (_childrenContextStateModifiers == null)
            {
                _childrenContextStateModifiers = new Collection<Func<Object>>();
            }

            _childrenContextStateModifiers.Add(modifier);
        }

        public void ModifyChildrenContextState(Object state)
        {
            if (state == null)
            {
                throw Error.ArgumentNull(nameof(state));
            }

            ModifyChildrenContextState(() => state);
        }

        public Task ProvisionAsync(TContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull(nameof(context));
            }

            return RunSelfAndChildren(
                context,
                OnProvisioningAsync,
                ProvisionChildrenAsync
            );
        }

        public async Task UnprovisionAsync(TContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull(nameof(context));
            }

            if (MayDeleteUserData || context.MayDeleteUserData || !Metadata.UnprovisionDeletesUserData)
            {
                await RunSelfAndChildren(
                    context,
                    OnUnprovisioningAsync,
                    UnprovisionChildrenAsync
                );
            }
        }

        protected virtual Task InitializeAsync()
        {
            return HarshTask.Completed;
        }

        protected virtual void Complete()
        {
        }

        protected virtual Task OnProvisioningAsync()
        {
            return HarshTask.Completed;
        }

        protected virtual void OnValidating()
        {
        }

        [NeverDeletesUserData]
        protected virtual Task OnUnprovisioningAsync()
        {
            return HarshTask.Completed;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected Task<IEnumerable<T>> TryResolveAsync<T>(IResolve<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.TryResolveAsync(
                PrepareResolveContext()
            );
        }

        protected Task<T> TryResolveSingleAsync<T>(IResolve<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.TryResolveSingleAsync(
                PrepareResolveContext()
            );
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected Task<IEnumerable<T>> ResolveAsync<T>(IResolve<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.ResolveAsync(
                PrepareResolveContext()
            );
        }

        protected Task<T> ResolveSingleAsync<T>(IResolve<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.ResolveSingleAsync(
                PrepareResolveContext()
            );
        }

        protected virtual ICollection<HarshProvisionerBase> CreateChildrenCollection()
        {
            return new Collection<HarshProvisionerBase>();
        }

        internal virtual ResolveContext<TContext> CreateResolveContext()
        {
            return new ResolveContext<TContext>();
        }

        internal abstract Task ProvisionChild(HarshProvisionerBase provisioner, TContext context);

        internal abstract Task UnprovisionChild(HarshProvisionerBase provisioner, TContext context);

        private void InitializeDefaultFromContext()
        {
            var parameters = Metadata.Parameters.Where(p => p.IsDefaultFromContext);

            foreach (var param in parameters)
            {
                if (!param.HasDefaultValue(this))
                {
                    Logger.Debug(
                        "{Method}: Parameter {Parameter} already has non-default value, skipping",
                        nameof(InitializeDefaultFromContext),
                        param
                    );

                    continue;
                }

                var value = GetValueFromContext(param);

                if (value != null)
                {
                    Logger.Debug(
                        "{Method}: Setting parameter {Parameter} to {Value}",
                        nameof(InitializeDefaultFromContext),
                        param,
                        value
                    );

                    param.Setter(this, value);
                }
                else
                {
                    Logger.Debug(
                        "{Method}: Got null from context for parameter {Parameter}, skipping",
                        nameof(InitializeDefaultFromContext),
                        param
                    );
                }
            }
        }

        private Object GetValueFromContext(Parameter param)
        {
            if (param.DefaultFromContext.TagType != null)
            {
                Logger.Debug(
                    "Parameter {Parameter} gets default value form context by the tag type {TagType}",
                    param,
                    param.DefaultFromContext.TagType
                );

                return Context
                    .GetState(param.DefaultFromContext.TagType)
                    .Cast<IDefaultFromContextTag>()
                    .FirstOrDefault()?
                    .Value;
            }

            if (param.DefaultFromContext.ResolvedType != null)
            {
                Logger.Debug(
                    "Parameter {Parameter} resolves objects of type {ResolvedType}",
                    param,
                    param.DefaultFromContext.ResolvedType
                );

                return ContextStateResolver.Create(
                    param.DefaultFromContext.ResolvedType
                );
            }

            Logger.Debug(
                "Parameter {Parameter} gets default value from context directly by its own type {ParameterType}",
                param,
                param.PropertyType
            );

            return Context
                .GetState(param.PropertyType)
                .FirstOrDefault();
        }

        private ParameterSet ResolveParameterSet()
        {
            var resolver = new ParameterSetResolver(this, Metadata.ParameterSets);
            return resolver.Resolve();
        }

        private void ValidateParameters()
        {
            foreach (var parameter in ParameterSet.Parameters)
            {
                if (parameter.IsMandatory && parameter.HasDefaultValue(this))
                {
                    throw Logger.Error.ParameterValidationFormat(
                        parameter,
                        SR.HarshProvisionerBase_ParameterMandatory,
                        parameter
                    );
                }

                foreach (var attr in parameter.ValidationAttributes)
                {
                    attr.Validate(parameter, parameter.Getter(this));
                }
            }
        }

        private TContext PrepareChildrenContext()
        {
            if (_childrenContextStateModifiers == null)
            {
                return Context;
            }

            return _childrenContextStateModifiers
                .Select(fn => fn())
                .Where(state => state != null)
                .Aggregate(
                    Context, (ctx, state) => (TContext)ctx.PushState(state)
                );
        }

        private ResolveContext<TContext> PrepareResolveContext()
        {
            var resolveContext = CreateResolveContext();
            resolveContext.ProvisionerContext = Context;
            return resolveContext;
        }

        private Task ProvisionChildrenAsync()
        {
            return RunChildren(ProvisionChild);
        }

        private Task UnprovisionChildrenAsync()
        {
            return RunChildren(UnprovisionChild, reverse: true);
        }

        private async Task RunChildren(
            Func<HarshProvisionerBase, TContext, Task> action,
            Boolean reverse = false)
        {
            if (!HasChildren)
            {
                return;
            }

            var children = reverse ? _children.Reverse() : _children;
            var context = PrepareChildrenContext();

            foreach (var child in children)
            {
                await action(child, context);
            }
        }

        private async Task RunWithContext(TContext context, Func<Task> action)
        {
            var contextLogger = Logger.ForContext(
                new PropertyEnricher("ProvisionerContext", context),
                new PropertyEnricher("MayDeleteUserData", MayDeleteUserData)
            );

            using (_context.Enter(context))
            using (_logger.Enter(contextLogger))
            {
                await action();
            }
        }

        private Task RunSelfAndChildren(
            TContext context,
            Func<Task> action,
            Func<Task> childAction)
        {
            return RunWithContext(context, async delegate
            {
                // parameter set resolving depends on values
                // from context being already set

                InitializeDefaultFromContext();

                using (_parameterSet.Enter(ResolveParameterSet()))
                {
                    try
                    {
                        ValidateParameters();
                        OnValidating();

                        await InitializeAsync();
                        await action();
                    }
                    finally
                    {
                        Complete();
                    }
                }

                await childAction();
            });
        }
        
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly ICollection<HarshProvisionerBase> NoChildren =
            ImmutableArray<HarshProvisionerBase>.Empty;
    }
}
