﻿using Microsoft.SharePoint.Client;
using System.Collections.Generic;

namespace HarshPoint.Provisioning.Resolvers
{
    public abstract class ResolveList<TIdentifier, TSelf>
        : Implementation.Resolvable<List, TIdentifier, HarshProvisionerContext, TSelf>
        where TSelf : ResolveList<TIdentifier, TSelf>
    {
        protected ResolveList(IEnumerable<TIdentifier> identifiers)
            : base(identifiers)
        {
        }

        protected ResolveList(IEnumerable<TIdentifier> identifiers, IEqualityComparer<TIdentifier> idComparer)
            : base(identifiers, idComparer)
        {
        }
    }
}
