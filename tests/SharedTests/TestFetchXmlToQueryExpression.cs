using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestFetchXmlToQueryExpression : UnitTestBase
    {
        private const string accountName = "Litware, Inc.";
        private const string accountAddress1Stateorprovince = "Colorado";
        Guid _contact1Id;
        Guid _contact2Id;
        Guid _opportunity1Id;
        Guid _opportunity2Id;

        public TestFetchXmlToQueryExpression(XrmMockupFixture fixture) : base(fixture)
        {
            // Create an account.
            var acc = new Account
            {
                Name = accountName,
                Address1_StateOrProvince = accountAddress1Stateorprovince
            };
            acc.Id = orgAdminUIService.Create(acc);

            // Create the two contacts.
            _contact1Id = orgAdminUIService.Create(
                new Contact()
                {
                    FirstName = "Ben",
                    LastName = "Andrews",
                    EMailAddress1 = "sample@example.com",
                    Address1_City = "Redmond",
                    Address1_StateOrProvince = "WA",
                    Address1_Telephone1 = "(206)555-5555",
                    ParentCustomerId = acc.ToEntityReference()
                });

            _contact2Id = orgAdminUIService.Create(
                new Contact()
                {
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
                new Opportunity()
                {
                    Name = "Litware, Inc. Opportunity 1",
                    EstimatedCloseDate = DateTime.Now.AddMonths(6),
                    CustomerId = acc.ToEntityReference()
                });

            _opportunity2Id = orgAdminUIService.Create(
                new Opportunity()
                {
                    Name = "Litware, Inc. Opportunity 2",
                    EstimatedCloseDate = DateTime.Now.AddYears(4),
                    CustomerId = acc.ToEntityReference()
                });
        }

        [Fact]
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
                            <condition attribute='estimatedclosedate' operator='gt' value='2000-01-01 00:00:00' />
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

                var conversionResponse = (FetchXmlToQueryExpressionResponse)orgAdminUIService.Execute(
                    new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml });

                // Use the newly converted query expression to make a retrieve multiple
                // request to Microsoft Dynamics CRM.
                QueryExpression queryExpression = conversionResponse.Query;

                EntityCollection result = orgAdminUIService.RetrieveMultiple(queryExpression);

                Assert.Single(result.Entities);
                var entity = result.Entities.First();

                Assert.True(entity.Attributes.ContainsKey("opportunityid"));
                Assert.Equal(_opportunity1Id, entity.Attributes["opportunityid"]);

                Assert.True(entity.Attributes.ContainsKey("name"));
                Assert.Equal("Litware, Inc. Opportunity 1", entity.Attributes["name"]);

                Assert.True(entity.Attributes.ContainsKey("contact.firstname"));
                Assert.True(entity.Attributes["contact.firstname"] is AliasedValue);
                Assert.Equal("Colin", entity.GetAttributeValue<AliasedValue>("contact.firstname").Value);
            }
        }

        [Fact]
        public void TestFetchXmlToQueryExpressionFromExpr()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Create a Fetch Expression
                var fetchXml =
                    @"<fetch mapping='logical' version='1.0'>
                    <entity name='opportunity'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='estimatedclosedate' operator='next-x-years' value='3' />
                            <condition attribute='estimatedclosedate' operator='gt' value='2000-01-01 00:00:00' />
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

                Assert.Single(result.Entities);
                var entity = result.Entities.First();

                Assert.True(entity.Attributes.ContainsKey("opportunityid"));
                Assert.Equal(_opportunity1Id, entity.Attributes["opportunityid"]);

                Assert.True(entity.Attributes.ContainsKey("name"));
                Assert.Equal("Litware, Inc. Opportunity 1", entity.Attributes["name"]);

                Assert.True(entity.Attributes.ContainsKey("contact.firstname"));
                Assert.True(entity.Attributes["contact.firstname"] is AliasedValue);
                Assert.Equal("Colin", entity.GetAttributeValue<AliasedValue>("contact.firstname").Value);
            }
        }

        [Fact]
        public void TestFetchXmlParallelLinkEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXml =
                    @"<fetch>
                    <entity name='opportunity'>
                        <link-entity name='account' from='accountid' to='customerid'>
                        </link-entity>
                    </entity>
                </fetch>";
                var result = orgAdminUIService.RetrieveMultiple(new FetchExpression(fetchXml));
                Assert.Equal(2, result.Entities.Count);

                fetchXml =
                    @"<fetch>
                    <entity name='opportunity'>
                        <link-entity name='contact' from='contactid' to='customerid'>
                        </link-entity>
                    </entity>
                </fetch>";
                result = orgAdminUIService.RetrieveMultiple(new FetchExpression(fetchXml));
                Assert.Empty(result.Entities);

                fetchXml =
                    @"<fetch>
                    <entity name='opportunity'>
                        <link-entity name='account' from='accountid' to='customerid'>
                        </link-entity>
                        <link-entity name='contact' from='contactid' to='customerid'>
                        </link-entity>
                    </entity>
                </fetch>";
                result = orgAdminUIService.RetrieveMultiple(new FetchExpression(fetchXml));
                Assert.Empty(result.Entities);
            }
        }

        [Fact]
        public void TestFetchXmlAliasedValueLinkEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXml =
                    @"<fetch>
                    <entity name='opportunity'>
                        <link-entity name='account' from='accountid' to='customerid'>
                            <attribute name='name' />
                            <attribute name='address1_stateorprovince' />
                        </link-entity>
                        <link-entity name='account' from='accountid' to='customerid'>
                            <attribute name='name' />
                        </link-entity>
                        <link-entity name='account' from='accountid' to='customerid'>
                            <attribute name='address1_stateorprovince' />
                        </link-entity>
                        <link-entity name='contact' from='contactid' to='customerid' link-type='outer'>
                            <attribute name='fullname' />
                        </link-entity>
                    </entity>
                </fetch>";
                var result = orgAdminUIService.RetrieveMultiple(new FetchExpression(fetchXml));
                Assert.Equal(2, result.Entities.Count);
                Assert.Equal(accountName, ((AliasedValue)result.Entities[0].Attributes["account.name"]).Value);
                Assert.Equal(accountName, ((AliasedValue)result.Entities[1].Attributes["account.name"]).Value);
                Assert.Equal(accountAddress1Stateorprovince, ((AliasedValue)result.Entities[0].Attributes["account.address1_stateorprovince"]).Value);
                Assert.Equal(accountAddress1Stateorprovince, ((AliasedValue)result.Entities[1].Attributes["account.address1_stateorprovince"]).Value);
                Assert.False(result.Entities[0].Attributes.ContainsKey("contact.fullname"));
                Assert.False(result.Entities[1].Attributes.ContainsKey("contact.fullname"));
            }
        }

    }
}
