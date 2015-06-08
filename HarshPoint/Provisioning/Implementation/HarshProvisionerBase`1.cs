﻿using Serilog;
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
        private ICollection<HarshProvisionerBase> _children;
        private ICollection<Func<Object>> _childrenContextStateModifiers;
        private HarshProvisionerMetadata _metadata;

        protected HarshProvisionerBase()
        {
            Logger = Log.ForContext(GetType());
        }

        public TContext Context
        {
            get;
            private set;
        }

        public ICollection<HarshProvisionerBase> Children
            => HarshLazy.Initialize(ref _children, CreateChildrenCollection);

        public ILogger Logger
        {
            get;
            private set;
        }

        public Boolean MayDeleteUserData
        {
            get;
            set;
        }

        internal HarshProvisionerMetadata Metadata
            => HarshLazy.Initialize(ref _metadata, () => new HarshProvisionerMetadata(GetType()));

        internal Boolean HasChildren
        {
            get
            {
                if (_children == null || _children.IsReadOnly)
                {
                    return false;
                }

                return _children.Any();
            }
        }

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

        public Task<HarshProvisionerResult> ProvisionAsync(TContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull(nameof(context));
            }

            return RunWithContext(OnProvisioningAsync, context);
        }

        public Task<HarshProvisionerResult> UnprovisionAsync(TContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull(nameof(context));
            }

            if (MayDeleteUserData || context.MayDeleteUserData || !Metadata.UnprovisionDeletesUserData)
            {
                return RunWithContext(OnUnprovisioningAsync, context);
            }

            return Task.FromResult<HarshProvisionerResult>(
                new HarshProvisionerResultNotRun(this)
            );
        }

        protected virtual Task InitializeAsync()
        {
            return HarshTask.Completed;
        }

        protected virtual void Complete()
        {
        }

        protected virtual async Task<HarshProvisionerResult> OnProvisioningAsync()
        {
            return new HarshProvisionerResult(
                this,
                await ProvisionChildrenAsync()
            );
        }

        [NeverDeletesUserData]
        protected virtual async Task<HarshProvisionerResult> OnUnprovisioningAsync()
        {
            return new HarshProvisionerResult(
                this,
                await UnprovisionChildrenAsync()
            );
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected Task<IEnumerable<HarshProvisionerResult>> ProvisionChildrenAsync()
        {
            return RunChildren(ProvisionChild);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected Task<IEnumerable<HarshProvisionerResult>> UnprovisionChildrenAsync()
        {
            return RunChildren(UnprovisionChild, reverse: true);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected Task<IEnumerable<T>> ResolveAsync<T>(IResolve<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.ResolveAsync(Context);
        }

        protected Task<T> ResolveSingleOrDefaultAsync<T>(IResolveSingle<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.ResolveSingleOrDefaultAsync(Context);
        }

        protected Task<T> ResolveSingleAsync<T>(IResolveSingle<T> resolver)
        {
            if (resolver == null)
            {
                throw Error.ArgumentNull(nameof(resolver));
            }

            return resolver.ResolveSingleAsync(Context);
        }

        protected virtual ICollection<HarshProvisionerBase> CreateChildrenCollection()
        {
            return new Collection<HarshProvisionerBase>();
        }

        internal abstract Task<HarshProvisionerResult> ProvisionChild(HarshProvisionerBase provisioner, TContext context);

        internal abstract Task<HarshProvisionerResult> UnprovisionChild(HarshProvisionerBase provisioner, TContext context);

        private void InitializeDefaultFromContextProperties()
        {
            var properties = Metadata
                .DefaultFromContextProperties
                .Where(p => p.Getter(this) == null);

            foreach (var p in properties)
            {
                Object value = null;

                if (p.TagType != null)
                {
                    var tag = Context
                        .GetState(p.TagType)
                        .FirstOrDefault();

                    value = (tag as IDefaultFromContextTag)?.Value;
                }
                else if (p.ResolvedType != null)
                {
                    value = Activator.CreateInstance(
                        typeof(ContextStateResolver<>).MakeGenericType(p.ResolvedType)
                    );
                }
                else
                {
                    value = Context.GetState(p.PropertyType).FirstOrDefault();
                }

                if (value != null)
                {
                    p.Setter(this, value);
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

        private Task<IEnumerable<HarshProvisionerResult>> RunChildren(
            Func<HarshProvisionerBase, TContext, Task<HarshProvisionerResult>> action,
            Boolean reverse = false)
        {
            if (!HasChildren)
            {
                return NoResults;
            }

            var children = reverse ? _children.Reverse() : _children;
            var context = PrepareChildrenContext();

            return children.SelectSequentially(
                child => action(child, context)
            );
        }

        private async Task<HarshProvisionerResult> RunWithContext(Func<Task<HarshProvisionerResult>> action, TContext context)
        {
            if (action == null)
            {
                throw Error.ArgumentNull(nameof(action));
            }

            if (context == null)
            {
                throw Error.ArgumentNull(nameof(context));
            }

            Context = context;

            try
            {
                try
                {
                    InitializeDefaultFromContextProperties();

                    await InitializeAsync();
                    return await action();
                }
                finally
                {
                    Complete();
                }
            }
            finally
            {
                Context = null;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly ICollection<HarshProvisionerBase> NoChildren =
            ImmutableList<HarshProvisionerBase>.Empty;

        private static readonly Task<IEnumerable<HarshProvisionerResult>> NoResults =
            Task.FromResult<IEnumerable<HarshProvisionerResult>>(
                ImmutableList<HarshProvisionerResult>.Empty
            );
    }
}
