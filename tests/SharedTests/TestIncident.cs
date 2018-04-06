using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmMockupTest;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace SharedTests
{
    [TestClass]
    public class TestIncident : UnitTestBase
    {

#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
        [TestMethod]
        public void TestCloseIncidentRequestSuccess()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
        }

        [TestMethod]
        public void TestCloseIncidentRequestFailsWhenIncidentResolutionMissing()
        {
            var incident = new Incident();
            incident.Id = orgAdminUIService.Create(incident);
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

            var incidentResolution = new Entity();
            incidentResolution.Attributes.Add("incidentid", incident.ToEntityReference());

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

            var incidentResolution = new Entity("invalidlogicalname", Guid.NewGuid());
            incidentResolution.Attributes.Add("incidentid", incident.ToEntityReference());

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

            var incidentResolution = new Entity(Account.EntityLogicalName, Guid.NewGuid());
            incidentResolution.Attributes.Add("incidentid", incident.ToEntityReference());

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
            incident.SetState(orgAdminService, IncidentState.Active, Incident_StatusCode.InProgress);

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
#endif
    }
}
