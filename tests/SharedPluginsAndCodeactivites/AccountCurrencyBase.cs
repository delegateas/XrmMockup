using DG.Some.Namespace;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Delegate.Lab4.Plugins.Helpers {
    public class AccountCurrencyBase : PluginNonDaxif {

        // Register when/how to execute
        public AccountCurrencyBase() : base(typeof(AccountCurrencyBase)) {
            

        }

        // Execute plugin logic
        public override void Execute(IServiceProvider provider) {
            var context = new LocalPluginContext(provider);
            // Insert logic here
            var id = context.PluginExecutionContext.PrimaryEntityId;
            var acc = context.OrganizationService.Retrieve(Account.EntityLogicalName, id, new Microsoft.Xrm.Sdk.Query.ColumnSet("name")).ToEntity<Account>();
            acc.Name += "UpdateBase";
            context.OrganizationService.Update(acc);
            
        }
    }
}
