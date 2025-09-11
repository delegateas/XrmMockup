using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestThreadSafety : UnitTestBase
    {
        public TestThreadSafety(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestMultipleInstancesAreIndependent()
        {
            // Test that multiple instances created concurrently are truly independent
            var results = new ConcurrentBag<string>();
            var exceptions = new ConcurrentBag<Exception>();

            // Create multiple instances in parallel
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                int instanceNumber = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        TestIndependentInstance(instanceNumber, results);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Check for any exceptions
            Assert.Empty(exceptions);

            // Verify we got results from all instances
            Assert.Equal(5, results.Count);
        }

        private void TestIndependentInstance(int instanceNumber, ConcurrentBag<string> results)
        {
            // Each task gets its own instance using the same metadata as the test fixture
            var mockup = XrmMockup365.GetInstance(crm);
            var service = mockup.GetAdminService();

            // Create a contact with instance-specific data
            var contact = new Entity("contact");
            contact["firstname"] = $"Test{instanceNumber}";
            contact["lastname"] = $"User{instanceNumber}";
            
            var id = service.Create(contact);
            
            // Retrieve and verify the data is correct for this instance
            var retrieved = service.Retrieve("contact", id, new ColumnSet("firstname", "lastname"));
            
            Assert.Equal($"Test{instanceNumber}", retrieved["firstname"].ToString());
            Assert.Equal($"User{instanceNumber}", retrieved["lastname"].ToString());

            // Create multiple records to test database isolation
            for (int j = 0; j < 3; j++)
            {
                var additionalContact = new Entity("contact");
                additionalContact["firstname"] = $"Test{instanceNumber}_Extra{j}";
                additionalContact["lastname"] = $"User{instanceNumber}_Extra{j}";
                service.Create(additionalContact);
            }

            // Query all contacts in this instance - should only see our data
            var query = new QueryExpression("contact");
            query.ColumnSet = new ColumnSet("firstname", "lastname");
            query.Criteria.AddCondition("firstname", ConditionOperator.BeginsWith, $"Test{instanceNumber}");
            
            var contacts = service.RetrieveMultiple(query);
            
            // Should have exactly 4 contacts (1 main + 3 extras)
            Assert.Equal(4, contacts.Entities.Count);

            results.Add($"Instance {instanceNumber} completed successfully with {contacts.Entities.Count} contacts");
        }

        [Fact]
        public void TestResetEnvironmentWithNewArchitecture()
        {
            // Test that ResetEnvironment works correctly with the new architecture
            var mockup = XrmMockup365.GetInstance(crm);
            var service = mockup.GetAdminService();

            // Create some data
            var contact = new Entity("contact");
            contact["firstname"] = "Test";
            contact["lastname"] = "User";
            var id = service.Create(contact);

            // Verify data exists
            var retrieved = service.Retrieve("contact", id, new ColumnSet("firstname"));
            Assert.Equal("Test", retrieved["firstname"].ToString());

            // Reset environment
            mockup.ResetEnvironment();

            // Verify data is gone (should throw exception since record no longer exists)
            Assert.Throws<FaultException>(() => service.Retrieve("contact", id, new ColumnSet("firstname")));

            // Verify we can create new data after reset
            var newContact = new Entity("contact");
            newContact["firstname"] = "NewTest";
            newContact["lastname"] = "NewUser";
            var newId = service.Create(newContact);

            var newRetrieved = service.Retrieve("contact", newId, new ColumnSet("firstname"));
            Assert.Equal("NewTest", newRetrieved["firstname"].ToString());
        }

        [Fact] 
        public void TestMultipleInstancesHaveIndependentDatabases()
        {
            // Test that instances are truly independent by verifying data isolation
            // Create two instances using the same metadata as the test fixture
            var mockup1 = XrmMockup365.GetInstance(crm);
            var mockup2 = XrmMockup365.GetInstance(crm);

            // Test that they're truly independent by creating data in each
            var service1 = mockup1.GetAdminService();
            var service2 = mockup2.GetAdminService();

            var contact1 = new Entity("contact") { ["firstname"] = "Instance1" };
            var contact2 = new Entity("contact") { ["firstname"] = "Instance2" };

            var id1 = service1.Create(contact1);
            var id2 = service2.Create(contact2);

            // Each service should only see its own data
            var retrieved1 = service1.Retrieve("contact", id1, new ColumnSet("firstname"));
            var retrieved2 = service2.Retrieve("contact", id2, new ColumnSet("firstname"));

            Assert.Equal("Instance1", retrieved1["firstname"].ToString());
            Assert.Equal("Instance2", retrieved2["firstname"].ToString());

            // Cross-instance retrieval should fail
            Assert.Throws<FaultException>(() => service1.Retrieve("contact", id2, new ColumnSet("firstname")));
            Assert.Throws<FaultException>(() => service2.Retrieve("contact", id1, new ColumnSet("firstname")));
        }
    }
}