using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;
using System.Linq;
using System.Threading;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Client;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    public class TestQueue : UnitTestBase
    {
#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
        [TestMethod]
        public void TestAddPrincipalToQueuePrincipalMissingFails()
        {
            Queue queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);


            var request = new AddPrincipalToQueueRequest()
            {
                QueueId = queue.Id
            };

            try
            {
                orgAdminUIService.Execute(request);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(FaultException));
            }
        }

        [TestMethod]
        public void TestAddPrincipalToQueuePrincipalLogicalNameNotFoundFails()
        {

        }

        [TestMethod]
        public void TestAddPrincipalToQueueIdEmptyFails()
        {

        }

        [TestMethod]
        public void TestAddPrincipalToQueueNotFoundFails()
        {

        }


        [TestMethod]
        public void TestAddPrincipalToQueuePrincipalLogicalNameWrongFails()
        {

        }

        [TestMethod]
        public void TestAddPrincipalToQueuePrincipalNotFoundFails()
        {

        }

        [TestMethod]
        public void TestAddPrincipalToQueueSuccess()
        {

        }
#endif
    }
}
