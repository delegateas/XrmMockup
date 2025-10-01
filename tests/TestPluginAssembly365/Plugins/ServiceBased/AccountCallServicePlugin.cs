using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace TestPluginAssembly365.Plugins.ServiceBased
{
    public abstract class DIPlugin : Plugin
    {
        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<IAccountService, AccountService>();
        }
    }

    public class AccountCallServicePlugin : DIPlugin
    {
        public AccountCallServicePlugin()
        {
            RegisterStep<Account, IAccountService>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                service => service.HandleCreate());
        }
    }
}
