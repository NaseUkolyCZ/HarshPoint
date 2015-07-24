﻿using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;

namespace HarshPoint.Provisioning.Implementation
{
    public class ResolveListByUrl : IdentifierResolveBuilder<List, ClientObjectResolveContext, String>
    {
        public ResolveListByUrl(
            IResolveBuilder<List, ClientObjectResolveContext> parent,
            IEnumerable<String> urls
        )
            : base(parent, urls, StringComparer.OrdinalIgnoreCase)
        {
        }

        protected override void InitializeContextBeforeParent(ClientObjectResolveContext context)
        {
            if (context == null)
            {
                throw Logger.Fatal.ArgumentNull(nameof(context));
            }

            context.Include<List>(
                list => list.ParentWebUrl,
                list => list.RootFolder.ServerRelativeUrl
            );
        }

        protected override String GetIdentifier(List result)
            => HarshUrl.GetRelativeTo(result.RootFolder.ServerRelativeUrl, result.ParentWebUrl);

        private static readonly HarshLogger Logger = HarshLog.ForContext<ResolveListByUrl>();
    }
}
