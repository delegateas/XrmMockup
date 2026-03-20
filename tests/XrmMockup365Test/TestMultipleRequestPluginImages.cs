using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestMultipleRequestPluginImages : UnitTestBase
    {
        public TestMultipleRequestPluginImages(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void UpdateMultiple_TriggersUpdatePostOpPlugin_WithPostImages()
        {
            // Arrange: create contacts
            var contact1Id = orgGodService.Create(new Contact { FirstName = "Alice", LastName = "One" });
            var contact2Id = orgGodService.Create(new Contact { FirstName = "Bob", LastName = "Two" });

            // Act: UpdateMultiple - this previously threw NullReferenceException
            var updateMultiple = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities =
                    {
                        new Contact(contact1Id) { Description = "updated-1" },
                        new Contact(contact2Id) { Description = "updated-2" },
                    }
                }
            };
            orgAdminService.Execute(updateMultiple);

            // Assert: the PostImagePlugin should have created a Task for each contact
            var query = new QueryExpression("task") { ColumnSet = new ColumnSet(true) };
            var tasks = orgAdminService.RetrieveMultiple(query);

            var pluginTasks = tasks.Entities
                .Where(t => t.GetAttributeValue<string>("subject")?.Contains("PostImagePlugin executed") == true)
                .ToList();

            Assert.True(pluginTasks.Count >= 2, $"Expected at least 2 plugin tasks, but found {pluginTasks.Count}");
            Assert.All(pluginTasks, t => Assert.Contains("HasPostImage=True", t.GetAttributeValue<string>("subject")));
        }

        [Fact]
        public void CreateMultiple_TriggersCreatePostOpPlugin_DoesNotCrash()
        {
            // Arrange & Act: CreateMultiple should not crash even though
            // there's a PostImage plugin registered on Update (not Create)
            var createMultiple = new CreateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities =
                    {
                        new Contact { FirstName = "Charlie", LastName = "Three" },
                        new Contact { FirstName = "Diana", LastName = "Four" },
                    }
                }
            };

            // Should not throw
            var response = (CreateMultipleResponse)orgAdminService.Execute(createMultiple);
            Assert.NotNull(response);
        }

        [Fact]
        public void SingleUpdate_TriggersUpdateMultiplePlugin_DoesNotCrash()
        {
            // Arrange: create a contact, then do a single Update
            // The SetCityOnCreateUpdateMultiple plugin is registered on UpdateMultiple
            // and should fire via the Single->Multiple cross-trigger
            var contactId = orgGodService.Create(new Contact { FirstName = "Eve", Address2_City = "Berlin" });

            // Act: single Update triggers cross-fire to UpdateMultiple
            orgAdminService.Update(new Contact(contactId) { FirstName = "Eve-Updated" });

            // Assert: the UpdateMultiple plugin should have set city to Copenhagen
            var retrieved = Contact.Retrieve(orgAdminService, contactId);
            Assert.Equal("Copenhagen", retrieved.Address2_City);
        }

        [Fact]
        public void SingleCreate_TriggersCreateMultiplePlugin_DoesNotCrash()
        {
            // Act: single Create triggers cross-fire to CreateMultiple
            // The SetCityOnCreateUpdateMultiple plugin is registered on CreateMultiple
            var contactId = orgAdminService.Create(new Contact { FirstName = "Frank" });

            // Assert: the CreateMultiple plugin should have set city to Copenhagen
            var retrieved = Contact.Retrieve(orgAdminService, contactId);
            Assert.Equal("Copenhagen", retrieved.Address2_City);
        }
    }
}
