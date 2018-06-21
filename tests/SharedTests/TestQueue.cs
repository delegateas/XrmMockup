using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestQueue : UnitTestBase
    {
#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
        #region AddPrincipalToQueueRequest
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
        #endregion

        #region PickFromQueueRequest
        [TestMethod]
        public void TestPickFromQueueQueueItemIdEmptyFails()
        {
            var orgUser = new SystemUser();

            orgUser.Id = orgAdminUIService.Create(orgUser);

            var request = new PickFromQueueRequest
            {
                WorkerId = orgUser.Id
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
        public void TestPickFromQueueWorkerIdEmptyFails()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference()
            };

            queueItem.Id = orgAdminUIService.Create(queueItem);

            var request = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
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
        public void TestPickFromQueueWorkerIdNotFoundFails()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference()
            };

            queueItem.Id = orgAdminUIService.Create(queueItem);

            var orgUser = new SystemUser
            {
                Id = Guid.NewGuid()
            };

            var request = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
                WorkerId = orgUser.Id
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
        public void TestPickFromQueueQueueItemNotFoundFails()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference(),
                Id = Guid.NewGuid()
            };

            var orgUser = new SystemUser();

            orgUser.Id = orgAdminUIService.Create(orgUser);

            var request = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
                WorkerId = orgUser.Id
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
        public void TestPickFromQueueSuccess()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference()
            };

            queueItem.Id = orgAdminUIService.Create(queueItem);

            var orgUser = new SystemUser();

            orgUser.Id = orgAdminUIService.Create(orgUser);

            var request = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
                WorkerId = orgUser.Id
            };

            var response = orgAdminUIService.Execute(request) as PickFromQueueResponse;

            Assert.IsNotNull(response);

            var newQueueItem = orgAdminUIService.Retrieve(QueueItem.EntityLogicalName, queueItem.Id, new ColumnSet(true)) as QueueItem;
            Assert.IsNotNull(newQueueItem);
            Assert.IsTrue(queueItem.WorkerId.Id == orgUser.Id);
        }
        #endregion

        #region ReleaseToQueueRequest
        [TestMethod]
        public void TestReleaseToQueueMissingQueueItemIdFails()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference(),
                Id = Guid.NewGuid()
            };

            var request = new ReleaseToQueueRequest
            {
                QueueItemId = queueItem.Id
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
        public void TestReleaseToQueueQueueItemNotFoundFails()
        {
            var request = new ReleaseToQueueRequest();

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
        public void TestReleaseToQueueSuccess()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference()
            };

            queueItem.Id = orgAdminUIService.Create(queueItem);

            var request = new ReleaseToQueueRequest
            {
                QueueItemId = queueItem.Id
            };

            var response = orgAdminUIService.Execute(request) as ReleaseToQueueResponse;

            Assert.IsNotNull(response);
        }
        #endregion

        #region RemoveFromQueueRequest
        [TestMethod]
        public void TestRemoveFromQueueMissingQueueItemIdFails()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference(),
                Id = Guid.NewGuid()
            };

            var request = new RemoveFromQueueRequest
            {
                QueueItemId = queueItem.Id
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
        public void TestRemoveFromQueueQueueItemNotFoundFails()
        {
            var request = new RemoveFromQueueRequest();

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
        public void TestRemoveFromQueueSuccess()
        {
            var queue = new Queue();

            queue.Id = orgAdminUIService.Create(queue);

            var letter = new Letter
            {
                Description = "Test letter"
            };

            letter.Id = orgAdminUIService.Create(letter);

            var queueItem = new QueueItem
            {
                QueueId = queue.ToEntityReference(),
                ObjectId = letter.ToEntityReference()
            };

            queueItem.Id = orgAdminUIService.Create(queueItem);

            var request = new RemoveFromQueueRequest
            {
                QueueItemId = queueItem.Id
            };

            var response = orgAdminUIService.Execute(request) as RemoveFromQueueResponse;

            Assert.IsNotNull(response);
        }
        #endregion

        #region RetrieveUserQueuesRequest
        [TestMethod]
        public void TestRetrieveUserQueuesMissingUserIdFails()
        {
            var request = new RetrieveUserQueuesRequest();
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
        public void TestRetrieveUserQueuesRetrivesPrivateQueues()
        {
            var orgUser = new SystemUser();

            orgUser.Id = orgAdminUIService.Create(orgUser);

            var privateQueue = new Queue
            {
                QueueViewType = Queue_QueueViewType.Private,
                OwnerId = orgUser.ToEntityReference()
            };

            privateQueue.Id = orgAdminUIService.Create(privateQueue);

            var publicQueue = new Queue
            {
                QueueViewType = Queue_QueueViewType.Public,
                OwnerId = orgUser.ToEntityReference()
            };

            publicQueue.Id = orgAdminUIService.Create(publicQueue);

            var request = new RetrieveUserQueuesRequest
            {
                UserId = orgUser.Id
            };

            var response = orgAdminUIService.Execute(request) as RetrieveUserQueuesResponse;

            Assert.IsNotNull(response);
            Assert.IsTrue(response.EntityCollection.Entities.Contains(privateQueue));
            Assert.IsFalse(response.EntityCollection.Entities.Contains(publicQueue));
        }

        [TestMethod]
        public void TestRetrieveUserQueuesRetrivesPublicQueues()
        {

        }
        #endregion
#endif
        #region AddToQueueRequest

        [TestMethod]
        public void TestAddToQueueTargetMissingFails()
        {
            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = queue.Id
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
        public void TestAddToQueueTargetMetadataNotFoundFails()
        {
            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var target = new EntityReference("wronglogicalname", Guid.NewGuid());

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = queue.Id,
                Target = target
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
        public void TestAddToQueueDestinationQueueIdMissingFails()
        {
            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var request = new AddToQueueRequest()
            {
                Target = letter.ToEntityReference()
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
        public void TestAddToQueueTargetNotValidForQueueFails()
        {
            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "This is a test queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            queue.Id = orgAdminUIService.Create(queue);

            var account = new Account();

            account.Id = orgAdminUIService.Create(account);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = queue.Id,
                Target = account.ToEntityReference()
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
        public void TestAddToQueueSourceQueueNotFoundFails()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
                SourceQueueId = Guid.NewGuid()
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
        public void TestAddToQueueSourceQueueItemNotFoundFails()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var sourceQueue = new Queue
            {
                Name = "Source Queue",
                Description = "This is a Source queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            sourceQueue.Id = orgAdminUIService.Create(sourceQueue);

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
                SourceQueueId = sourceQueue.Id
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
        public void TestAddToQueueTargetNotFoundFails()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var letter = new Letter
            {
                Id = Guid.NewGuid()
            };

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
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
        public void TestAddToQueueDestinationNotFoundFails()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private,
                Id = Guid.NewGuid()
            };

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
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
        public void TestAddToQueuePropertiesMetadataNotFoundFails()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private,
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var queueItemProperties = new Entity("wronglogicalname");

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
                QueueItemProperties = queueItemProperties
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
        public void TestAddToQueuePropertiesNotQueueItemFails()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private,
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var queueItemProperties = new Account();

            queueItemProperties.Id = orgAdminUIService.Create(queueItemProperties);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
                QueueItemProperties = queueItemProperties
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
        public void TestAddToQueueNoSourceQueueSuccess()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private,
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var queueItemProperties = new QueueItem();

            queueItemProperties.Id = orgAdminUIService.Create(queueItemProperties);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                Target = letter.ToEntityReference(),
                QueueItemProperties = queueItemProperties
            };

            var response = orgAdminUIService.Execute(request) as AddToQueueResponse;
            Assert.IsNotNull(response);
            Assert.IsInstanceOfType(response.QueueItemId, typeof(Guid));
            Assert.IsTrue(response.QueueItemId != Guid.Empty);

            var queueItem = orgAdminUIService.Retrieve(QueueItem.EntityLogicalName, response.QueueItemId, new ColumnSet(true)) as QueueItem;
            Assert.IsNotNull(queueItem);
            Assert.IsTrue(queueItem.QueueId.Id == destinationQueue.Id);
        }

        [TestMethod]
        public void TestAddToQueueSourceQueueSuccess()
        {
            var destinationQueue = new Queue
            {
                Name = "Destination Queue",
                Description = "This is a destination queue.",
                QueueViewType = Queue_QueueViewType.Private,
            };

            destinationQueue.Id = orgAdminUIService.Create(destinationQueue);

            var letter = new Letter();

            letter.Id = orgAdminUIService.Create(letter);

            var sourceQueue = new Queue
            {
                Name = "Source Queue",
                Description = "This is a source queue.",
                QueueViewType = Queue_QueueViewType.Private
            };

            sourceQueue.Id = orgAdminUIService.Create(sourceQueue);

            var queueItem = new QueueItem
            {
                ObjectId = letter.ToEntityReference(),
                QueueId = sourceQueue.ToEntityReference()
            };

            queueItem.Id = orgAdminUIService.Create(queueItem);

            var request = new AddToQueueRequest()
            {
                DestinationQueueId = destinationQueue.Id,
                SourceQueueId = sourceQueue.Id,
                Target = letter.ToEntityReference()
            };

            var response = orgAdminUIService.Execute(request) as AddToQueueResponse;
            Assert.IsNotNull(response);
            Assert.IsTrue(response.QueueItemId == queueItem.Id);

            var newQueueItem = orgAdminUIService.Retrieve(QueueItem.EntityLogicalName, response.QueueItemId, new ColumnSet(true)) as QueueItem;
            Assert.IsNotNull(newQueueItem);
            Assert.IsTrue(newQueueItem.QueueId.Id == destinationQueue.Id);
        }

        #endregion
    }
}
