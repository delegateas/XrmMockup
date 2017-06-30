Configuring the MetadataGenerator
-------------------
First you need to replace the default values in the configuration file. 
The file is located at your-test-project-folder/Metadata/MetadataGenerator/MetadataGenerator**.exe.config

First specify the url to the Organization service for your crm instance.

	[lang=xml]
	<setting name="url" serializeAs="String">
		<value>https://your.url.for.crm.com/XRMServices/2011/Organization.svc</value>
	</setting>

Then specify the username of a system administrator user.
	
	[lang=xml]
	<setting name="username" serializeAs="String">
		<value>yourusername</value>
	</setting>

Then specify the password of the same user.

	[lang=xml]
	<setting name="password" serializeAs="String">
		<value>yourpassword</value>
	</setting>

Then specify the domain, or leave it blank.

	[lang=xml]
	<setting name="domain" serializeAs="String">
		<value>yourdomain</value>
	</setting>
	
Then specify with authenticationprovider your crm instance is using.
	
	[lang=xml]
	<setting name="authenticationprovider" serializeAs="String">
		<value>OnlineFederation</value>
	</setting>

In order to use an entity inside a test, you need list it in the entities setting. Note that if you use XrmContext, or another early-bound generation tool, 
then all the entities used there will automatically be added to this list, but be sure that you have build the
project before fetching the metadata, as the entities are fetched from the output path of your project.

	[lang=xml]
	<setting name="entities" serializeAs="Xml">
		<value>
			<ArrayOfString xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
				xmlns:xsd="http://www.w3.org/2001/XMLSchema">
				<string>entity</string>
				<string>anotherone</string>
			</ArrayOfString>
		</value>
	</setting>

Finally specify which solutions the metadata is to be fetched from, such that unwanted changes aren't included.

	[lang=xml]
	<setting name="solutions" serializeAs="Xml">
		<value>
			<ArrayOfString xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
				xmlns:xsd="http://www.w3.org/2001/XMLSchema">
				<string>aSolution	</string>
				<string>AnotherSolution</string>
			</ArrayOfString>
		</value>
	</setting>

Generating metadata
-------------------
Now that the configuration is specified, the MetadataGenerator executable can be run. If you're using XrmContext, or another early-bound generation tool, be sure
that the project has been build, in order to ensure that all the entities you're using have metadata in XrmMockup.

After running the MetadataGenerator, the following should be visible if the metadata was generated.

<img src="img/generatorpost.png" />


You should now notice that the metadata folder contains 2 new files, EntityMetadata.xml and TypeDeclarations.cs, as well as
2 new folders, SecurityRoles and Workflows.

The metadata only resembles the crm instance from when it was fetched, therefore the MetadataGenerator should be executed after each change
in your crm instance.


Defining a base test class
--------------------------
Now the metadata is setup and you're ready to test, but to make your life easier we present an example of a base test class, which
all your test classes should extend, such that it becomes easier to write tests. After creating the base test class you're ready to write test,
check out [how to use XrmMockup](usingmockup.html).

	[lang=csharp]
	using System;
	using DG.Tools;
	using Microsoft.Xrm.Sdk;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using static DG.Tools.XrmMockupBase;

	namespace DG.XrmMockupTest {

		[TestClass]
		public class UnitTestBase {
			private static DateTime _startTime { get; set; }

			protected IOrganizationService orgAdminUIService;
			protected IOrganizationService orgAdminService;
			protected static XrmMockup2016 crm;

			public UnitTestBase() {
				this.orgAdminUIService = crm.GetConfigurableAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
				this.orgAdminService = crm.GetAdminService(); 
			}

			[TestCleanup]
			public void TestCleanup() {
				crm.ResetEnvironment();
			}


			[AssemblyInitialize]
			public static void InitializeServices(TestContext context) {
				InitializeMockup(context);
			}

			public static void InitializeMockup(TestContext context) {
				crm = XrmMockup2016.GetInstance(new XrmMockupSettings {
					BasePluginTypes = new Type[] { typeof(Plugin) },
					CodeActivityInstanceType = typeof(AccountWorkflowActivity),
					EnableProxyTypes = true,
					IncludeAllWorkflows = true
				});
			}
		}
	} 

