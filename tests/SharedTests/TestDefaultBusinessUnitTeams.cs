using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestDefaultBusinessUnitTeams : UnitTestBase
    {
        private BusinessUnit _businessUnit1;
        private BusinessUnit _businessUnit2;
        private BusinessUnit _businessUnit3;

        [TestInitialize]
        public void Init()
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;
            
            _businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };

            _businessUnit2 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 2" };
            _businessUnit2.Id = orgAdminService.Create(_businessUnit2);

            _businessUnit3 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 3" };
            _businessUnit3.Id = orgAdminService.Create(_businessUnit3);
        }

        [TestMethod]
        public void CreateBusinessUnit()
        {
            _businessUnit1.Id = orgAdminService.Create(_businessUnit1);

            RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit1);
        }

        [TestMethod]
        public void UpdateBusinessUnit()
        {
            _businessUnit2.Name = "A new business unit name";
            orgAdminService.Update(_businessUnit2);

            RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit2);
        }

        [TestMethod]
        public void DeleteBusinessUnit()
        {
            var fetchedTeam = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit3);

            orgAdminService.Delete("businessunit", _businessUnit3.Id);
            
            try
            {
                orgAdminService.Retrieve("team", fetchedTeam.Id, new ColumnSet(true));
            }
            catch (FaultException e)
            {
                // Test error message
                Assert.AreEqual(
                    $"The record of type 'team' with id '{fetchedTeam.Id}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.", 
                    e.Message);
                return;
            }
            Assert.Fail("Exception when trying to fetch deleted team isn't thrown.");
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
#if !(XRM_MOCKUP_2011)
            Assert.AreEqual(Team_TeamType.Owner, fetchedTeam.TeamType);
#endif
            Assert.AreEqual(true, fetchedTeam.IsDefault);
            Assert.AreEqual("Default team for the parent business unit. The name and membership for default team are inherited from their parent business unit.",
                fetchedTeam.Description);
            Assert.AreEqual(businessUnit.CreatedBy.Id, fetchedTeam.AdministratorId.Id);
            Assert.AreEqual(businessUnit.Id, fetchedTeam.BusinessUnitId.Id);
        }
    }
}
