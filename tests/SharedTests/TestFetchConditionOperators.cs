using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;
namespace DG.XrmMockupTest
{
    public class TestFetchConditionOperators : UnitTestBase
    {
        public TestFetchConditionOperators(XrmMockupFixture fixture) : base(fixture)
        {
        }

        [Theory]
        [InlineData("older-than-x-years", 365 * 1440, 1, true)]
        [InlineData("older-than-x-years", 365 * 1440, 3, false)]
        [InlineData("older-than-x-months", 30 * 1440, 1, true)]
        [InlineData("older-than-x-months", 30 * 1440, 3, false)]
        [InlineData("older-than-x-weeks", 7 * 1440, 1, true)]
        [InlineData("older-than-x-weeks", 7 * 1440, 3, false)]
        [InlineData("older-than-x-days", 1440, 1, true)]
        [InlineData("older-than-x-days", 1440, 3, false)]
        [InlineData("older-than-x-hours", 60, 1, true)]
        [InlineData("older-than-x-hours", 60, 3, false)]
        [InlineData("older-than-x-minutes", 1, 1, true)]
        [InlineData("older-than-x-minutes", 1, 3, false)]
        public void TestFetchConditionOperatorsTheoryOlderThanX(string conditionOperator, int minutes, int x, bool hasHit)
        {
            orgAdminUIService.Create(
                new Opportunity()
                {
                    EstimatedCloseDate = DateTime.UtcNow.AddMinutes(-(2 * minutes))
                });

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXml1 =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='opportunity'>
                            <filter>
                                <condition attribute='estimatedclosedate' operator='{conditionOperator}' value='{x}' />
                            </filter>
                        </entity>
                    </fetch>";
                EntityCollection result1 = Fetch(fetchXml1);
                if (hasHit)
                {
                    Assert.Single(result1.Entities);
                }
                else
                {
                    Assert.Empty(result1.Entities);
                }
            }
        }

        [Theory]
        [InlineData("yesterday", 0, false)]
        [InlineData("yesterday", -1, true)]
        [InlineData("yesterday", -2, false)]
        [InlineData("today", -1, false)]
        [InlineData("today", 0, true)]
        [InlineData("today", 1, false)]
        [InlineData("tomorrow", 0, false)]
        [InlineData("tomorrow", 1, true)]
        [InlineData("tomorrow", 2, false)]
        public void TestFetchConditionOperatorsTheoryYesterdayTodayTomorrowX(string conditionOperator, int days, bool hasHit)
        {
            orgAdminUIService.Create(
                new Opportunity()
                {
                    EstimatedCloseDate = DateTime.UtcNow.AddDays(days)
                });

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXml1 =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='opportunity'>
                            <filter>
                                <condition attribute='estimatedclosedate' operator='{conditionOperator}' />
                            </filter>
                        </entity>
                    </fetch>";
                EntityCollection result1 = Fetch(fetchXml1);
                if (hasHit)
                {
                    Assert.Single(result1.Entities);
                }
                else
                {
                    Assert.Empty(result1.Entities);
                }
            }
        }

        private EntityCollection Fetch(string fetchXml2)
        {
            var conversionResponse2 = (FetchXmlToQueryExpressionResponse)orgAdminUIService.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml2 });
            QueryExpression queryExpression2 = conversionResponse2.Query;
            EntityCollection result2 = orgAdminUIService.RetrieveMultiple(queryExpression2);
            return result2;
        }
    }
}
