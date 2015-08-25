using HarshPoint.Provisioning.Implementation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SMA = System.Management.Automation;
using HarshPoint.ShellployGenerator.CodeGen;

namespace HarshPoint.ShellployGenerator.Builders
{
    public abstract class NewProvisionerCommandBuilder : NewObjectCommandBuilder
    {
        protected NewProvisionerCommandBuilder(
            HarshProvisionerMetadata metadata
        )
            : base(metadata)
        {
            BaseTypes.Remove(typeof(SMA.PSCmdlet).FullName);
            BaseTypes.Add(HarshProvisionerCmdlet);
        }

        protected sealed override IEnumerable<PropertyModel> CreateProperties()
            => CreatePropertiesRecursively().SelectMany(g => g);

        protected internal override void ValidatePropertyName(String name)
        {
            base.ValidatePropertyName(name);

            if (InputObjectName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                throw Logger.Fatal.ArgumentFormat(
                    nameof(name),
                    SR.CommandBuilder_ReservedName,
                    name
                );
            }
        }

        protected virtual IEnumerable<PropertyModel> CreatePropertiesLocal()
        {
            var properties = base.CreateProperties();

            properties = IgnoreUnfixedParameterSets.Visit(properties);
            properties = RemoveIgnoredUnsynthesized.Visit(properties);
            properties = SetValueFromPipelineByPropertyName.Visit(properties);
            properties = BoolToSwitchVisitor.Visit(properties);

            if (HasInputObject)
            {
                properties = properties.Concat(
                    InputObjectProperty
                );
            }

            return properties;
        }

        public virtual NewProvisionerCommandModel ToNewProvisionerCommand()
            => new NewProvisionerCommandModel(
                ToCommand(),
                CreatePropertiesRecursively().Select(
                    g => g.Key.ToNewObjectCommand(g)
                )
            );

        public override CommandCodeGenerator ToCodeGenerator()
        {
            var newProvCodeGen = new NewProvisionerCommandCodeGenerator(
                ToNewProvisionerCommand()
            );

            return newProvCodeGen.ToCodeGenerator();
        }

        internal IChildProvisionerCommandBuilder ChildBuilder { get; set; }

        private IEnumerable<IGrouping<NewProvisionerCommandBuilder, PropertyModel>> CreatePropertiesRecursively()
        {
            foreach (var child in ChildBuilders)
            {
                var parentProperties = ChildBuilder.PropertyContainer.ApplyTo(
                    ChildBuilder.ParentBuilder.CreatePropertiesLocal()
                );

                // need to call this here again to apply any fixed values 
                // specified by ChildBuilder.

                parentProperties = IgnoreUnfixedParameterSets.Visit(
                    parentProperties
                );

                // remove any ignored properties right away, don't want them
                // to get processed by any children

                parentProperties = RemoveIgnoredUnsynthesized.Visit(
                    parentProperties
                );

                yield return HarshGrouping.Create(
                    child.ParentBuilder,
                    parentProperties
                );
            }

            yield return HarshGrouping.Create(
                this,
                CreatePropertiesLocal()
            );
        }

        private IEnumerable<IChildProvisionerCommandBuilder> ChildBuilders
        {
            get
            {
                var child = ChildBuilder;

                while (child != null)
                {
                    yield return child;
                    child = child.ParentBuilder.ChildBuilder;
                }
            }
        }

        private static readonly ChangePropertyTypeVisitor BoolToSwitchVisitor =
            new ChangePropertyTypeVisitor(
                typeof(Boolean),
                typeof(SMA.SwitchParameter)
            );

        private static readonly PropertyModelVisitor IgnoreUnfixedParameterSets
            = new IgnoreUnfixedParameterSetPropertiesVisitor();

        private static readonly PropertyModelVisitor SetValueFromPipelineByPropertyName
            = new AttributeNamedArgumentVisitor(
                typeof(SMA.ParameterAttribute),
                "ValueFromPipelineByPropertyName",
                true
            );

        private static readonly AttributeModel ValueFromPipeline
            = new AttributeModel(typeof(SMA.ParameterAttribute))
                .SetProperty("ValueFromPipeline", true);

        private static readonly PropertyModel InputObjectProperty
            = new PropertyModelPositional(
                Int32.MaxValue,
                new PropertyModelInputObject(
                    new PropertyModelSynthesized(
                        InputObjectName,
                        typeof(Object),
                        ValueFromPipeline
                    )
                )
            );

        private static readonly HarshLogger Logger
            = HarshLog.ForContext(typeof(NewProvisionerCommandBuilder));

        internal const String InputObjectName = "InputObject";

        private const String HarshProvisionerCmdlet = "HarshProvisionerCmdlet";
    }
}