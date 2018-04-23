using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestQueue : UnitTestBase
    {
#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
        [TestMethod]
        public void TestAddPrincipalToQueuePrincipalMissingFails()
        {
            var queue = new Queue
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
            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var entity = new Entity("nonExistingLogicalName", Guid.NewGuid());

            var request = new AddPrincipalToQueueRequest()
            {
                QueueId = queue.Id,
                Principal = entity
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
        public void TestAddPrincipalToQueueIdEmptyFails()
        {
            var systemUser = new SystemUser
            {
                FirstName = "Test",
                LastName = "User"
            };

            systemUser.Id = orgAdminService.Create(systemUser);

            var request = new AddPrincipalToQueueRequest()
            {
                Principal = systemUser
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
        public void TestAddPrincipalToQueueNotFoundFails()
        {
            var systemUser = new SystemUser
            {
                FirstName = "Test",
                LastName = "User"
            };

            systemUser.Id = orgAdminService.Create(systemUser);

            var request = new AddPrincipalToQueueRequest()
            {
                Principal = systemUser,
                QueueId = Guid.NewGuid()
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
        public void TestAddPrincipalToQueuePrincipalLogicalNameWrongFails()
        {
            var account = new Account
            {
                Name = "Test Account"
            };

            account.Id = orgAdminService.Create(account);

            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var request = new AddPrincipalToQueueRequest()
            {
                Principal = account,
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
        public void TestAddPrincipalToQueuePrincipalNotFoundFails()
        {
            var systemUser = new SystemUser
            {
                FirstName = "Test",
                LastName = "User",
                Id = Guid.NewGuid()
            };

            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var request = new AddPrincipalToQueueRequest()
            {
                Principal = systemUser,
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
        public void TestAddPrincipalToQueueSuccess()
        {
            var systemUser = new SystemUser
            {
                FirstName = "Test",
                LastName = "User",
                Id = Guid.NewGuid()
            };

            systemUser.Id = orgAdminUIService.Create(systemUser);

            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var request = new AddPrincipalToQueueRequest()
            {
                Principal = systemUser,
                QueueId = queue.Id
            };

            var response = orgAdminUIService.Execute(request) as AddPrincipalToQueueResponse;
            Assert.IsNotNull(response);
        }
#endif
    }
}
