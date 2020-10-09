using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;
using DG.XrmContext;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestDelete : UnitTestBase
    {
        public TestDelete(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void DeleteTest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var guid = orgAdminUIService.Create(new Contact());

                var firstRetrieve = orgAdminUIService.Retrieve<Contact>(guid, null);
                Assert.NotNull(firstRetrieve);

                orgAdminUIService.Delete(Contact.EntityLogicalName, guid);

                try
                {
                    orgAdminUIService.Retrieve<Contact>(guid, null);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

            }
        }

        [Fact]
        public void TestDeleteNonExistingEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                try
                {
                    orgAdminUIService.Delete(Contact.EntityLogicalName, Guid.NewGuid());
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

            }
        }
    }

}
