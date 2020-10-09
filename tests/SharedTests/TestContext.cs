using System.Linq;
using Microsoft.Xrm.Sdk;
using Xunit;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    public class ContextTest : UnitTestBase
    {
        public ContextTest(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAddObject()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                context.AddObject(new Lead());
                context.SaveChanges();
                Assert.NotNull(context.LeadSet.FirstOrDefault());
                context.ClearChanges();
            }
        }

        [Fact]
        public void TestContextIntersectEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc1 = orgAdminUIService.Create(new Account());
                var acc2 = orgAdminUIService.Create(new Account());
                var acc3 = orgAdminUIService.Create(new Account());
                var con = orgAdminUIService.Create(new Contact());


                var relatedAccounts = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, acc1),
                    new EntityReference(Account.EntityLogicalName, acc2),
                    new EntityReference(Account.EntityLogicalName, acc3)
                };

                Relationship relationship = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Contact.EntityLogicalName, con, relationship, relatedAccounts);

                Assert.Equal(3, context.dg_account_contactSet.Where(x => x.contactid == con).ToList().Count());
            }
        }


        [Fact]
        public void TestUpdateObject()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var lead = new Lead
                {
                    FirstName = "Before"
                };
                orgAdminUIService.Create(lead);
                var leadFromContext = context.LeadSet.First();
                leadFromContext.FirstName = "After";
                context.SaveChanges();

                Assert.Equal(lead.FirstName, context.LeadSet.First().FirstName);
                context.ClearChanges();

                context.Attach(leadFromContext);
                context.UpdateObject(leadFromContext);
                context.SaveChanges();

                Assert.Equal(leadFromContext.FirstName, context.LeadSet.First().FirstName);
                context.ClearChanges();
            }
        }
    }
}
