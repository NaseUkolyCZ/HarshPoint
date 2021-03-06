﻿using System;

namespace HarshPoint.ObjectModel
{
    public sealed class ObjectMappingAction
    {
        public ObjectMappingAction(
            PropertyAccessor targetAccessor,
            Object sourceValue
        )
        {
            if (targetAccessor == null)
            {
                throw Logger.Fatal.ArgumentNull(nameof(targetAccessor));
            }

            SourceValue = sourceValue;
            TargetAccessor = targetAccessor;
        }

        public ObjectMappingAction(
            PropertyAccessor targetAccessor,
            Object sourceValue,
            Object targetValue
        )
            : this(targetAccessor, sourceValue)
        {
            TargetValue = targetValue;
            ValuesEqual = Equals(SourceValue, TargetValue);
        }

        public Object SourceValue { get; }
        public PropertyAccessor TargetAccessor { get; }
        public Object TargetValue { get; }
        public Boolean ValuesEqual { get; }

        private static readonly HarshLogger Logger
            = HarshLog.ForContext(typeof(ObjectMappingAction));
    }
}
