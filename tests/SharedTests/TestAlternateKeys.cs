#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestAlternateKeys : UnitTestBase
    {
        [TestMethod]
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
                var req = new RetrieveRequest {
                    Target = new EntityReference {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                
                var resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var entity = resp.Entity as Account;
                Assert.AreEqual("Burgers", entity.Name);
                Assert.AreEqual("Virum", entity.Address1_City);

                var newAttributes = new AttributeCollection {
                    { "name", "Toast" }
                };
                orgAdminUIService.Update(new Account { KeyAttributes = keyAttributes, Attributes = newAttributes });

                keyAttributes["name"] = "Toast";

                req = new RetrieveRequest {
                    Target = new EntityReference {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var updatedEntity = resp.Entity as Account;
                Assert.AreEqual("Toast", updatedEntity.Name);
                Assert.AreEqual("Virum", updatedEntity.Address1_City);
                
            }
        }

        [TestMethod]

        public void AltKeyRetrieveWithoutEntityTypeInDb() {

            var y = Account.Retrieve_dg_name(orgAdminUIService, "woop", x => x.AccountNumber);
        }
    }

}
#endif