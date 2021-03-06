﻿using HarshPoint.Provisioning;
using HarshPoint.Provisioning.Implementation;
using Microsoft.SharePoint.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HarshPoint.Server.Provisioning
{
    public static class HarshProvisionerConverter
    {
        public static HarshServerProvisioner ToServerProvisioner(this HarshProvisionerBase provisioner)
        {
            if (provisioner == null)
            {
                throw Logger.Fatal.ArgumentNull(nameof(provisioner));
            }

            var clientProvisioner = (provisioner as HarshProvisioner);
            var serverProvisioner = (provisioner as HarshServerProvisioner);

            if (clientProvisioner != null)
            {
                serverProvisioner = new ClientProvisionerWrapper(clientProvisioner);
            }

            if (serverProvisioner == null)
            {
                throw Logger.Fatal.ArgumentFormat(
                    nameof(provisioner),
                    SR.HarshServerProvisionerConverter_CannotConvert,
                    provisioner.GetType().FullName
                );
            }

            return serverProvisioner;
        }

        private sealed class ClientProvisionerWrapper : HarshServerProvisioner
        {
            public ClientProvisionerWrapper(HarshProvisioner provisioner)
            {
                Provisioner = provisioner;
            }

            private ClientContext ClientContext
            {
                get;
                set;
            }

            private HarshProvisionerContext ProvisionerContext
            {
                get;
                set;
            }

            private HarshProvisioner Provisioner
            {
                get;
            }

            protected override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                if (Web == null)
                {
                    throw Logger.Fatal.InvalidOperation(SR.HarshServerProvisionerConverter_OnlyWebAndSiteSupported);
                }

                ClientContext = new ClientContext(Web.Url);
                ProvisionerContext = new HarshProvisionerContext(ClientContext);
            }

            protected override void Complete()
            {
                ProvisionerContext = null;

                if (ClientContext != null)
                {
                    ClientContext.Dispose();
                    ClientContext = null;
                }

                base.Complete();
            }

            protected override ICollection<HarshProvisionerBase> CreateChildrenCollection()
                => NoChildren;

            protected override Task OnProvisioningAsync()
                => Provisioner.ProvisionAsync(ProvisionerContext);

            protected override Task OnUnprovisioningAsync()
                => Provisioner.UnprovisionAsync(ProvisionerContext);
        }

        private static readonly HarshLogger Logger = HarshLog.ForContext(typeof(HarshProvisionerConverter));
    }
}
