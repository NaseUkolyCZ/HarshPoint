﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HarshPoint.ObjectModel
{
    internal sealed class ParameterSet
    {
        public static readonly String ImplicitParameterSetName = "__DefaultParameterSet";
        public static readonly StringComparer NameComparer = StringComparer.Ordinal;

        public ParameterSet(String name, IEnumerable<Parameter> parameters, Boolean isDefault)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw Logger.Fatal.ArgumentNullOrWhiteSpace(nameof(name));
            }

            if (parameters == null)
            {
                throw Logger.Fatal.ArgumentNull(nameof(parameters));
            }

            Name = name;
            IsDefault = isDefault;
            Parameters = parameters.ToImmutableArray();
        }

        public Boolean IsDefault
        {
            get;

        }

        public String Name
        {
            get;

        }

        public IReadOnlyList<Parameter> Parameters
        {
            get;
        }

        private static readonly HarshLogger Logger = HarshLog.ForContext<ParameterSet>();
    }
}
