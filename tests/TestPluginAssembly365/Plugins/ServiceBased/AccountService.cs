using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore.Extensions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPluginAssembly365.Plugins.ServiceBased
{
    public interface IAccountService
    {
        void HandleCreate();
    }

    public class AccountService : IAccountService
    {
        public AccountService(IPluginExecutionContext context, ITracingService tracingService, IOrganizationServiceFactory organizationServiceFactory, IManagedIdentityService managedIdentityService)
        {
            Service = organizationServiceFactory.CreateOrganizationService(context.UserId);
            Context = context;
            TracingService = tracingService;

            // Just to show that DI can inject this service as well
            _ = managedIdentityService;
        }

        private IOrganizationService Service { get; }
        private IPluginExecutionContext Context { get; }
        private ITracingService TracingService { get; }

        public void HandleCreate()
        {
            var account = Context.GetEntity<Account>(TracingService);
            if (!(account.AccountNumber?.StartsWith("DI-") ?? false))
            {
                return;
            }

            Service.Create(new Contact()
            {
                FirstName = "Injected",
                LastName = "Contact",
                ParentCustomerId = account.ToEntityReference()
            });
        }
    }
}
