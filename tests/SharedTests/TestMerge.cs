using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    public class TestMerge : UnitTestBase
    {
        public TestMerge(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestMergeAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var _account1Id= orgAdminUIService.Create(
                    new Account {
                        Name = "Fourth Coffee",
                        Description = "Coffee House"
                    });
                var _account2Id = orgAdminUIService.Create(
                    new Account {
                        Name = "Fourth Coffee",
                        NumberOfEmployees = 55,
                    });


                // Create the target for the request.
                EntityReference target = new EntityReference();

                // Id is the GUID of the account that is being merged into.
                // LogicalName is the type of the entity being merged to, as a string
                target.Id = _account1Id;
                target.LogicalName = Account.EntityLogicalName;

                // Create another account to hold new data to merge into the entity.
                // If you use the subordinate account object, its data will be merged.
                Account updateContent = new Account();
                updateContent.Address1_Line1 = "Test";
                updateContent.NumberOfEmployees = 45;

                var req = new MergeRequest {
                    Target = target,
                    SubordinateId = _account2Id,
                    UpdateContent = updateContent,
                    PerformParentingChecks = false
                };

                // Execute the request.
                MergeResponse merged = (MergeResponse)orgAdminUIService.Execute(req);
                

                Account mergeeAccount =
                    (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName,
                    _account2Id, new ColumnSet(true));

                Assert.True(mergeeAccount.StateCode.Equals(AccountState.Inactive));
                Assert.True(mergeeAccount.StatusCode.Equals(Account_StatusCode.Inactive));
                Assert.True(mergeeAccount.Merged.Value);

                Account mergedAccount =
                    (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName,
                    _account1Id, new ColumnSet(true));

                Assert.Equal("Fourth Coffee", mergedAccount.Name);
                Assert.Equal("Coffee House", mergedAccount.Description);
                Assert.Equal(updateContent.NumberOfEmployees, mergedAccount.NumberOfEmployees);
                Assert.Equal(updateContent.Address1_Line1, mergedAccount.Address1_Line1);

            }
        }
    }

}
