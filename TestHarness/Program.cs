using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {

            SystemUser user1;
            Account acc1;
            Account acc2;
            Contact contact;

            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { },
                CodeActivityInstanceTypes = new Type[] { },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                ExceptionFreeRequests = new string[] { "TestWrongRequest" },
                MetadataDirectoryPath = "../../Metadata"
                ,
                DatabaseConnectionString = "Server=.;Database=XrmMockup;Trusted_Connection=True;"
                ,
                RecreateDatabase = false
            };

            var crm = XrmMockup365.GetInstance(settings);

            var orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            var orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            var orgAdminService = crm.GetAdminService();

            user1 = crm.CreateUser(orgGodService, crm.RootBusinessUnit, new Guid("b6495cab-c047-e611-80d9-c4346badf080")).ToEntity<SystemUser>();

            acc1 = new Account()
            {
                Name = "Parent Account"
            };
            acc1.Id = orgAdminUIService.Create(acc1);

            acc2 = new Account()
            {
                Name = "Account",
                ParentAccountId = acc1.ToEntityReference()
            };
            acc2.Id = orgAdminUIService.Create(acc2);

            contact = new Contact()
            {
                FirstName = "Child Contact",
                LastName = "Test",
                ParentCustomerId = acc2.ToEntityReference()
            };
            contact.Id = orgAdminUIService.Create(contact);

            using (var context = new Xrm(orgAdminUIService))
            {
                var accountSet = context.AccountSet.ToList();

                var fetchedAccount1 = accountSet.FirstOrDefault(x => x.Id == acc1.Id);
                var fetchedAccount2 = accountSet.FirstOrDefault(x => x.Id == acc2.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);


                var req = new AssignRequest()
                {
                    Assignee = user1.ToEntityReference(),
                    Target = acc1.ToEntityReference()
                };
                orgAdminUIService.Execute(req);

                using (var context2 = new Xrm(orgAdminUIService))
                {
                     accountSet = context2.AccountSet.ToList();

                     fetchedAccount1 = accountSet.FirstOrDefault(x => x.Id == acc1.Id);
                     fetchedAccount2 = accountSet.FirstOrDefault(x => x.Id == acc2.Id);
                     fetchedContact = context2.ContactSet.FirstOrDefault(x => x.Id == contact.Id);

                }
            }           
        }
    }
}
