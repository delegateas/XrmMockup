Using XrmMockup for testing
---------------------------
First declare your new test class, let's decide we want to test plugins. 
We declare a test class called TestPlugins, which is an extention of the base test class. 
Note that XrmMockup works with any test framwork, just make sure to also change the import in the base test class.

	[lang=csharp]
	using DG.Some.Namespace; // The namespace used for our XrmContext early-bound types
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Microsoft.Xrm.Sdk.Query;
	namespace DG.XrmMockupTest {
		[TestClass]
		public class TestPlugins : UnitTestBase {

		}
	}

This class doesn't contain any tests yet, but first we need to understand how to connect to XrmMockup.
XrmMockup is to be considered a CRM instance. Therefore organization services are used to communicate with XrmMockup.
Initially XrmMockup is populated with your currencies, the root businessunit and the security roles for that businessunit and
a system administrator user. Therefore XrmMockup can give you the organization service for the standard user. In the base test
class this is stored as the orgAdminService, which is readily available to us.


Now to continue with the example, consider the need for creating a lead whenever an account is created. A test
for this functionality could be defined as follows.

	[lang=csharp]
	using DG.Some.Namespace; // The namespace used for our XrmContext early-bound types
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Microsoft.Xrm.Sdk.Query;
	namespace DG.XrmMockupTest {
		[TestClass]
		public class TestPlugins : UnitTestBase {

			[TestMethod]
			public void TestLeadsAreCreated() {
				using (var context = new Xrm(orgAdminService)) {
					var acc = new Account();
					acc.Name = "Some account";
					orgAdminService.Create(acc);
					var leads = context.LeadSet.ToList();
					Assert.IsTrue(leads.Count > 0);
				}
			}

		}
	}
	
Notice how a context is created with the service from XrmMockup, and this context
is used to check that some lead was created. In order to create the account, the 
organization service is used again, this time the create function is used, which functions
exactly like the regular organization service for a real CRM instance.


This test will fail until we create a plugin that triggers when the account has been created. This
plugin has been registered using [DAXIF](http://delegateas.github.io/Delegate.Daxif/), but XrmMockup fetches
all plugins from your CRM instance. The added benefit of using DAXIF with XrmMockup is that, the plugin registration
can be changed without needing to update the metadata with the MetadataGenerator.

	[lang=csharp]
    using System;
    using Microsoft.Xrm.Sdk;
	namespace DG.Some.Namespace {

    public class AccountPostPlugin : Plugin {

        public AccountPostPlugin() : base(typeof(AccountPostPlugin)) {
            RegisterPluginStep<Account>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }
            var service = localContext.OrganizationService;
            var rand = new Random();
            service.Create(new Lead() {
                Subject = "Some new lead " + rand.Next(0, 1000),
                ParentAccountId = 
					new Account(localContext.PluginExecutionContext.PrimaryEntityId)
					.ToEntityReference()
            });
        }
    }
}

Notice that the base class of the plugin has been added to the array of BasePluginTypes inside the bast test class. Now that
the plugin has been added, the test works, since XrmMockup automatically triggers the plugin after the account has been created.
For debug purposes, it is possible to set breakpoints in the definition of the plugin in order to figure out what is wrong with the code.
Every test could be writtin only using the organization service, but in order to make the developer's life easier, XrmMockup has a set
of [helpful methods](mockupreference.html)


Configuring XrmMockup
---------------------
In order to customize how you want to write your test, every XrmMockup instance can be configured.
The follow settings can be set in a XrmMockupSettings class

* BasePluginTypes: Takes a Type array, where each type is the base class for your plugins. 
This is used to find the assemblies of your plugins, in order to execute their logic when needed.

* CodeActivityInstanceType: Like the BasePluginTypes setting, this is used to find assemblies, but since
CodeActivites usually don't extend a common class, XrmMockup assummes that all your CodeActivites are in the same assembly.
Therefore CodeActivityInstanceType takes a single Type, which is the type of any of your CodeActivites, such that the assembly can be located.

* EnableProxyTypes: If set to true, XrmMockup will return early-bound types instead of simply return the standard entity type. The early-bound
types can be created using a proxytype generator like [XrmContext](http://delegateas.github.io/Delegate.XrmContext/).

* IncludeAllWorkflows: If set to true, XrmMockup will use all workflows defined in the workflows folder. Otherwise each test has to specify, which workflows
are to be used, thus enabling you to test workflows individually.


Configuring an organization service
-----------------------------------
In the base test class there is a variable called orgAdminUIService. This is also an 
organization service, but it is set to function as the operation would if done through the UI, 
thus standard values will be set on create, inactive records can't be modified and so on. This service
is created by passing a MockupServiceSetting to the service constructor.

	[lang=csharp]
	this.orgAdminUIService = crm.GetConfigurableAdminService(
		new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));

The MockupServiceSetting holds the following settings.

* TriggerProcesses: If set to true, plugins and workflows will trigger when using this service.

* SetUnsettableFields: If set to true, read-only fields can be set.

* ServiceRole: Sets whether this service should act as the SDK or UI.

