using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestSettings : UnitTestBase
    {
        public TestSettings(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestNoExceptionRequest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var req = new OrganizationRequest("WrongRequestThatFails");
                try
                {
                    orgAdminUIService.Execute(req);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<NotImplementedException>(e);
                }

                req = new OrganizationRequest("TestWrongRequest");
                orgAdminUIService.Execute(req);
            }
        }

        [Fact(Skip = "Using real data")]
        public void TestRealDataRetrieve()
        {
            var acc = new Account(new Guid("9155CF31-BA6A-E611-80E0-C4346BAC0E68"))
            {
                Name = "babuasd"
            };
            orgRealDataService.Update(acc);
            var retrieved = orgRealDataService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(acc.Name, retrieved.Name);
            Assert.Equal("12321123312", retrieved.AccountNumber);
        }

        [Fact(Skip = "Using real data")]
        public void TestRealDataRetrieveMultiple()
        {
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                PageInfo = new PagingInfo()
                {
                    Count = 1000,
                    PageNumber = 1
                }
            };
            var res = orgRealDataService.RetrieveMultiple(query);
            Assert.True(res.Entities.Count > 0);
        }
    }
}
