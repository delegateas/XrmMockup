using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
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

            // Needs update of context to support strongly typed templates
            var template = new Entity
            {
                LogicalName = "template",
                ["title"] = "Sample E-mail Template for Account",
                ["body"] = bodyXml,
                ["subject"] = subjectXml,
                ["presentationxml"] = presentationXml,
                ["subjectpresentationxml"] = subjectPresentationXml,
                ["templatetypecode"] = Account.EntityLogicalName,
                ["languagecode"] = 1033, // For US English.
                ["ispersonal"] = false
            };
            template.Id = new Guid("00000000-0000-0000-0000-000000000001"); // orgAdminUIService.Create(template);

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
                //Assert.Equal(template.GetAttributeValue<string>("subject"), retrievedEmail.Subject);
                //Assert.Equal(template.GetAttributeValue<string>("body"), retrievedEmail.Description);
            }
        }
    }
}
