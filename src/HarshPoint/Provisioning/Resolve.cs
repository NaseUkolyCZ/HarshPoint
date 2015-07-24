﻿using HarshPoint.Provisioning.Implementation;
using HarshPoint.Provisioning.Resolvers;
using Microsoft.SharePoint.Client;
using System;

namespace HarshPoint.Provisioning
{
    public static class Resolve
    {
        public static ResolveContentType ContentType => new ResolveContentType();
        public static ResolveField Field = new ResolveField();
        public static ResolveList List => new ResolveList();

        public static ResolveCatalog Catalog(params ListTemplateType[] templateTypes)
        {
            if (templateTypes == null)
            {
                throw Error.ArgumentNull(nameof(templateTypes));
            }

            return new ResolveCatalog(templateTypes);
        }

        public static OldResolveFieldById FieldById(params Guid[] ids)
        {
            if (ids == null)
            {
                throw Error.ArgumentNull(nameof(ids));
            }

            return new OldResolveFieldById(ids);
        }

        public static OldResolveFieldByInternalName FieldByInternalName(params String[] names)
        {
            if (names == null)
            {
                throw Error.ArgumentNull(nameof(names));
            }

            return new OldResolveFieldByInternalName(names);
        }

        [Obsolete]
        public static OldResolveListByUrl ListByUrlOld(params String[] urls)
        {
            if (urls == null)
            {
                throw Error.ArgumentNull(nameof(urls));
            }

            return new OldResolveListByUrl(urls);
        }

        public static ResolveTermStoreKeywordsDefault TermStoreKeywordsDefault()
        {
            return new ResolveTermStoreKeywordsDefault();
        }

        public static ResolveTermStoreSiteCollectionDefault TermStoreSiteCollectionDefault()
        {
            return new ResolveTermStoreSiteCollectionDefault();
        }

        public static ResolvedResolver<T> Value<T>(params T[] values)
        {
            return new ResolvedResolver<T>(values);
        }
    }
}
