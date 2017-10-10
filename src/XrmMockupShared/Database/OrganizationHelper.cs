using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace DG.Tools.XrmMockup {
    public class OrganizationHelper {

        private Uri _uri;
        private AuthenticationProviderType _ap;
        private string _userName;
        private string _password;
        private string _domain;

        public OrganizationHelper() {
            _uri = new Uri(GetConnectionString("CrmUri") ?? "");
            _ap = (AuthenticationProviderType)Enum.Parse(typeof(AuthenticationProviderType),
                GetConnectionString("CrmAp"));
            _userName = GetConnectionString("CrmUsr");
            _password = GetConnectionString("CrmPwd");
            _domain = GetConnectionString("CrmDmn");
        }

        public OrganizationHelper(Uri uri, AuthenticationProviderType ap, string userName, string password, string domain = null) {
            _uri = uri;
            _ap = ap;
            _userName = userName;
            _password = password;
            _domain = domain;
        }

        private string GetConnectionString(string name) {
            return ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
        }

        public OrganizationServiceProxy GetServiceProxy() {
            var proxy = GetServiceProxyInternal();
            proxy.ServiceConfiguration.CurrentServiceEndpoint.Behaviors.Add(new ProxyTypesBehavior());
            proxy.Timeout = new TimeSpan(1, 0, 0);
            return proxy;
        }

        private OrganizationServiceProxy GetServiceProxyInternal() {
            var management = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(_uri);
            var ac = management.Authenticate(GetCredentials(management, _ap));

            switch (_ap) {
                case AuthenticationProviderType.ActiveDirectory:
                    return new OrganizationServiceProxy(management, ac.ClientCredentials);
                default:
                    return new OrganizationServiceProxy(management, ac.SecurityTokenResponse);
            }
        }

        private AuthenticationCredentials GetCredentials<TService>(IServiceManagement<TService> service, AuthenticationProviderType endpointType) {
            AuthenticationCredentials authCredentials = new AuthenticationCredentials();

            switch (endpointType) {
                case AuthenticationProviderType.ActiveDirectory:
                    authCredentials.ClientCredentials.Windows.ClientCredential =
                        new System.Net.NetworkCredential(_userName, _password, _domain);
                    break;
                case AuthenticationProviderType.LiveId:
                    authCredentials.ClientCredentials.UserName.UserName = _userName;
                    authCredentials.ClientCredentials.UserName.Password = _password;
                    authCredentials.SupportingCredentials = new AuthenticationCredentials();
                    authCredentials.SupportingCredentials.ClientCredentials =
                        Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
                    break;
                default: // For Federated and OnlineFederated environments.                    
                    authCredentials.ClientCredentials.UserName.UserName = _userName;
                    authCredentials.ClientCredentials.UserName.Password = _password;
                    // For OnlineFederated single-sign on, you could just use current UserPrincipalName instead of passing user name and password.
                    // authCredentials.UserPrincipalName = UserPrincipal.Current.UserPrincipalName;  // Windows Kerberos

                    // The service is configured for User Id authentication, but the user might provide Microsoft
                    // account credentials. If so, the supporting credentials must contain the device credentials.
                    if (endpointType == AuthenticationProviderType.OnlineFederation) {
                        IdentityProvider provider = service.GetIdentityProvider(authCredentials.ClientCredentials.UserName.UserName);
                        if (provider != null && provider.IdentityProviderType == IdentityProviderType.LiveId) {
                            authCredentials.SupportingCredentials = new AuthenticationCredentials();
                            authCredentials.SupportingCredentials.ClientCredentials =
                                Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
                        }
                    }

                    break;
            }

            return authCredentials;
        }
    }
}
