using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestDefaultBusinessUnitTeamMembers : UnitTestBase
    {
        BusinessUnit businessUnit1;
        BusinessUnit businessUnit2;
        BusinessUnit businessUnit3;
        BusinessUnit businessUnit4;
        private SystemUser user1;
        private SystemUser user2;

        [TestInitialize]
        public void Initialize()
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;

            businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };
            businessUnit1.Id = orgAdminService.Create(businessUnit1);
            
            businessUnit2 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 2" };
            businessUnit2.Id = orgAdminService.Create(businessUnit2);
            user1 = crm.CreateUser(orgAdminService, businessUnit2.ToEntityReference(), SecurityRoles.SystemCustomizer) as SystemUser;

            businessUnit3 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 3" };
            businessUnit3.Id = orgAdminService.Create(businessUnit3);
            businessUnit4 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 4" };
            businessUnit4.Id = orgAdminService.Create(businessUnit4);
            user2 = crm.CreateUser(orgAdminService, businessUnit3.ToEntityReference(), SecurityRoles.SystemCustomizer) as SystemUser;
        }

        [TestMethod]
        public void AddMembersToBusinessUnit()
        {
            var team = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit1);

            SystemUser user11 = crm.CreateUser(orgAdminService, businessUnit1.ToEntityReference(), SecurityRoles.SystemCustomizer) as SystemUser;

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.AreEqual(1, fetchedTeam.Count);
            }
        }

        [TestMethod]
        public void RemoveMemberFromBusinessUnit()
        {
            var team = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit2);

            orgAdminService.Delete("systemuser", user1.Id);

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.AreEqual(0, fetchedTeam.Count);
            }
        }

        [TestMethod]
        public void MoveMemberFromBUOneToBUTwo()
        {
            var retrievedUser = orgAdminService.Retrieve("systemuser", user2.Id, new ColumnSet("businessunitid"));

            retrievedUser.Attributes["businessunitid"] = businessUnit4.ToEntityReference();

            orgAdminService.Update(retrievedUser);

            var team3 = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit3);
            var team4 = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit4);

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team3.Id).ToList();
                Assert.AreEqual(0, fetchedTeam.Count);
            }

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team4.Id).ToList();
                Assert.AreEqual(1, fetchedTeam.Count);
            }
        }

        private Team RetrieveBusinessUnitDefaultTeamAndCheckAttributes(BusinessUnit businessUnit)
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamSet
                    .Where(x => x.BusinessUnitId.Id == businessUnit.Id)
                    .Where(x => x.Name == businessUnit.Name)
                    .FirstOrDefault();

                Assert.IsNotNull(fetchedTeam);
                CheckTeamAttributes(fetchedTeam, businessUnit);

                return fetchedTeam;
            }
        }

        private void CheckTeamAttributes(Team fetchedTeam, BusinessUnit businessUnit)
        {
            businessUnit = (BusinessUnit)orgAdminService.Retrieve(LogicalNames.BusinessUnit, businessUnit.Id, new ColumnSet("name", "createdby"));

            Assert.AreEqual(businessUnit.Name, fetchedTeam.Name);
            Assert.AreEqual(Team_TeamType.Owner, fetchedTeam.TeamType);
            Assert.AreEqual(true, fetchedTeam.IsDefault);
            Assert.AreEqual("Default team for the parent business unit. The name and membership for default team are inherited from their parent business unit.",
                fetchedTeam.Description);
            Assert.AreEqual(businessUnit.CreatedBy.Id, fetchedTeam.AdministratorId.Id);
            Assert.AreEqual(businessUnit.Id, fetchedTeam.BusinessUnitId.Id);
        }
    }
}
