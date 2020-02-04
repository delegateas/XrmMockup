using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.Collections.Generic;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestDefaultBusinessUnitTeams : UnitTestBase
    {
        BusinessUnit businessUnit1;


        [TestInitialize]
        public void Initialize()
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;
            businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };
        }

        [TestMethod]
        public void CreateBusinessUnit()
        {
            businessUnit1.Id = orgAdminService.Create(businessUnit1);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamSet
                    .Where(x => x.BusinessUnitId.Id == businessUnit1.Id)
                    .Where(x => x.Name == businessUnit1.Name)
                    .FirstOrDefault();

                Assert.IsNotNull(fetchedTeam);
            }
        }
    }
}
