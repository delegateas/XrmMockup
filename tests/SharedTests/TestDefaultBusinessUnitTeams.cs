using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.Collections.Generic;
using System.ServiceModel.Security;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestDefaultBusinessUnitTeams : UnitTestBase
    {
        BusinessUnit businessUnit1;
        BusinessUnit businessUnit2;
        BusinessUnit businessUnit3;


        [TestInitialize]
        public void Initialize()
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;
            
            businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };

            businessUnit2 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 2" };
            businessUnit2.Id = orgAdminService.Create(businessUnit2);

            businessUnit3 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 3" };
            businessUnit3.Id = orgAdminService.Create(businessUnit3);
        }

        [TestMethod]
        public void CreateBusinessUnit()
        {
            businessUnit1.Id = orgAdminService.Create(businessUnit1);

            RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit1);
        }

        [TestMethod]
        public void UpdateBusinessUnit()
        {
            businessUnit2.Name = "A new business unit name";
            orgAdminService.Update(businessUnit2);

            RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit2);
        }

        [TestMethod]
        public void DeleteBusinessUnit()
        {
            var fetchedTeam = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(businessUnit3);

            orgAdminService.Delete("businessunit", businessUnit3.Id);
            
            try
            {
                var fetchedTeamAfterDeletion = orgAdminService.Retrieve("team", fetchedTeam.Id, new ColumnSet(true));
            }
            catch (FaultException e)
            {
                Assert.AreEqual($"The record of type 'team' with id '{fetchedTeam.Id}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.", 
                    e.Message, "Error message doesn't match expected error message, maybe a different error is thrown?");
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
            Assert.AreEqual(Team_TeamType.Owner, fetchedTeam.TeamType);
            Assert.AreEqual(true, fetchedTeam.IsDefault);
            Assert.AreEqual("Default team for the parent business unit. The name and membership for default team are inherited from their parent business unit.",
                fetchedTeam.Description);
            Assert.AreEqual(businessUnit.CreatedBy.Id, fetchedTeam.AdministratorId.Id);
            Assert.AreEqual(businessUnit.Id, fetchedTeam.BusinessUnitId.Id);
        }
    }
}
