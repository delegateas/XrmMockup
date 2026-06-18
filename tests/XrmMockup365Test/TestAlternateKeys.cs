using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAlternateKeys : UnitTestBase
    {
        public TestAlternateKeys(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAlternateKeysAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var attributes = new AttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                orgAdminUIService.Create(new Account { Attributes = attributes });

                var keyAttributes = new KeyAttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                var req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };

                var resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var entity = resp.Entity as Account;
                Assert.Equal("Burgers", entity.Name);
                Assert.Equal("Virum", entity.Address1_City);

                var newAttributes = new AttributeCollection {
                    { "name", "Toast" }
                };
                orgAdminUIService.Update(new Account { KeyAttributes = keyAttributes, Attributes = newAttributes });

                keyAttributes["name"] = "Toast";

                req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var updatedEntity = resp.Entity as Account;
                Assert.Equal("Toast", updatedEntity.Name);
                Assert.Equal("Virum", updatedEntity.Address1_City);
            }
        }

        [Fact]
        public void TestAlternateKeysUpdateOnly()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var attributes = new AttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                orgAdminUIService.Create(new Account { Attributes = attributes });

                var keyAttributes = new KeyAttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                var req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };

                var resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                
                var newAttributes = new AttributeCollection {
                    { "address1_line1", "Some street" }
                };
                orgAdminUIService.Update(new Account { KeyAttributes = keyAttributes, Attributes = newAttributes });

                req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var updatedEntity = resp.Entity as Account;
                Assert.Equal("Some street", updatedEntity.Address1_Line1);
            }
        }

        // Migrated from Account.Retrieve_dg_name -> ctx_parent.Retrieve_ctx_NameKey. The provisioner
        // creates the ctx_NameKey alternate key (on ctx_name), so XrmContext generates this
        // retrieve-by-key helper. Verifies a record can be retrieved via its alternate key.
        [Fact]
        public void AltKeyRetrieveWithoutEntityTypeInDb()
        {
            var created = new ctx_parent { ctx_Name = "woop" };
            created.Id = orgAdminUIService.Create(created);

            var y = ctx_parent.Retrieve_ctx_NameKey(orgAdminUIService, "woop", x => x.ctx_Name);
            Assert.NotNull(y);
            Assert.Equal(created.Id, y.Id);
            Assert.Equal("woop", y.ctx_Name);
        }
    }
}
