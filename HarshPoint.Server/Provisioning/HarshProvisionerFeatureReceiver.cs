﻿using HarshPoint.Provisioning;
using Microsoft.SharePoint;
using System.Collections.ObjectModel;

namespace HarshPoint.Server.Provisioning
{
    public abstract class HarshProvisionerFeatureReceiver : SPFeatureReceiver
    {
        private readonly HarshServerCompositeProvisioner Composite =
            new HarshServerCompositeProvisioner();

        public Collection<HarshProvisionerBase> Provisioners
        {
            get { return Composite.Provisioners; }
        }

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            base.FeatureActivated(properties);

            SetContext(properties);
            Composite.Provision();
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            SetContext(properties);
            Composite.Unprovision();

            base.FeatureDeactivating(properties);
        }

        private void SetContext(SPFeatureReceiverProperties properties)
        {
            if (properties == null)
            {
                throw Error.ArgumentNull("properties");
            }

            if (properties.Feature == null)
            {
                throw Error.ArgumentOutOfRange("properties", SR.HarshProvisionerFeatureReceiver_PropertiesFeatureNull);
            }

            Composite.SetContext(properties.Feature.Parent);
        }
    }
}
