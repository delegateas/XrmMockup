Methods defined on XrmMockup
----------------------------
The following table defines which methods are available on XrmMockup instance

| Method name                                         | Description   
| :-                                                  |:-             
| ResetEnvironment()                                  | Wipes the database for all values created during each test, thus it preserves the standard values from the metadata.
| AddWorkflow(path)                                   | Adds a workflow from the definitions inside the workflows folder.
| ContainsEntity(entity)                              | Check if an entity with the logicalname, id and attributes of the given entity exists in the database. Useful for asserting if an entity has the expected value in the database.
| PopulateWith(entities)                              | Inserts the entity into the database without checking any values. Use this wisely as this could break your test is unexpected ways. Useful for testing against legacy or deprecated values.
| GetAdminService()                                   | Gets an organization service for the system administrator.
| GetConfigurableAdminService(settings)               | Gets an organization service for the system administrator, with the desired settings.
| CreateOrganizationService(id)                       | Creates a new organization service for the systemeruser with the given id.
| CreateConfigurableOrganizationService(id, settings) | Creates a new organization service for the systemeruser with the given id, and with the desired settings.
| CreateUser(service, businessUnit, securityRoles)    | Creates a new user with the given businessUnit and the securityRoles defined by the ids from the TypeDeclarations file. The user is created using the passed service.
| CreateUser(service, entity, securityRoles)          | Creates a new user according to the passed entity, which is required to have a businessUnit. The user gets the securityRoles defined by the ids from the TypeDeclarations file. The user is created using the passed service.
| CreateTeam(service, businessUnit, securityRoles)    | Creates a new team with the given businessUnit and the securityRoles defined by the ids from the TypeDeclarations file. The team is created using the passed service.
| CreateTeam(service, entity, securityRoles)          | Creates a new team according to the passed entity, which is required to have a businessUnit. The team gets the securityRoles defined by the ids from the TypeDeclarations file. The user is created using the passed service.
| AddTime(offset)                                     | Increases the time offset in XrmMockup by the amount defined in the timespan.
| AddTime(days)                                       | Increases the time offset in XrmMockup by the amount specified in the parameters.
| AddTime(days, hours)                                | Increases the time offset in XrmMockup by the amount specified in the parameters.
| AddTime(days, hours, minutes)                       | Increases the time offset in XrmMockup by the amount specified in the parameters.
| AddTime(days, hours, minutes, seconds)              | Increases the time offset in XrmMockup by the amount specified in the parameters.
| AddDays(days)                                       | Increases the time offset in XrmMockup by the amount specified in the number of days.
| AddHours(hours)                                     | Increases the time offset in XrmMockup by the amount specified in the number of hours.
     