using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Tools.XrmMockup {
    internal abstract class RequestHandler {
        protected DataMethods dataMethods;
        protected Core core;
        internal string RequestName;

        internal RequestHandler(Core core, ref DataMethods dataMethods, string RequestName) {
            this.dataMethods = dataMethods;
            this.core = core;
            this.RequestName = RequestName;
        }

        internal Boolean HandlesRequest(string RequestName) {
            return this.RequestName == RequestName;
        }
        abstract internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef);

        public static T MakeRequest<T>(OrganizationRequest req) where T : OrganizationRequest {
            var typedReq = Activator.CreateInstance<T>();
            if (req.RequestName != typedReq.RequestName) {
                throw new MockupException($"Incorrect request type made. The name '{req.RequestName}' does not match '{typedReq.RequestName}'.");
            }
            typedReq.Parameters = req.Parameters;
            return typedReq;
        }

    }
}
