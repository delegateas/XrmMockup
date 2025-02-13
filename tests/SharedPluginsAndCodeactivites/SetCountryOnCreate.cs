using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;

namespace DG.Some.Namespace
{
    public class SetCountryOnCreate : Plugin
    {
        public SetCountryOnCreate() : base(typeof(SetCountryOnCreate))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;

            if (target == null)
            {
                throw new InvalidPluginExecutionException("Invalid request type.");
            }

            target["address2_country"] = "Denmark";
        }
    }
}
