#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAction : UnitTestBase
    {
        public TestAction(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestActionExecution()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var someString = "A some string";
                var entity = new Contact();
                entity.Id = orgAdminUIService.Create(entity);
                var req = new OrganizationRequest("ActionTest");
                req["SomeString"] = someString;
                req["Target"] = entity.ToEntityReference();
                var resp = orgAdminUIService.Execute(req);
                var leadRef = resp["CreatedEntity"] as EntityReference;
                var lead = orgAdminUIService.Retrieve(leadRef.LogicalName, leadRef.Id, new ColumnSet(true)) as Lead;
               Assert.Equal(someString, lead.LastName);
               Assert.Equal("From Action", lead.Subject);
            }
        }

        [Fact]
        public void TestActionParts()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var stringInput = "A string";
                var datetimeInput = DateTime.Now;
                var boolInput = true;
                var decimalInput = 12.3m;
                var floatInput = 412.2f;
                var intInput = 12;
                var moneyInput = new Money(123.7m);
                var pickListInput = new OptionSetValue(3);

                var entity = new Contact();
                entity.Id = orgAdminUIService.Create(entity);

                var req = new OrganizationRequest("Full action");
                req["StringInput"] = stringInput;
                req["Target"] = entity.ToEntityReference();
                var resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                var output = resp["Output"] as string;
               Assert.Equal(stringInput, output);

                req = new OrganizationRequest("Full action");
                req["DateTimeInput"] = datetimeInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(datetimeInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["BoolInput"] = boolInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(boolInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["DecimalInput"] = decimalInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(decimalInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["FloatInput"] = floatInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(floatInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["IntegerInput"] = intInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(intInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["PicklistInput"] = pickListInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(pickListInput.ToString(), output);

            }
        }
    }
}
#endif