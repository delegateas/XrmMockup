using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Configuration;
using System.Net;


namespace DG.Tools.XrmMockup.Metadata
{
    internal class AuthHelper {
        private string username = ConfigurationManager.AppSettings["username"];
        private string password = ConfigurationManager.AppSettings["password"];
        private string domain = ConfigurationManager.AppSettings["domain"];
        private AuthenticationProviderType provider = ToProviderType(ConfigurationManager.AppSettings["ap"]);
        private string url = ConfigurationManager.AppSettings["url"];

        public AuthHelper(
            string url, 
            string username, 
            string password, 
            AuthenticationProviderType provider = AuthenticationProviderType.OnlineFederation, 
            string domain = null) 
        {
            this.url = url;
            this.username = username;
            this.password = password;
            this.provider = provider;
            this.domain = domain;
        }

        public AuthHelper(
            string url,
            string username,
            string password,
            string provider = "OnlineFederation",
            string domain = null) 
            : this(url, username, password, ToProviderType(provider), domain) {
        }

        private static AuthenticationProviderType ToProviderType(string ap) {
            if (String.IsNullOrWhiteSpace(ap)) return AuthenticationProviderType.OnlineFederation;
            if (Enum.TryParse(ap, out AuthenticationProviderType parsedAp)) return parsedAp;
            throw new Exception($"Could not interpret AuthenticationProviderType argument given: '{ap}'");
        }

        internal OrganizationServiceProxy GetOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            AuthenticationCredentials authCredentials) {
            var osp = serviceManagement.AuthenticationType == AuthenticationProviderType.ActiveDirectory 
                ? new OrganizationServiceProxy(serviceManagement, authCredentials.ClientCredentials)
                : new OrganizationServiceProxy(serviceManagement, authCredentials.SecurityTokenResponse);
            osp.Timeout = new TimeSpan(0, 59, 0);
            return osp;
        }

        internal AuthenticationCredentials GetCredentials() {
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

        internal OrganizationServiceProxy Authenticate() {
            var m = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(new Uri(url));
            var ac = m.Authenticate(GetCredentials());
            var proxy = GetOrganizationServiceProxy(m, ac);
            proxy.ServiceConfiguration.CurrentServiceEndpoint.EndpointBehaviors.Add(new ProxyTypesBehavior());
            return proxy;
        }

    }
}
