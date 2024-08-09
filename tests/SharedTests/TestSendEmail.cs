using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestSendEmail : UnitTestBase
    {
        public TestSendEmail(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSendEmailRequestSuccessWhenIssueSendTrue()
        {
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
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(EmailState.Completed, retrievedEmail.StateCode);
                Assert.Equal(Email_StatusCode.PendingSend, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestSuccessWhenIssueSendFalse()
        {
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
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(EmailState.Completed, retrievedEmail.StateCode);
                Assert.Equal(Email_StatusCode.Sent, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestSuccessWithUnresolvedParties()
        {
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
                        AddressUsed = "test@test.com"
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(EmailState.Completed, retrievedEmail.StateCode);
                Assert.Equal(Email_StatusCode.PendingSend, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenNoSender()
        {
            var email = new Email();
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenSenderNotCreated()
        {
            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = new Contact
                        {
                            FirstName = "Test",
                            LastName = "Unsaved Contact"
                        }.ToEntityReference()
                    }
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenSenderInvalid()
        {
            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty()
                    {
                        AddressUsed = "test@test.com"
                    }
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenMoreThanOneSender()
        {
            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty(),
                    new ActivityParty(),
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenNoRecipients()
        {
            var email = new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenRecipientInvalid()
        {
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
                    new ActivityParty()
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }
    }
}
