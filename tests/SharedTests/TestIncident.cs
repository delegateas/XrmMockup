using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Linq;
using System;
using System.ServiceModel;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestIncident : UnitTestBase
    {
        public TestIncident(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
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
            Assert.NotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedIncident = context.IncidentSet.FirstOrDefault(x => x.Id == incident.Id);
                Assert.Equal(IncidentState.Resolved, retrievedIncident.StateCode);
                Assert.Equal(Incident_StatusCode.ProblemSolved, retrievedIncident.StatusCode);

                var retrievedIncidentResolution = context.IncidentResolutionSet.FirstOrDefault(x => x.IncidentId.Id == incident.Id);
                Assert.NotNull(retrievedIncidentResolution);
            }
        }

        [Fact]
        public void TestCloseIncidentRequestFailsWhenPreviouslyResolved()
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
            Assert.NotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedIncident = context.IncidentSet.FirstOrDefault(x => x.Id == incident.Id);
                Assert.Equal(IncidentState.Resolved, retrievedIncident.StateCode);
                Assert.Equal(Incident_StatusCode.ProblemSolved, retrievedIncident.StatusCode);

                var retrievedIncidentResolution = context.IncidentResolutionSet.FirstOrDefault(x => x.IncidentId.Id == incident.Id);
                Assert.NotNull(retrievedIncidentResolution);
            }

            var incidentResolution2 = new IncidentResolution
            {
                IncidentId = incident.ToEntityReference(),
                Subject = "Resolved Incident"
            };

            var request2 = new CloseIncidentRequest()
            {
                IncidentResolution = incidentResolution2,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };

            try
            {
                orgAdminUIService.Execute(request2);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
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
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestUpdateResolvedIncidentFailsWhenFieldModificationIsNotAllowed()
        {
            var incident = new Incident()
            {
                Title = "Old Title"
            };
            incident.Id = orgAdminService.Create(incident);

            var resolution = new IncidentResolution
            {
                Subject = "Case closed",
                IncidentId = incident.ToEntityReference()
            };

            var closeRequest = new CloseIncidentRequest()
            {
                IncidentResolution = resolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };
            orgAdminService.Execute(closeRequest);

            var incidentUpdate = new Incident(incident.Id)
            {
                Title = "New Title"
            };

            try
            {
                orgAdminService.Update(incidentUpdate);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }

            var retrievedIncident = Incident.Retrieve(orgAdminService, incident.Id);
            Assert.Equal(incident.Title, retrievedIncident.Title);
        }

        [Fact]
        public void TestUpdateResolvedIncidentSucceedsWhenFieldModificationIsAllowed()
        {
            var incident = new Incident()
            {
                Title = "Old Title",
            };
            incident.Id = orgAdminService.Create(incident);

            var resolution = new IncidentResolution
            {
                Subject = "Case closed",
                IncidentId = incident.ToEntityReference()
            };

            var closeRequest = new CloseIncidentRequest()
            {
                IncidentResolution = resolution,
                Status = new OptionSetValue((int)Incident_StatusCode.ProblemSolved)
            };
            orgAdminService.Execute(closeRequest);

            var incidentUpdate = new Incident(incident.Id)
            {
                StateCode = IncidentState.Active
            };

            orgAdminService.Update(incidentUpdate);

            var retrievedIncident = Incident.Retrieve(orgAdminService, incident.Id);
            Assert.Equal(IncidentState.Active, retrievedIncident.StateCode);
        }
    }
}
