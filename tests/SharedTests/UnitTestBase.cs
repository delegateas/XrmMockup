using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using System.Activities;
using DG.Tools;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Workflow;

namespace DG.XrmMockupTest
{

    [TestClass]
    public class UnitTestBase
    {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
        protected IOrganizationService orgRealDataService;
#if XRM_MOCKUP_TEST_2011
        protected static XrmMockup2011 crm;
        protected static XrmMockup2011 crmRealData;
#elif XRM_MOCKUP_TEST_2013
        protected static XrmMockup2013 crm;
        protected static XrmMockup2013 crmRealData;
#elif XRM_MOCKUP_TEST_2015
        protected static XrmMockup2015 crm;
        protected static XrmMockup2015 crmRealData;
#elif XRM_MOCKUP_TEST_2016
        protected static XrmMockup2016 crm;
        protected static XrmMockup2016 crmRealData;
#elif XRM_MOCKUP_TEST_365
        protected static XrmMockup365 crm;
        protected static XrmMockup365 crmRealData;
#endif

        public UnitTestBase()
        {
            this.orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            this.orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            this.orgAdminService = crm.GetAdminService();
            if (crmRealData != null)
                this.orgRealDataService = crmRealData.GetAdminService();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            crm.ResetEnvironment();
        }


        [AssemblyInitialize]
        public static void InitializeServices(TestContext context)
        {
            InitializeMockup(context);
        }

        public static void InitializeMockup(TestContext context)
        {
            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { typeof(DG.Some.Namespace.Plugin), typeof(PluginNonDaxif) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                ExceptionFreeRequests = new string[] { "TestWrongRequest" },
            };

#if XRM_MOCKUP_TEST_2011
            crm = XrmMockup2011.GetInstance(settings);
#elif XRM_MOCKUP_TEST_2013
            crm = XrmMockup2013.GetInstance(settings);
            RegisterWorkflowCodeActivitiesFromExternalAssemblies(crm, new List<Type>() { typeof(CodeActivity) });
#elif XRM_MOCKUP_TEST_2015
            crm = XrmMockup2015.GetInstance(settings);
            RegisterWorkflowCodeActivitiesFromExternalAssemblies(crm, new List<Type>() { typeof(CodeActivity) });
#elif XRM_MOCKUP_TEST_2016
            crm = XrmMockup2016.GetInstance(settings);
            RegisterWorkflowCodeActivitiesFromExternalAssemblies(crm, new List<Type>() { typeof(CodeActivity) });
#elif XRM_MOCKUP_TEST_365
            crm = XrmMockup365.GetInstance(settings);
            RegisterWorkflowCodeActivitiesFromExternalAssemblies(crm, new List<Type>() { typeof(CodeActivity) });
#endif

            RegisterPluginsFromExternalAssemblies(crm, settings.BasePluginTypes.ToList());





            try
            {
                var realDataSettings = new XrmMockupSettings
                {
                    BasePluginTypes = settings.BasePluginTypes,
                    CodeActivityInstanceTypes = settings.CodeActivityInstanceTypes,
                    EnableProxyTypes = settings.EnableProxyTypes,
                    IncludeAllWorkflows = settings.IncludeAllWorkflows,
                    ExceptionFreeRequests = settings.ExceptionFreeRequests,
                    OnlineEnvironment = new Env
                    {
                        providerType = AuthenticationProviderType.OnlineFederation,
                        uri = "https://exampleURL/XRMServices/2011/Organization.svc",
                        username = "exampleUser",
                        password = "examplePass"
                    }
                };
#if XRM_MOCKUP_TEST_2011
                crmRealData = XrmMockup2011.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_2013
                crmRealData = XrmMockup2013.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_2015
                crmRealData = XrmMockup2015.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_2016
                crmRealData = XrmMockup2016.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_365
                crmRealData = XrmMockup365.GetInstance(realDataSettings);
#endif
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// allows you to register additional workflow activities from a list of compiled assemblies, present at a given location
        /// </summary>
        /// <param name="crm">the mocked instance to register the plugins against</param>
        /// <param name="matchingTypes">base types to match against eg. CodeActivity / AccountWorkflowActivity</param>
        private static void RegisterWorkflowCodeActivitiesFromExternalAssemblies(XrmMockupBase crm, List<Type> matchingTypes)
        {
            string workFlowAssembliesPath = WebConfigurationManager.AppSettings["CompiledWorkflowAssembliesPath"];
            if (!string.IsNullOrEmpty(workFlowAssembliesPath))
            {
                var workflowTypes = GetLoadableTypesFromAssembliesInPath(workFlowAssembliesPath, matchingTypes);

                foreach (Type type in workflowTypes)
                {
                    crm.AddCodeActivityTrigger(type);
                }
            }
        }

        /// <summary>
        /// allows you to register additional plugin steps from a list of compiled assemblies, present at a given location
        /// </summary>
        /// <param name="crm">the mocked instance to register the plugins against</param>
        /// <param name="matchingTypes">base types to match against eg. Plugin</param>
        private static void RegisterPluginsFromExternalAssemblies(XrmMockupBase crm, List<Type> matchingTypes)
        {
            string pluginAssembliesPath = WebConfigurationManager.AppSettings["CompiledPluginAssembliesPath"];
            if (!string.IsNullOrEmpty(pluginAssembliesPath))
            {
                var pluginTypes = GetLoadableTypesFromAssembliesInPath(pluginAssembliesPath, matchingTypes);

                foreach (Type type in pluginTypes)
                {
                    crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, type);
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypesFromAssembliesInPath(string path, List<Type> typesToLoad)
        {
            List<Type> types = new List<Type>();
            try
            {
                DirectoryInfo d = new DirectoryInfo(path);
                FileInfo[] Files = d.GetFiles("*.dll"); //get libraries

                foreach (FileInfo file in Files)
                {
                    if (!IsRestrictedLibrary(file.Name))
                    {
                        var assembly = Assembly.LoadFrom(file.FullName);
                        var allTypes = assembly.GetTypes();
                        foreach (Type type in allTypes)
                        {
                            if (typesToLoad.Any(p => p == type.BaseType))
                            {
                                types.Add(type);
                            }
                        }
                    }
                }

                return types;
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Give the ability to ignore loading types from any library matching the filter below
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private static bool IsRestrictedLibrary(string fullName)
        {
            if (fullName.StartsWith("Microsoft."))
            {
                return true;
            }

            return false;
        }
    }
}
