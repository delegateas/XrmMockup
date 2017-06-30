using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DG.XrmMockupTest {

    [TestClass]
    public class ContextTest : UnitTestBase {

        [TestMethod]
        public void TestAddObject() {
            using (var context = new Xrm(orgAdminUIService)) {
                context.AddObject(new Lead());
                context.SaveChanges();
                Assert.IsNotNull(context.LeadSet.FirstOrDefault());
                context.ClearChanges();
            }
        }

        [TestMethod]
        public void TestContextIntersectEntity() {
            using (var context = new Xrm(orgAdminUIService)) {
                var acc1 = orgAdminUIService.Create(new Account());
                var acc2 = orgAdminUIService.Create(new Account());
                var acc3 = orgAdminUIService.Create(new Account());
                var con = orgAdminUIService.Create(new Contact());


                var relatedAccounts = new EntityReferenceCollection();
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, acc1));
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, acc2));
                relatedAccounts.Add(new EntityReference(Account.EntityLogicalName, acc3));

                Relationship relationship = new Relationship(dg_account_contact.EntityLogicalName);

                orgAdminUIService.Associate(Contact.EntityLogicalName, con, relationship, relatedAccounts);

                Assert.AreEqual(3, context.dg_account_contactSet.Where(x => x.contactid == con).ToList().Count());
            }
        }


        [TestMethod]
        public void TestUpdateObject() {
            using (var context = new Xrm(orgAdminUIService)) {
                var lead = new Lead();
                lead.FirstName = "Before";
                orgAdminUIService.Create(lead);
                var leadFromContext = context.LeadSet.First();
                leadFromContext.FirstName = "After";
                context.SaveChanges();

                Assert.AreEqual(lead.FirstName, context.LeadSet.First().FirstName);
                context.ClearChanges();

                context.Attach(leadFromContext);
                context.UpdateObject(leadFromContext);
                context.SaveChanges();

                Assert.AreEqual(leadFromContext.FirstName, context.LeadSet.First().FirstName);
                context.ClearChanges();
            }
        }
    }
}
