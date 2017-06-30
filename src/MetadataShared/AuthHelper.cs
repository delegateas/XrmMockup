using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Configuration;
using System.Net;


namespace DG.Tools.XrmMockup.Metadata
{
    internal static class AuthHelper {
        private static string username = ConfigurationManager.AppSettings["username"];
        private static string password = ConfigurationManager.AppSettings["password"];
        private static string domain = ConfigurationManager.AppSettings["domain"];
        private static AuthenticationProviderType provider = ToProviderType(ConfigurationManager.AppSettings["ap"]);
        private static string url = ConfigurationManager.AppSettings["url"];

        private static AuthenticationProviderType ToProviderType(string ap) {
            if (String.IsNullOrWhiteSpace(ap)) return AuthenticationProviderType.OnlineFederation;
            if (Enum.TryParse<AuthenticationProviderType>(ap, out AuthenticationProviderType parsedAp)) return parsedAp;
            throw new Exception($"Could not interpret AuthenticationProviderType argument given: '{ap}'");
        }

        internal static OrganizationServiceProxy GetOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            AuthenticationCredentials authCredentials) {
            var osp = serviceManagement.AuthenticationType == AuthenticationProviderType.ActiveDirectory 
                ? new OrganizationServiceProxy(serviceManagement, authCredentials.ClientCredentials)
                : new OrganizationServiceProxy(serviceManagement, authCredentials.SecurityTokenResponse);
            osp.Timeout = new TimeSpan(0, 59, 0);
            return osp;
        }

        internal static AuthenticationCredentials GetCredentials() {
            var ac = new AuthenticationCredentials();
            switch (provider) {
                case AuthenticationProviderType.ActiveDirectory:
                    ac.ClientCredentials.Windows.ClientCredential = new NetworkCredential(username, password, domain);
                    break;
                case AuthenticationProviderType.OnlineFederation:
                case AuthenticationProviderType.Federation:
                    ac.ClientCredentials.UserName.UserName = username;
                    ac.ClientCredentials.UserName.Password = password;
                    break;

                default:
                    throw new NotImplementedException("No valid authentification provider was used.");
            }
            return ac;
        }

        internal static OrganizationServiceProxy Authenticate() {
            var m = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(new Uri(url));
            var ac = m.Authenticate(GetCredentials());
            var proxy = GetOrganizationServiceProxy(m, ac);
            proxy.ServiceConfiguration.CurrentServiceEndpoint.EndpointBehaviors.Add(new ProxyTypesBehavior());
            return proxy;
        }

    }
}
