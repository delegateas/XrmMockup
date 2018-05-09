using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmMockupTest;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestIncident : UnitTestBase
    {

        [TestMethod]
        public void TestCloseIncidentRequestSuccess()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif
            var incidentResolution = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            var response = orgAdminUIService.Execute(request) as CloseIncidentResponse;
            Assert.IsNotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedIncident = context.IncidentSet.FirstOrDefault(x => x.Id == incident.Id);
                Assert.AreEqual(Incident_StatusCode.ProblemSolved, retrievedIncident.StatusCode);

                var retrievedIncidentResolution = context.IncidentResolutionSet.FirstOrDefault(x => x.IncidentId.Id == incident.Id);
                Assert.IsNotNull(retrievedIncidentResolution);
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenIncidentResolutionMissing()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var request = new CloseIncidentRequest()
            {
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenLogicalNameMissing()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new Entity();
            incidentResolution.Attributes["incidentid"] = incident.ToEntityReference();

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenLogicalNameDoesNotExist()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new Entity
            {
                LogicalName = "invalidlogicalname",
                Id = Guid.NewGuid()
            };

            incidentResolution.Attributes["incidentid"] = incident.ToEntityReference();

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenLogicalNameWrong()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new Account
            {
                Id = Guid.NewGuid()
            };
            incidentResolution.Attributes["incidentid"] = incident.ToEntityReference();

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenStatusMissing()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenStatusDoesNotExist()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue(12345678)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenStateOfStatusWrong()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.InProgress)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenIncidentidMissing()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new IncidentResolution
            {
                Subject = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenIncidentDoesNotExist()
        {
            var incident = new Incident();

            var incidentResolution = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.InformationProvided)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenIncidentResolutionAlreadyExists()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);
#else
            incident.StateCode = IncidentState.Active;
            incident.StatusCode = Incident_StatusCode.InProgress;
            orgAdminUIService.Update(incident);
#endif

            var incidentResolution = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            orgAdminUIService.Create(incidentResolution);

            var request = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }
    }
}
