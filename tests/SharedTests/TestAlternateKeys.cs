#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;
using System.ServiceModel;

namespace DG.XrmMockupTest
{
    public class TestAlternateKeys : UnitTestBase
    {
        public TestAlternateKeys(XrmMockupFixture fixture) : base(fixture) { }

        [Fact(Skip = "Alternate key implementation should be based on column values")]
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

        public void AltKeyRetrieveWithoutEntityTypeInDb()
        {
            var y = Account.Retrieve_dg_name(orgAdminUIService, "woop", x => x.AccountNumber);
        }


        [Fact]
        public void TestUniqueNameOnInsert()
        {
            var u1 = new Entity("mock_uniquesinglekeyonname");
            u1["mock_name"] = "unique1";
            orgAdminService.Create(u1);
            
            var u2 = new Entity("mock_uniquesinglekeyonname");
            u2["mock_name"] = "unique1";

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Create(u2));
            Assert.Equal("A record that has the attribute values Name already exists. The entity key Name requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }
    }
}
#endif