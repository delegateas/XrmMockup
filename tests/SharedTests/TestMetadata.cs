using System;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestMetadata : UnitTestBase
    {
        [TestMethod]
        public void TestRetrieveOptionSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionRetrieved = orgAdminUIService.Execute(new RetrieveOptionSetRequest() { Name = "workflow_stage" }) as RetrieveOptionSetResponse;
                Assert.IsTrue(optionRetrieved.OptionSetMetadata.Name == "workflow_stage");

            }
        }

        [TestMethod]
        public void TestRetrieveAllOptionSets()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionsRetrieved = orgAdminUIService.Execute(new RetrieveAllOptionSetsRequest()) as RetrieveAllOptionSetsResponse;
                Assert.IsTrue(optionsRetrieved.OptionSetMetadata.Any(x => x.Name == "workflow_stage"));

            }
        }

        [TestMethod]
        public void TestSetttingAttributes()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc.Attributes["name"] = "Jon";
                orgAdminUIService.Update(acc);

                try
                {
                    acc.Attributes["illegalName"] = 1;
                    orgAdminUIService.Update(acc);
                    Assert.Fail("FaultException should have been thrown");
                }
                catch (FaultException)
                {
                }
                catch (Exception e)
                {
                    Assert.Fail(
                         string.Format("Unexpected exception of type {0} caught: {1}",
                                        e.GetType(), e.Message)
                    );
                }
            }
        }

        [TestMethod]
        public void TestCRURestrictions()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                acc.Attributes.Add("opendeals_state", 22);
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("opendeals_state")) as Account;
                Assert.AreNotEqual(22, retrieved.OpenDeals_State);

                orgAdminUIService.Update(acc);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("opendeals_state")) as Account;
                Assert.AreNotEqual(22, retrieved.OpenDeals_State);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("isprivate")) as Account;
                Assert.IsFalse(retrieved.Attributes.ContainsKey("isprivate"));

            }
        }


        [TestMethod]
        public void RetrieveEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNotNull(resp);
            Assert.IsNotNull(resp.EntityMetadata);
            Assert.AreEqual(req.LogicalName, resp.EntityMetadata.LogicalName);
        }

        [TestMethod]
        public void RetrieveAllFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.All
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNotNull(resp.EntityMetadata.Privileges);
            Assert.IsNotNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.IsNotNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.IsNotNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.IsNotNull(resp.EntityMetadata.Attributes);
        }


        [TestMethod]
        public void RetrieveAttributesFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Attributes
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNull(resp.EntityMetadata.Privileges);
            Assert.IsNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.IsNotNull(resp.EntityMetadata.Attributes);
        }


        [TestMethod]
        public void RetrievePrivilegesFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Privileges
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNotNull(resp.EntityMetadata.Privileges);
            Assert.IsNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.IsNull(resp.EntityMetadata.Attributes);
        }


        [TestMethod]
        public void RetrieveRelationshipsFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Relationships
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNull(resp.EntityMetadata.Privileges);
            Assert.IsNotNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.IsNotNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.IsNotNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.IsNull(resp.EntityMetadata.Attributes);
        }

        [TestMethod]
        public void RetrieveEntityFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Entity
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNull(resp.EntityMetadata.Privileges);
            Assert.IsNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.IsNull(resp.EntityMetadata.Attributes);
        }

        [TestMethod]
        public void RetrieveDefaultFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Default
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.IsNull(resp.EntityMetadata.Privileges);
            Assert.IsNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.IsNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.IsNull(resp.EntityMetadata.Attributes);
        }

    }

}
