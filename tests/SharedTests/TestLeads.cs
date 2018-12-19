using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using DG.XrmMockupTest;
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

        [TestMethod]
        public void TestQualifyLeadRequestLeadAlreadyClosedFails()
        {
            var lead = new Lead
            {
                FirstName = "Test",
                LastName = "Lead"
            };

            lead.Id = orgAdminUIService.Create(lead);

#if (XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
            lead.SetState(orgAdminUIService, LeadState.Qualified, Lead_StatusCode.Qualified);
#else
            lead.StateCode = LeadState.Qualified;
            lead.StatusCode = Lead_StatusCode.Qualified;
            orgAdminUIService.Update(lead);
#endif

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

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);
        }

        [TestMethod]
        public void TestQualifyLeadRequestOpportunityCurrencyIdSuccess()
        {
            var lead = GetLeadWithAttributes();
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

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);
        }

        [TestMethod]
        public void TestQualifyLeadRequestOpportunityCustomerIdSuccess()
        {
            var lead = GetLeadWithAttributes();

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

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateAccountSuccess()
        {
            var lead = GetLeadWithAttributes();
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

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var accountId = createdEntities.First(e => e.LogicalName == Account.EntityLogicalName).Id;
            var account = orgAdminUIService.Retrieve(Account.EntityLogicalName, accountId, new ColumnSet(true)) as Account;

            Assert.IsNotNull(account);
            AssertAccountMatchesLead(account, lead);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateContactSuccess()
        {
            var lead = GetLeadWithAttributes();
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

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var contactId = createdEntities.First(e => e.LogicalName == Contact.EntityLogicalName).Id;
            var contact = orgAdminUIService.Retrieve(Contact.EntityLogicalName, contactId, new ColumnSet(true)) as Contact;

            Assert.IsNotNull(contact);
            AssertContactMatchesLead(contact, lead);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateOpportunitySuccess()
        {
            var lead = GetLeadWithAttributes();
            lead.Id = orgAdminUIService.Create(lead);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var opportunityId = createdEntities.First(e => e.LogicalName == Opportunity.EntityLogicalName).Id;
            var opportunity = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunityId, new ColumnSet(true)) as Opportunity;

            Assert.IsNotNull(opportunity);
            AssertOpportunityMatchesLead(opportunity, lead);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateOpportunityCurrencySuccess()
        {
            var lead = GetLeadWithAttributes();
            lead.Id = orgAdminUIService.Create(lead);

            var currency = new TransactionCurrency()
            {
                ExchangeRate = 0.5m,
                CurrencyPrecision = 2
            };

            currency.Id = orgAdminUIService.Create(currency);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                OpportunityCurrencyId = currency.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var opportunityId = createdEntities.First(e => e.LogicalName == Opportunity.EntityLogicalName).Id;
            var opportunity = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunityId, new ColumnSet(true)) as Opportunity;

            Assert.IsNotNull(opportunity);
            AssertOpportunityMatchesLead(opportunity, lead);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateOpportunityCustomerAccountSuccess()
        {
            var lead = GetLeadWithAttributes();
            lead.Id = orgAdminUIService.Create(lead);

            var account = new Account();
            account.Id = orgAdminUIService.Create(account);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                OpportunityCustomerId = account.ToEntityReference(),
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 1);

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var opportunityId = createdEntities.First(e => e.LogicalName == Opportunity.EntityLogicalName).Id;
            var opportunity = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunityId, new ColumnSet(true)) as Opportunity;

            Assert.IsNotNull(opportunity);
            AssertOpportunityMatchesLead(opportunity, lead, account);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateOpportunityCustomerContactSuccess()
        {
            var lead = GetLeadWithAttributes();
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

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var opportunityId = createdEntities.First(e => e.LogicalName == Opportunity.EntityLogicalName).Id;
            var opportunity = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunityId, new ColumnSet(true)) as Opportunity;

            Assert.IsNotNull(opportunity);
            AssertOpportunityMatchesLead(opportunity, lead, contact);
        }

        [TestMethod]
        public void TestQualifyLeadRequestCreateAllSuccess()
        {
            var lead = GetLeadWithAttributes();
            lead.Id = orgAdminUIService.Create(lead);

            var customer = new Contact();
            customer.Id = orgAdminUIService.Create(customer);

            var currency = new TransactionCurrency()
            {
                ExchangeRate = 0.5m,
                CurrencyPrecision = 2
            };

            currency.Id = orgAdminUIService.Create(currency);

            var request = new QualifyLeadRequest
            {
                Status = new OptionSetValue((int)Lead_StatusCode.Qualified),
                LeadId = lead.ToEntityReference(),
                CreateOpportunity = true,
                CreateAccount = true,
                CreateContact = true,
                OpportunityCustomerId = customer.ToEntityReference(),
                OpportunityCurrencyId = currency.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as QualifyLeadResponse;
            Assert.IsNotNull(response);
            var createdEntities = response.CreatedEntities;
            Assert.IsNotNull(createdEntities);
            Assert.IsTrue(createdEntities.Count == 3);

            var qualifiedLead = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;

            Assert.IsNotNull(qualifiedLead);
            Assert.AreEqual(Lead_StatusCode.Qualified, qualifiedLead.StatusCode);

            var opportunityId = createdEntities.First(e => e.LogicalName == Opportunity.EntityLogicalName).Id;
            var opportunity = orgAdminUIService.Retrieve(Opportunity.EntityLogicalName, opportunityId, new ColumnSet(true)) as Opportunity;
            Assert.IsNotNull(opportunity);
            AssertOpportunityMatchesLead(opportunity, lead, customer, currency);

            var contactId = createdEntities.First(e => e.LogicalName == Contact.EntityLogicalName).Id;
            var contact = orgAdminUIService.Retrieve(Contact.EntityLogicalName, contactId, new ColumnSet(true)) as Contact;
            AssertContactMatchesLead(contact, lead);

            var accountId = createdEntities.First(e => e.LogicalName == Account.EntityLogicalName).Id;
            var account = orgAdminUIService.Retrieve(Account.EntityLogicalName, accountId, new ColumnSet(true)) as Account;
            AssertAccountMatchesLead(account, lead);
        }

        private Lead GetLeadWithAttributes()
        {
            return new Lead
            {
                FirstName = "Test",
                LastName = "Lead",
                Subject = "Subject",
                SIC = "SIC",
                EMailAddress1 = "EMailAddress1",
                CompanyName = "CompanyName",
                Fax = "Fax",
                WebSiteUrl = "WebSiteUrl",
                Address1_Country = "Address1_Country",
                Address1_City = "Address1_City",
                Address1_Line1 = "Address1_Line1",
                Address1_Line2 = "Address1_Line2",
                Address1_Line3 = "Address1_Line3",
                Address1_PostalCode = "Address1_PostalCode",
                Address1_StateOrProvince = "Address1_StateOrProvince",
                Telephone2 = "Telephone2",
                DoNotPostalMail = true,
                DoNotPhone = true,
                DoNotFax = true,
                DoNotSendMM = true,
                Description = "Description",
                DoNotEMail = true,
                YomiCompanyName = "YomiCompanyName",
                DoNotBulkEMail = true,
                MobilePhone = "MobilePhone",
                EMailAddress3 = "EMailAddress3",
                YomiLastName = "YomiLastName",
                YomiMiddleName = "YomiMiddleName",
                Address2_Country = "Address2_Country",
                JobTitle = "JobTitle",
                Pager = "Pager",
                Telephone3 = "Telephone3",
                Address2_Fax = "Address2_Fax",
                EMailAddress2 = "EMailAddress2",
                YomiFirstName = "YomiFirstName",
                QualificationComments = "QualificationComments"
            };
        }

        private void AssertOpportunityMatchesLead(Opportunity opportunity, Lead lead, Entity customer = null, TransactionCurrency currency = null)
        {
            Assert.AreEqual(lead.Subject, opportunity.Name);
            Assert.AreEqual(lead.QualificationComments, opportunity.QualificationComments);
            Assert.AreEqual(lead.Description, opportunity.Description);
            Assert.IsNotNull(opportunity.OriginatingLeadId);
            Assert.AreEqual(lead.LeadId, opportunity.OriginatingLeadId.Id);

            if(customer != null)
            {
                Assert.IsNotNull(opportunity.CustomerId);
                Assert.AreEqual(customer.Id, opportunity.CustomerId.Id);
            }

            if(currency != null)
            {
                Assert.IsNotNull(opportunity.TransactionCurrencyId);
                Assert.AreEqual(currency.Id, opportunity.TransactionCurrencyId.Id);
            }
        }

        private void AssertContactMatchesLead(Contact contact, Lead lead)
        {
            Assert.AreEqual(lead.MobilePhone, contact.MobilePhone);
            Assert.AreEqual(lead.EMailAddress1, contact.EMailAddress1);
            Assert.AreEqual(lead.EMailAddress3, contact.EMailAddress3);
            Assert.AreEqual(lead.WebSiteUrl, contact.WebSiteUrl);
            Assert.AreEqual(lead.YomiLastName, contact.YomiLastName);
            Assert.AreEqual(lead.LastName, contact.LastName);
            Assert.AreEqual(lead.DoNotPostalMail, contact.DoNotPostalMail);
            Assert.AreEqual(lead.DoNotPhone, contact.DoNotPhone);
            Assert.AreEqual(lead.YomiMiddleName, contact.YomiMiddleName);
            Assert.AreEqual(lead.Description, contact.Description);
            Assert.AreEqual(lead.FirstName, contact.FirstName);
            Assert.AreEqual(lead.DoNotEMail, contact.DoNotEMail);
            Assert.AreEqual(lead.Address1_StateOrProvince, contact.Address1_StateOrProvince);
            Assert.AreEqual(lead.Address2_Country, contact.Address2_Country);
            Assert.AreEqual(lead.DoNotFax, contact.DoNotFax);
            Assert.AreEqual(lead.DoNotSendMM, contact.DoNotSendMM);
            Assert.AreEqual(lead.JobTitle, contact.JobTitle);
            Assert.AreEqual(lead.Pager, contact.Pager);
            Assert.AreEqual(lead.Address1_Country, contact.Address1_Country);
            Assert.AreEqual(lead.Address1_Line1, contact.Address1_Line1);
            Assert.AreEqual(lead.Address1_Line2, contact.Address1_Line2);
            Assert.AreEqual(lead.Address1_Line3, contact.Address1_Line3);
            Assert.AreEqual(lead.Telephone2, contact.Telephone2);
            Assert.AreEqual(lead.Telephone3, contact.Telephone3);
            Assert.AreEqual(lead.Address2_Fax, contact.Address2_Fax);
            Assert.AreEqual(lead.DoNotBulkEMail, contact.DoNotBulkEMail);
            Assert.AreEqual(lead.EMailAddress2, contact.EMailAddress2);
            Assert.AreEqual(lead.Fax, contact.Fax);
            Assert.AreEqual(lead.YomiFirstName, contact.YomiFirstName);
            Assert.AreEqual(lead.Address1_PostalCode, contact.Address1_PostalCode);
            Assert.AreEqual(lead.Address1_City, contact.Address1_City);
            Assert.AreEqual(lead.Address2_Country, contact.Address2_Country);
            Assert.IsNotNull(contact.OriginatingLeadId);
            Assert.AreEqual(lead.Id, contact.OriginatingLeadId.Id);
        }

        private void AssertAccountMatchesLead(Account account, Lead lead)
        {
            Assert.AreEqual(lead.SIC, account.SIC);
            Assert.AreEqual(lead.EMailAddress1, account.EMailAddress1);
            Assert.AreEqual(lead.CompanyName, account.Name);
            Assert.AreEqual(lead.Fax, account.Fax);
            Assert.AreEqual(lead.WebSiteUrl, account.WebSiteURL);
            Assert.AreEqual(lead.Address1_City, account.Address1_City);
            Assert.AreEqual(lead.Address1_Country, account.Address1_Country);
            Assert.AreEqual(lead.Address1_Line1, account.Address1_Line1);
            Assert.AreEqual(lead.Address1_Line2, account.Address1_Line2);
            Assert.AreEqual(lead.Address1_Line3, account.Address1_Line3);
            Assert.AreEqual(lead.Address1_PostalCode, account.Address1_PostalCode);
            Assert.AreEqual(lead.Address1_StateOrProvince, account.Address1_StateOrProvince);
            Assert.AreEqual(lead.Telephone2, account.Telephone2);
            Assert.AreEqual(lead.DoNotPostalMail, account.DoNotPostalMail);
            Assert.AreEqual(lead.DoNotPhone, account.DoNotPhone);
            Assert.AreEqual(lead.DoNotFax, account.DoNotFax);
            Assert.AreEqual(lead.DoNotSendMM, account.DoNotSendMM);
            Assert.AreEqual(lead.Description, account.Description);
            Assert.AreEqual(lead.DoNotEMail, account.DoNotEMail);
            Assert.AreEqual(lead.YomiCompanyName, account.YomiName);
            Assert.AreEqual(lead.DoNotBulkEMail, account.DoNotBulkEMail);
            Assert.IsNotNull(account.OriginatingLeadId);
            Assert.AreEqual(lead.Id, account.OriginatingLeadId.Id);
        }
    }
}
