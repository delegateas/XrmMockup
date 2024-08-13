using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestSendEmailFromTemplate : UnitTestBase
    {
        public TestSendEmailFromTemplate(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSendEmailFromTemplateRequestSuccess()
        {
            var account = new Account
            {
                Name = "Test Account",
            };
            account.Id = orgAdminUIService.Create(account);

            string bodyXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></xsl:template></xsl:stylesheet>";

            string subjectXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA[New account notification]]></xsl:template></xsl:stylesheet>";

            string presentationXml =
               "<template><text><![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></text></template>";

            string subjectPresentationXml =
               "<template><text><![CDATA[New account notification]]></text></template>";

            var template = new Template
            {
                Title = "Sample E-mail Template for Account",
                Body = bodyXml,
                Subject = subjectXml,
                PresentationXml = presentationXml,
                SubjectPresentationXml = subjectPresentationXml,
                TemplateTypeCode = Account.EntityLogicalName,
                LanguageCode = 1033, // For US English.
                IsPersonal = false
            };
            template.Id = orgAdminUIService.Create(template);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Contact",
                EMailAddress1 = "test@contact.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                },
                To = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "placeholder",
                RegardingObjectId = account.ToEntityReference()
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailFromTemplateRequest = new SendEmailFromTemplateRequest
            {
                RegardingId = account.Id,
                RegardingType = account.LogicalName,
                Target = email,
                TemplateId = template.Id
            };

            var response = orgAdminUIService.Execute(sendEmailFromTemplateRequest) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(EmailState.Completed, retrievedEmail.StateCode);
                Assert.Equal(Email_StatusCode.PendingSend, retrievedEmail.StatusCode);
                Assert.Equal(template.GetAttributeValue<string>("subject"), retrievedEmail.Subject);
                Assert.Equal(template.GetAttributeValue<string>("body"), retrievedEmail.Description);
            }
        }

        [Fact]
        public void TestSendEmailFromTemplateRequestFailsWhenEmailNotCreated()
        {
            var account = new Account
            {
                Name = "Test Account",
            };
            account.Id = orgAdminUIService.Create(account);

            string bodyXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></xsl:template></xsl:stylesheet>";

            string subjectXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA[New account notification]]></xsl:template></xsl:stylesheet>";

            string presentationXml =
               "<template><text><![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></text></template>";

            string subjectPresentationXml =
               "<template><text><![CDATA[New account notification]]></text></template>";

            var template = new Template
            {
                Title = "Sample E-mail Template for Account",
                Body = bodyXml,
                Subject = subjectXml,
                PresentationXml = presentationXml,
                SubjectPresentationXml = subjectPresentationXml,
                TemplateTypeCode = Account.EntityLogicalName,
                LanguageCode = 1033, // For US English.
                IsPersonal = false
            };
            template.Id = orgAdminUIService.Create(template);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Contact",
                EMailAddress1 = "test@contact.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                },
                To = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "placeholder",
                RegardingObjectId = account.ToEntityReference()
            };

            var sendEmailFromTemplateRequest = new SendEmailFromTemplateRequest
            {
                RegardingId = account.Id,
                RegardingType = account.LogicalName,
                Target = email,
                TemplateId = template.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailFromTemplateRequest));
        }

        [Fact]
        public void TestSendEmailFromTemplateRequestFailsWhenEmailHasNoRegardingObject()
        {
            var account = new Account
            {
                Name = "Test Account",
            };
            account.Id = orgAdminUIService.Create(account);

            string bodyXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></xsl:template></xsl:stylesheet>";

            string subjectXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA[New account notification]]></xsl:template></xsl:stylesheet>";

            string presentationXml =
               "<template><text><![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></text></template>";

            string subjectPresentationXml =
               "<template><text><![CDATA[New account notification]]></text></template>";

            var template = new Template
            {
                Title = "Sample E-mail Template for Account",
                Body = bodyXml,
                Subject = subjectXml,
                PresentationXml = presentationXml,
                SubjectPresentationXml = subjectPresentationXml,
                TemplateTypeCode = Account.EntityLogicalName,
                LanguageCode = 1033, // For US English.
                IsPersonal = false
            };
            template.Id = orgAdminUIService.Create(template);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Contact",
                EMailAddress1 = "test@contact.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                },
                To = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "placeholder"
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailFromTemplateRequest = new SendEmailFromTemplateRequest
            {
                RegardingId = account.Id,
                RegardingType = account.LogicalName,
                Target = email,
                TemplateId = template.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailFromTemplateRequest));
        }

        [Fact]
        public void TestSendEmailFromTemplateRequestFailsWhenSpecifyingTwoRegarding()
        {
            var account = new Account
            {
                Name = "Test Account",
            };
            account.Id = orgAdminUIService.Create(account);

            string bodyXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></xsl:template></xsl:stylesheet>";

            string subjectXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA[New account notification]]></xsl:template></xsl:stylesheet>";

            string presentationXml =
               "<template><text><![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></text></template>";

            string subjectPresentationXml =
               "<template><text><![CDATA[New account notification]]></text></template>";

            var template = new Template
            {
                Title = "Sample E-mail Template for Account",
                Body = bodyXml,
                Subject = subjectXml,
                PresentationXml = presentationXml,
                SubjectPresentationXml = subjectPresentationXml,
                TemplateTypeCode = Account.EntityLogicalName,
                LanguageCode = 1033, // For US English.
                IsPersonal = false
            };
            template.Id = orgAdminUIService.Create(template);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Contact",
                EMailAddress1 = "test@contact.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                },
                To = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "placeholder",
                RegardingObjectId = contact.ToEntityReference()
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailFromTemplateRequest = new SendEmailFromTemplateRequest
            {
                RegardingId = account.Id,
                RegardingType = account.LogicalName,
                Target = email,
                TemplateId = template.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailFromTemplateRequest));
        }

        [Fact]
        public void TestSendEmailFromTemplateRequestFailsWhenTemplateTypeCodeDifferentFromRegardingType()
        {
            var account = new Account
            {
                Name = "Test Account",
            };
            account.Id = orgAdminUIService.Create(account);

            string bodyXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></xsl:template></xsl:stylesheet>";

            string subjectXml =
               "<?xml version=\"1.0\" ?>"
               + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
               + "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">"
               + "<![CDATA[New account notification]]></xsl:template></xsl:stylesheet>";

            string presentationXml =
               "<template><text><![CDATA["
               + "This message is to notify you that a new account has been created."
               + "]]></text></template>";

            string subjectPresentationXml =
               "<template><text><![CDATA[New account notification]]></text></template>";

            var template = new Template
            {
                Title = "Sample E-mail Template for Account",
                Body = bodyXml,
                Subject = subjectXml,
                PresentationXml = presentationXml,
                SubjectPresentationXml = subjectPresentationXml,
                TemplateTypeCode = "contact",
                LanguageCode = 1033, // For US English.
                IsPersonal = false
            };
            template.Id = orgAdminUIService.Create(template);

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Contact",
                EMailAddress1 = "test@contact.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                },
                To = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "placeholder",
                RegardingObjectId = account.ToEntityReference()
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailFromTemplateRequest = new SendEmailFromTemplateRequest
            {
                RegardingId = account.Id,
                RegardingType = account.LogicalName,
                Target = email,
                TemplateId = template.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailFromTemplateRequest));
        }
    }
}
