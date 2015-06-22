﻿using HarshPoint.Provisioning;
using System;
using System.Threading.Tasks;
using Xunit;

namespace HarshPoint.Tests.Provisioning
{
    public class FieldProvisionerTests : IClassFixture<SharePointClientFixture>
    {
        public FieldProvisionerTests(SharePointClientFixture data)
        {
            ClientOM = data;
        }

        public SharePointClientFixture ClientOM
        {
            get;
            set;
        }

        [Fact(Skip = "inconclusive")]
        public async Task Existing_field_is_not_provisioned()
        {
            var prov = new HarshField()
            {
                Id = HarshBuiltInFieldId.Title,
                DisplayName = "Title",
            };

            await prov.ProvisionAsync(ClientOM.Context);
            //Assert.False(prov.Result.ObjectAdded);
        }

        [Fact]
        public async Task Existing_field_DisplayName_gets_changed()
        {
            var id = Guid.NewGuid();
            var internalName = id.ToString("n");
            try
            {
                var prov = new HarshField()
                {
                    Id = id,
                    InternalName = internalName,
                    DisplayName = internalName + "-before"
                };

                await prov.ProvisionAsync(ClientOM.Context);

                ClientOM.ClientContext.Load(prov.Field, f => f.Title);
                await ClientOM.ClientContext.ExecuteQueryAsync();

                Assert.Equal(prov.DisplayName, prov.Field.Title);

                prov = new HarshField()
                {
                    Id = id,
                    InternalName = internalName,
                    DisplayName = internalName + "-after"
                };

                await prov.ProvisionAsync(ClientOM.Context);

                ClientOM.ClientContext.Load(prov.Field, f => f.Title);
                await ClientOM.ClientContext.ExecuteQueryAsync();

                Assert.Equal(prov.DisplayName, prov.Field.Title);
            }
            finally
            {
                ClientOM.Web.Fields.GetById(id).DeleteObject();
                await ClientOM.ClientContext.ExecuteQueryAsync();
            }

        }

        [Fact]
        public void Fails_if_id_missing()
        {
            var prov = new HarshField()
            {
                InternalName = "DummyField"
            };

            Assert.ThrowsAsync<InvalidOperationException>(async () => await prov.SchemaXmlBuilder.Update(null, null));
        }

        [Fact]
        public void Fails_if_InternalName_missing()
        {
            var prov = new HarshField()
            {
                Id = Guid.NewGuid(),
                DisplayName = "Dummy",
            };

            Assert.ThrowsAsync<InvalidOperationException>(async () => await prov.SchemaXmlBuilder.Update(null, null));
        }

        [Fact]
        public async Task Field_id_is_generated_into_schema()
        {
            var fieldId = Guid.NewGuid();
            var prov = new HarshField()
            {
                Id = fieldId,
                InternalName = "DummyField",
                DisplayName = "Dummy",
            };

            var schema = await prov.SchemaXmlBuilder.Update(null, null);

            Assert.NotNull(schema);
            Assert.Equal("Field", schema.Name);
            Assert.Equal(fieldId, new Guid(schema.Attribute("ID").Value));
        }

        [Fact]
        public async Task Field_DisplayName_is_generated_into_schema()
        {
            var prov = new HarshField()
            {
                Id = Guid.NewGuid(),
                InternalName = "DummyField",
                DisplayName = "Dummy",
            };

            var schema = await prov.SchemaXmlBuilder.Update(null, null);

            Assert.NotNull(schema);
            Assert.Equal("Field", schema.Name);
            Assert.Equal("Dummy", schema.Attribute("DisplayName").Value);
        }

        [Fact]
        public async Task Field_InternalName_is_generated_into_schema()
        {
            var prov = new HarshField()
            {
                Id = Guid.NewGuid(),
                InternalName = "DummyField",
                DisplayName = "Dummy",
            };

            var schema = await prov.SchemaXmlBuilder.Update(null, null);

            Assert.NotNull(schema);
            Assert.Equal("Field", schema.Name);
            Assert.Equal("DummyField", schema.Attribute("Name").Value);
        }

        [Fact]
        public async Task Field_no_StaticName_is_generated_into_schema()
        {
            var prov = new HarshField()
            {
                Id = Guid.NewGuid(),
                InternalName = "DummyField",
                DisplayName = "Dummy",
            };

            var schema = await prov.SchemaXmlBuilder.Update(null, null);

            Assert.NotNull(schema);
            Assert.Equal("Field", schema.Name);
            Assert.Equal("DummyField", schema.Attribute("StaticName").Value);
        }

        [Fact]
        public async Task Field_explicit_StaticName_is_generated_into_schema()
        {
            var fieldId = Guid.NewGuid();
            var prov = new HarshField()
            {
                Id = fieldId,
                InternalName = "DummyField",
                DisplayName = "Dummy",
                StaticName = "WhomDoYouCallDummy"
            };

            var schema = await prov.SchemaXmlBuilder.Update(null, null);

            Assert.NotNull(schema);
            Assert.Equal("WhomDoYouCallDummy", schema.Attribute("StaticName").Value);
        }

        [Fact]
        public async Task Field_group_is_generated_into_schema()
        {
            var fieldId = Guid.NewGuid();
            var prov = new HarshField()
            {
                Id = fieldId,
                InternalName = "DummyField",
                DisplayName = "Dummy",
                Group = "GROO GROO GROO"
            };

            var schema = await prov.SchemaXmlBuilder.Update(null, null);

            Assert.NotNull(schema);
            Assert.Equal("GROO GROO GROO", schema.Attribute("Group").Value);
        }

    }
}
