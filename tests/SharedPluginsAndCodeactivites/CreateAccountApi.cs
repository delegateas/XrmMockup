namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    public class CreateAccountApi : CustomAPI 
    {
        public CreateAccountApi()
            : base(typeof(CreateAccountApi)) 
        {
            RegisterCustomAPI("CreateAccount", Execute)
                .AddRequestParameter(new CustomAPIConfig.CustomAPIRequestParameter("Name", RequestParameterType.String))
                .AddResponseProperty(new CustomAPIConfig.CustomAPIResponseProperty("Name", RequestParameterType.String));
        }

        protected void Execute(LocalPluginContext localContext) 
        {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var name = localContext.PluginExecutionContext.InputParameters["Name"] as string;

            service.Create(new Entity("account")
            {
                ["name"] = name
            });

            localContext.PluginExecutionContext.OutputParameters["Name"] = name;
        }
    }
}
