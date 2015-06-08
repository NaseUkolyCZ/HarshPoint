﻿using HarshPoint.Provisioning;
using Moq;
using Moq.Protected;
using System;
using System.Threading.Tasks;
using Xunit;

namespace HarshPoint.Tests.Provisioning
{
    public class HarshCompositeProvisionerTests : IClassFixture<SharePointClientFixture>
    {
        public HarshCompositeProvisionerTests(SharePointClientFixture fixture)
        {
            ClientOM = fixture;
        }

        public SharePointClientFixture ClientOM
        {
            get;
            set;
        }

        [Fact]
        public async Task Calls_children_provision_in_correct_order()
        {
            var seq = String.Empty;

            var p1 = new Mock<HarshProvisioner>();
            var p2 = new Mock<HarshProvisioner>();

            p1.Protected()
                .Setup<Task<HarshProvisionerResult>>("OnProvisioningAsync")
                .Returns(Task.FromResult(new HarshProvisionerResult(p1.Object)))
                .Callback(() => seq += "1");

            p2.Protected()
                .Setup<Task<HarshProvisionerResult>>("OnProvisioningAsync")
                .Returns(Task.FromResult(new HarshProvisionerResult(p2.Object)))
                .Callback(() => seq += "2");

            var ctx = ClientOM.Context.AllowDeleteUserData();

            var composite = new HarshProvisioner()
            {
                Children = { p1.Object, p2.Object }
            };
            await composite.ProvisionAsync(ctx);

            Assert.Equal("12", seq);
        }

        [Fact]
        public async Task Calls_children_unprovision_in_correct_order()
        {
            var seq = String.Empty;

            var p1 = new Mock<HarshProvisioner>();
            var p2 = new Mock<HarshProvisioner>();

            p1.Protected()
                .Setup<Task<HarshProvisionerResult>>("OnUnprovisioningAsync")
                .Returns(Task.FromResult(new HarshProvisionerResult(p1.Object)))
                .Callback(() => seq += "1");

            p2.Protected()
                .Setup<Task<HarshProvisionerResult>>("OnUnprovisioningAsync")
                .Returns(Task.FromResult(new HarshProvisionerResult(p2.Object)))
                .Callback(() => seq += "2");

            var ctx = ClientOM.Context.AllowDeleteUserData();

            var composite = new HarshProvisioner()
            {
                Children = { p1.Object, p2.Object }
            };
            await composite.UnprovisionAsync(ctx);

            Assert.Equal("21", seq);
        }

        [Fact]
        public async Task Assigns_context_to_children()
        {
            var p = new Mock<HarshProvisioner>();
            p.Protected()
                .Setup<Task<HarshProvisionerResult>>("OnProvisioningAsync")
                .Returns(Task.FromResult(new HarshProvisionerResult(p.Object)))
                .Callback(() =>
                {
                    Assert.Equal(ClientOM.Web, p.Object.Web);
                });

            var composite = new HarshProvisioner()
            {
                Children = { p.Object }
            };
            await composite.ProvisionAsync(ClientOM.Context);
        }

        [Fact]
        public async Task Calls_child_Provision_with_modified_context_via_Modifier()
        {
            var composite = new ModifiesChildContextUsingModifier()
            {
                Children = { new ExpectsModifiedContext() }
            };
            await composite.ProvisionAsync(ClientOM.Context);
        }

        [Fact]
        public async Task Calls_child_Unprovision_with_modified_context_via_Modifier()
        {
            var composite = new ModifiesChildContextUsingModifier()
            {
                Children = { new ExpectsModifiedContext() }
            };
            await composite.UnprovisionAsync(ClientOM.Context);
        }
        
        private class ModifiesChildContextUsingModifier : HarshProvisioner
        {
            public ModifiesChildContextUsingModifier()
            {
                ModifyChildrenContextState(() => "1234");
            }
            protected override Task<HarshProvisionerResult> OnProvisioningAsync()
            {
                Assert.Empty(Context.StateStack);
                return base.OnProvisioningAsync();
            }

            [NeverDeletesUserData]
            protected override Task<HarshProvisionerResult> OnUnprovisioningAsync()
            {
                Assert.Empty(Context.StateStack);
                return base.OnUnprovisioningAsync();
            }
        }

        private class ExpectsModifiedContext : HarshProvisioner
        {
            protected override Task<HarshProvisionerResult> OnProvisioningAsync()
            {
                Assert.Single(Context.StateStack, "1234");
                return base.OnProvisioningAsync();
            }

            [NeverDeletesUserData]
            protected override Task<HarshProvisionerResult> OnUnprovisioningAsync()
            {
                Assert.Single(Context.StateStack, "1234");
                return base.OnUnprovisioningAsync();
            }
        }
    }
}
