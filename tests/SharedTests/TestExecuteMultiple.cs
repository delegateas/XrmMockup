using System;
using System.Collections.Generic;
using System.Linq;
using DG.Some.Namespace;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestExecuteMultiple : UnitTestBase
    {
        [TestMethod]
        public void TestExecuteMultipleAll()
        {
            var account1 = new Account { Name = "Account 1" };
            var account2 = new Account { Name = "Account 2" };
            var account3 = new Account { Name = "Account 3" };
            var account4 = new Account { Name = "Account 4" };
            var account5 = new Account { Name = "Account 5" };

            using (var context = new Xrm(orgAdminUIService))
            {
#region Execute Multiple with Results
                // Create an ExecuteMultipleRequest object.
                ExecuteMultipleRequest requestWithResults = new ExecuteMultipleRequest()
                {
                    // Assign settings that define execution behavior: continue on error, return responses. 
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    // Create an empty organization request collection.
                    Requests = new OrganizationRequestCollection()
                };

                // Create several (local, in memory) entities in a collection. 
                EntityCollection create = new EntityCollection()
                    {
                        EntityName = Account.EntityLogicalName,
                        Entities = {
                        account1,
                        account2,
                        account3,
                        account4,
                        account5
                    }
                    };

                // Add a CreateRequest for each entity to the request collection.
                foreach (var entity in create.Entities)
                {
                    CreateRequest createRequest = new CreateRequest { Target = entity };
                    requestWithResults.Requests.Add(createRequest);
                }

                // Execute all the requests in the request collection using a single web method call.
                ExecuteMultipleResponse responseWithResults =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithResults);

                Assert.AreEqual(5, responseWithResults.Responses.Count);

                // Display the results returned in the responses.
                foreach (var responseItem in responseWithResults.Responses)
                {
                    var response = responseItem.Response;
                    Assert.IsNotNull(response);
                    Assert.IsNull(responseItem.Fault);
                    Assert.IsInstanceOfType(response, typeof(CreateResponse));

                    // make this Test work, values are the same, but any is not allowed for some reason
                    //Assert.IsTrue(context.AccountSet.Any(x => x.Id.Equals(response.Results["id"])));

                }

#endregion Execute Multiple with Results

#region Execute Multiple with No Results

                ExecuteMultipleRequest requestWithNoResults = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to not continue after the first error is received
                    // and to not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                // Update the entities that were previously created.
                EntityCollection update = new EntityCollection()
                    {
                        EntityName = Account.EntityLogicalName,
                        Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = account2.Id },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = account4.Id },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                    };

                foreach (var entity in update.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithNoResults.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithNoResults =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithNoResults);

                Assert.AreEqual(0, responseWithNoResults.Responses.Count);

#endregion Execute Multiple with No Results


#region Execute Multiple with Continue On Error

                ExecuteMultipleRequest requestWithContinueOnError = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                // Update the entities but introduce some bad attribute values so we get errors.
                EntityCollection updateWithErrors = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = new Guid() },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = new Guid() },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithContinueOnError.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithContinueOnError =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithContinueOnError);

                Assert.AreEqual(2, responseWithContinueOnError.Responses.Count);
                Assert.IsTrue(responseWithContinueOnError.Responses.All(
                    x => x.Response == null && x.Fault != null && x.Fault.Message != null));


#endregion Execute Multiple with Continue On Error


#region Execute Multiple with Stop On Error

                ExecuteMultipleRequest requestWithStopOnError = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithStopOnError.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithStopOnError =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithStopOnError);

                Assert.AreEqual(1, responseWithStopOnError.Responses.Count);
                Assert.IsTrue(responseWithStopOnError.Responses.All(
                    x => x.Response == null && x.Fault != null && x.Fault.Message != null));


#endregion Execute Multiple with Stop On Error


#region Execute Multiple with Continue On Error And Returns

                ExecuteMultipleRequest requestWithContinueOnErrorAndReturns = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithContinueOnErrorAndReturns.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithContinueOnErrorAndReturns =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithContinueOnErrorAndReturns);

                Assert.AreEqual(5, responseWithContinueOnErrorAndReturns.Responses.Count);
                Assert.AreEqual(2, responseWithContinueOnErrorAndReturns.Responses.Count(
                    x => x.Response == null && x.Fault != null && x.Fault.Message != null));
                Assert.AreEqual(3, responseWithContinueOnErrorAndReturns.Responses.Count(
                    x => x.Response != null && x.Fault == null));


#endregion Execute Multiple with Continue On Error And Returns


#region Execute Multiple with Stop On Error And Returns

                ExecuteMultipleRequest requestWithStopOnErrorAndReturns = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithStopOnErrorAndReturns.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithStopOnErrorAndReturns =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithStopOnErrorAndReturns);

                Assert.AreEqual(2, responseWithStopOnErrorAndReturns.Responses.Count);
                Assert.AreEqual(1, responseWithStopOnErrorAndReturns.Responses.Count(
                    x => x.Response == null && x.Fault != null && x.Fault.Message != null));
                Assert.AreEqual(1, responseWithStopOnErrorAndReturns.Responses.Count(
                    x => x.Response != null && x.Fault == null));


#endregion Execute Multiple with Stop On Error And Returns
            }
        }

    }
}