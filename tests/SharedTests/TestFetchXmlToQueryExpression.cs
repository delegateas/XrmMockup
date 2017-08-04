using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{

    [TestClass]
    public class TestFetchXmlToQueryExpression : UnitTestBase
    {
        string accountName;
        Guid _contact1Id;
        Guid _contact2Id;
        Guid _opportunity1Id;
        Guid _opportunity2Id;

        [TestInitialize]
        public void TestInitialize() {
            accountName = "Litware, Inc.";
            // Create an account.
            var acc = new Account {
                Name = accountName,
                Address1_StateOrProvince = "Colorado"
            };
            acc.Id = orgAdminUIService.Create(acc);

            // Create the two contacts.
            _contact1Id = orgAdminUIService.Create(
                new Contact() {
                    FirstName = "Ben",
                    LastName = "Andrews",
                    EMailAddress1 = "sample@example.com",
                    Address1_City = "Redmond",
                    Address1_StateOrProvince = "WA",
                    Address1_Telephone1 = "(206)555-5555",
                    ParentCustomerId = acc.ToEntityReference()
                });


            _contact2Id = orgAdminUIService.Create(
                new Contact() {
                    FirstName = "Colin",
                    LastName = "Wilcox",
                    EMailAddress1 = "sample@example.com",
                    Address1_City = "Bellevue",
                    Address1_StateOrProvince = "WA",
                    Address1_Telephone1 = "(425)555-5555",
                    ParentCustomerId = acc.ToEntityReference()
                });

            // Create two opportunities.
            _opportunity1Id = orgAdminUIService.Create(
                new Opportunity() {
                    Name = "Litware, Inc. Opportunity 1",
                    EstimatedCloseDate = DateTime.Now.AddMonths(6),
                    CustomerId = acc.ToEntityReference()
                });

            _opportunity2Id = orgAdminUIService.Create(
                new Opportunity() {
                    Name = "Litware, Inc. Opportunity 2",
                    EstimatedCloseDate = DateTime.Now.AddYears(4),
                    CustomerId = acc.ToEntityReference()
                });
        }


        [TestMethod]
        public void TestFetchXmlToQueryExpressionFromXml()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                // Create a Fetch query that we will convert into a query expression.
                var fetchXml =
                    @"<fetch mapping='logical' version='1.0'>
                    <entity name='opportunity'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='estimatedclosedate' operator='next-x-years' value='3' />
                        </filter>
                        <link-entity name='account' from='accountid' to='customerid'>
                            <link-entity name='contact' from='parentcustomerid' to='accountid'>
                                <attribute name='firstname' />
                                <filter>
                                    <condition attribute='address1_city' operator='eq' value='Bellevue' />
                                    <condition attribute='address1_stateorprovince' operator='eq' value='WA' />
                                </filter>
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";


                var conversionResponse = (FetchXmlToQueryExpressionResponse) orgAdminUIService.Execute(
                    new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml });

                // Use the newly converted query expression to make a retrieve multiple
                // request to Microsoft Dynamics CRM.
                QueryExpression queryExpression = conversionResponse.Query;

                EntityCollection result = orgAdminUIService.RetrieveMultiple(queryExpression);


                Assert.AreEqual(1, result.Entities.Count);
                var entity = result.Entities.First();

                Assert.IsTrue(entity.Attributes.ContainsKey("opportunityid"));
                Assert.AreEqual(_opportunity1Id, entity.Attributes["opportunityid"]);

                Assert.IsTrue(entity.Attributes.ContainsKey("name"));
                Assert.AreEqual("Litware, Inc. Opportunity 1", entity.Attributes["name"]);

                Assert.IsTrue(entity.Attributes.ContainsKey("contact_1.firstname"));
                Assert.IsTrue(entity.Attributes["contact_1.firstname"] is AliasedValue);
                Assert.AreEqual("Colin",  entity.GetAttributeValue<AliasedValue>("contact_1.firstname").Value);

            }
        }

        [TestMethod]
        public void TestFetchXmlToQueryExpressionFromExpr() {
            using(var context = new Xrm(orgAdminUIService)) {
                // Create a Fetch Expression
                var fetchXml =
                    @"<fetch mapping='logical' version='1.0'>
                    <entity name='opportunity'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='estimatedclosedate' operator='next-x-years' value='3' />
                        </filter>
                        <link-entity name='account' from='accountid' to='customerid'>
                            <link-entity name='contact' from='parentcustomerid' to='accountid'>
                                <attribute name='firstname' />
                                <filter>
                                    <condition attribute='address1_city' operator='eq' value='Bellevue' />
                                    <condition attribute='address1_stateorprovince' operator='eq' value='WA' />
                                </filter>
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

                var fetchExpr = new FetchExpression(fetchXml);

                EntityCollection result = orgAdminUIService.RetrieveMultiple(fetchExpr);


                Assert.AreEqual(1, result.Entities.Count);
                var entity = result.Entities.First();

                Assert.IsTrue(entity.Attributes.ContainsKey("opportunityid"));
                Assert.AreEqual(_opportunity1Id, entity.Attributes["opportunityid"]);

                Assert.IsTrue(entity.Attributes.ContainsKey("name"));
                Assert.AreEqual("Litware, Inc. Opportunity 1", entity.Attributes["name"]);

                Assert.IsTrue(entity.Attributes.ContainsKey("contact_1.firstname"));
                Assert.IsTrue(entity.Attributes["contact_1.firstname"] is AliasedValue);
                Assert.AreEqual("Colin", entity.GetAttributeValue<AliasedValue>("contact_1.firstname").Value);
            }
        }

    }

}
