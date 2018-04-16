using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestLeads : UnitTestBase
    {
        [TestMethod]
        public void TestQualifyLeadRequestLeadIdMissingFails()
        {
            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified)
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
        public void TestQualifyLeadRequestLeadIdNotExistingFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead",
                Id = Guid.NewGuid()
            };
            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestOpportunityCurrencyIdWrongLogicalNameFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var currency = new Entity
            {
                LogicalName = "Wrong Logical Name",
                Id = Guid.NewGuid()
            };

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                OpportunityCurrencyId = currency.ToEntityReference()
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
        public void TestQualifyLeadRequestOpportunityCustomerIdNotExistingFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var customer = new Account()
            {
                Id = Guid.NewGuid()
            };

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                OpportunityCustomerId = customer.ToEntityReference()
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
        public void TestQualifyLeadRequestLeadNotExistingFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead",
                Id = Guid.NewGuid()
            };

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestLeadWrongLogicalNameFails()
        {
            var account = new Account();

            account.Id = orgAdminUIService.Create(account);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = account.ToEntityReference(),
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

#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
        [TestMethod]
        public void TestQualifyLeadRequestLeadAlreadyClosedFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);
            lead.SetState(orgAdminUIService, LeadState.Qualified, Lead_StatusCode.Qualified);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference()
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
#else
        [TestMethod]
        public void TestQualifyLeadRequestLeadAlreadyClosedFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);
            lead.StateCode = LeadState.Qualified;
            lead.StatusCode = Lead_StatusCode.Qualified;

            orgAdminUIService.Update(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference()
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

        [TestMethod]
        public void TestQualifyLeadRequestInvalidStatusCodeFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue(9999),
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestStateNotQualifiedFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Canceled),
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestCreateAccountAccountAlreadyExistsFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var account = new Account
            {
                Name = "Test Lead"
            };

            account.Id = orgAdminUIService.Create(account);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                CreateAccount = true,
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestCreateContactContactAlreadyExistsFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            contact.Id = orgAdminUIService.Create(contact);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                CreateContact = true,
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestCreateOpportunityOpportunityCustomerIdWrongLogicalNameFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                CreateOpportunity = true,
                OpportunityCustomerId = lead.ToEntityReference(),
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestCreateOpportunityOpportunityCustomerIdDoesNotExistFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Lead",
                Id = Guid.NewGuid()
            };

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                CreateOpportunity = true,
                OpportunityCustomerId = contact.ToEntityReference(),
                LeadId = lead.ToEntityReference()
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
        public void TestQualifyLeadRequestSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 0);
        }

        [TestMethod]
        public void TestQualifyLeadRequestOpportunityCurrencyIdSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            var currency = new TransactionCurrency()
            {
                ExchangeRate = 0.5m,
                CurrencyPrecision = 2
            };

            lead.Id = orgAdminUIService.Create(lead);
            currency.Id = orgAdminUIService.Create(currency);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                OpportunityCurrencyId = currency.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 0);
        }

        [TestMethod]
        public void TestQualifyLeadRequestOpportunityCustomerIdSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            var account = new Account();
            lead.Id = orgAdminUIService.Create(lead);
            account.Id = orgAdminUIService.Create(account);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                OpportunityCustomerId = account.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 0);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateAccountSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateAccount = true
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateContactSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateContact = true
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateOpportunityAccountSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            lead.Id = orgAdminUIService.Create(lead);

            var account = new Account();
            account.Id = orgAdminUIService.Create(account);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                OpportunityCustomerId = account.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateOpportunityContactSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            lead.Id = orgAdminUIService.Create(lead);

            var contact = new Contact();
            contact.Id = orgAdminUIService.Create(contact);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                OpportunityCustomerId = contact.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateAllSuccess()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };
            lead.Id = orgAdminUIService.Create(lead);

            var contact = new Contact();
            contact.Id = orgAdminUIService.Create(contact);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                CreateAccount = true,
                CreateContact = true,
                OpportunityCustomerId = contact.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 3);
        }
    }
}
