using DG.Some.Namespace;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Delegate.Lab4.Plugins.Helpers {
    public class AccountCurrencyBase : PluginNonDaxif {

        public AccountCurrencyBase() : base(typeof(AccountCurrencyBase)) { }

        public override void Execute(IServiceProvider provider) {
            var context = new LocalPluginContext(provider);

            var id = context.PluginExecutionContext.PrimaryEntityId;
            var acc = context.OrganizationService.Retrieve(Account.EntityLogicalName, id, new ColumnSet("name")).ToEntity<Account>();
            acc.Name += "UpdateBase";
            context.OrganizationService.Update(acc);
        }
    }
}
