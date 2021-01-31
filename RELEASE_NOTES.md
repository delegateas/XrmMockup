### 1.7.1 - 29 January 2021
* Added support for extensions to XrmMockup that tap into core

### 1.6.5 - 26 January 2021
* Added support for dictionary variables in workflows (@MattTrinder1)

### 1.6.4 - 26 January 2021
* Improved performance of fetching a row in the database (@MattTrinder1)

### 1.6.2 - 26 January 2021
* Security error messages now include the SecLib::AccessCheckEx2 tag (@MattTrinder1)

### 1.6.1 - 26 January 2021
* Plugins registered on AnyEntity are now handled in all cases (@MattTrinder1)

### 1.6.0 - 26 January 2021
* Plugin steps are now able have the same name but different assemblies (@MattTrinder1)

### 1.5.2 - 22 January 2021
* Fixed an error where assign cascaded to organization owned entities (@MattTrinder1)

### 1.5.1 - 22 January 2021
* Added support for RetrieveMetadataChangesRequest (@MattTrinder1)

### 1.5.0 - 22 January 2021
* Moved security checks to validation step in the pipeline (@MattTrinder1)

### 1.4.6 - 22 January 2021
* Added support for impersonating user in plugins (@MattTrinder1)

### 1.4.5 - 14 January 2021
* Fixed error when using entityreferences in linkentities.

### 1.4.4 - 14 January 2021
* Add formatted values for late-bound entities (@MattTrinder1)

### 1.4.3 - 14 January 2021
* Added calculated field calculation to retrieve multiple (@MattTrinder1)

### 1.4.2 - 14 January 2021
* Added roletemplates as actual records (@MattTrinder1)

### 1.4.1 - 14 January 2021
* Fixed error where tracing service throws an exception (@MattTrinder1)

### 1.4.0 - 25 November 2020
* CodeActivities are now allowed to extend abstract classes.
* Plugins without a base class will now be found if any class from the dll is referenced in the basePluginTypes setting.

### 1.3.3 - 25 November 2020
* Fixed issue where empty calculated fields caused retrieves to fail.
* Fixed issue with retrieving plugins registered on AnyEntity, that was not registered with DAXIF.

### 1.3.2 - 12 October 2020
* Fix issue in published nuget package where latest version of metadata generator was not added.

### 1.3.1 - 10 October 2020
* Fixed error with sales solution which had a role without a businessunit

### 1.3.0 - 07 September 2020
* Added support for RetrieveMultiple plugins
* Made some system attributes available in plugins

### 1.2.5 - 17 June 2020
* Added Connection String Authentication method

### 1.2.4 - 11 May 2020
* Fixed an issue in 2011 where default teams would fail to create

### 1.2.3 - 22 April 2020
* Fixed additional errors with orphaned security

### 1.2.2 - 08 April 2020
* Fixed generation error due to orphaned roleprivilege

### 1.2.1 - 24 March 2020
*  Added new method `AddSecurityRolesToPrinciple` for adding  security roles to a user or a team

### 1.2.0 - 21 February 2020
* Default teams are now added and managed in relation to businessunits.
* Custom workflows with optional parameters will now correctly be passed null values form workflows.
* The metadata generator will now fetch plugin images from selected solutions instead of a hard-coded solution.

### 1.1.1 - 18 February 2020
* Fix a bug with team security where members of an owner team had full access to any records the team owned
* Fixed a bug with XrmMockup365 when it was installed on projects without the tooling connector

### 1.1.0 - 11 February 2020
* Added better description when no default status reason exists
* Added support for client secret

### 1.0.0 - 31 January 2020
* It's happening!
* Inactive incidents can no longer be modified
* Fixed error with execution-ordering of sync and async plugins and workflows
* Excluded most standard workflows when generating metadata

### 0.13.3-beta - 12 November 2019
* Added TotalRecordCount to RetrieveMultipleResponse

### 0.13.1-beta - 10 September 2019
* Fixed an issue where CloseIncident request did not trigger plugins listening on update or set state of incident

### 0.13.0-beta - 18 July 2019
* XrmMockup is now back to include the changes introduced after 0.9.10
* Refactoring of the latest pull request to ensure that XrmMockup performance is not affected
* Fix issue in Retrieve Multiple request unable to handle missing entity alias on linked-entity and entity name on linked-entity filter criterias
* Re-added privilege check for Append and Append-To on create and update. This feature can be toggled of if necessary  
* Refactor internal check for user and team privilege
* Added new method `GetPrivilege` for retriving the privilege for a given user or team
* Added new method `HasPermission` that checks if a user or team has a given access to a record

### 0.12.0-beta - 10 April 2019
* Reverted to a stable version based on 0.9.10. Releases after 0.9.10 introduced severe performance degradation with a factor of 3-4 slower tests.
* Releases also introduced bugs and invalid CRM logic. Will work on reimplementing the changes added in later version, but will require time to ensure quality.
* Please use version between 0.9.10 and 0.12.0 if needed.
* added changes from release 0.11.1

### 0.11.1-beta - 01 April 2019
* Fixed a bug in calculated fields which expected caluclated fields based on lookup fields to always have a value.
* Added a new optional flag "BigBang" that simulates the entire universe.

### 0.11.0-beta - 04 March 2019
* Implemented FilterExpressions conditions across multiple LinkEntities (@aronmek)
* Changed the Created/Modified On fields to be Utc based. (@aronmek)
* Fixed error related to shared variables in plugincontext (@aronmek)

### 0.10.2-beta - 04 March 2019
* Fixed error when checking permissions in Deep privileges
* Now, XrmMockup also considers files in execution directory when looking for proxy types.

### 0.10.1-beta - 25 February 2019
* Added ConditionOperator NotLike
* Fixed error with Like, where both sides were wildcard

### 0.10.0-beta - 25 February 2019
* Added a check that ensures selected attributes exist on the entity.
* Fixed errors in MergeRequest.
* Fixed errors related to security checks for Parent Businessunit privilege.

### 0.9.11-beta - 18 February 2019
* Fixed error where default values for boolean and picklists were not set (thanks @majakubowski)
* Allows several plugin steps for the same plugin type (thanks @majakubowski)
* Now supports QueryByAttribute, RetrieveAttribute, RetrievePrincipalAccess & WhoAmI (thanks @majakubowski)
* Fixed error where null values was not handled correctly on create and update (thanks @majakubowski)
* Workflows: Added support for clear (thanks @majakubowski)
* Workflows: Added support for send email (thanks @majakubowski)
* Workflows: Various bug fixes and improvements (thanks @majakubowski)

### 0.9.10-beta - 16 November  2018
* Add support for formatted values in RetrieveMultiple

### 0.9.9-beta - 5 November  2018
* Add support for 'In' and 'NotIn' in conditional expression for query expressions

### 0.9.8-beta - September  2018
* Fixed a bug where members of a team could not read any records owned by a team in the same Business Unit og child Business Unit

### 0.9.7-beta - September 12 2018
* Added target type Guid, fixed bug when reference did not have value.

### 0.9.6-beta - August 16 2018
* Fixed a bug in delete request where a related entity only had N:N relationship.

### 0.9.5-beta - June 29 2018
* Fixed a bug when accessing ParentPluginContext on a PluginContext.

### 0.9.4-beta - June 22 2018
* Fixed a bug where registered plugins are disabled when XrmMockup is initialized first time

### 0.9.3-beta - June 13 2018
* Fixed a bug in ExecuteMultiple where IsFaulted was not set

### 0.9.2-beta - June 07 2018
* Fix bug in Owner Team where members could not access records that the team had access too

### 0.9.1-beta - June 07 2018
* Reverted handling of access teams
* Reverted TeamType for now as access teams are not supported
* CreateTeam now creates an owner team explicitly
* Fix bugs in AddMembersTeamRequest and RemoveMembersTeamRequest

### 0.9.0-beta - June 06 2018
* Add user priviliage check access through owner teams
* Update teams to handle Owner teams
* Updated CreateTeam methods to also excpect a team type
* Add new methods for adding or removing users from a team
* Fix bug when fetching security roles in Metadata Generator

### 0.8.1-beta - May 25 2018
* Added new method for registering additional plugins
* Added new method for disabling registered plugins
* It is now possible to test a selection of plugins
* XML documentation is added to nuget package

### 0.8.0-beta - May 23 2018
* Add new snapshot feature. It is now possible to take a snapshot of the database and swap between different snapshot making for faster initialization setup
* Fixed Assign Request to actually update the owner
* Fixed issue in cascading behaviour due to incorrect fetching of related entities.
* Implemented new requests:
 - IsValidStateTransition
 - CloseIncident
 - RetrieveExchangeRate
* Added filtering of metadata in RetrieveEntityRequest

### 0.7.3-beta - February 23 2018
* Changed dependency description of workflow dll for 365 version.

### 0.7.2-beta - February 21 2018
* Fixed bug where nuget package could be installed in projects running .NET <4.6.2 but did not actually work. Now nuget fails with an error if trying to install in projects running older versions of .NET framework.

### 0.7.1-beta - February 16 2018
* Fixed a bug where the primary entity id was not included beyond the first plugin execution in a chain.
* Fixed a bug where activitypointer was not included as a default entity in the metadata generator.

### 0.7.0-beta - February 12 2018
* Added handling of pre-validation plugins
* Added a new optional parameter in the XrmSettings for specifying the location of the directory containing the metadata files
* Changed the accessibility level for the Plugin class to be internal. The class was only intended for internal system plugins
* Added a new configuration parameter for MetadataGenerator to disable fetching of metadata of entities in local assemblies. This feature is now false as default
* Changed MetadataGenerator to also fetch entities from the list of specified solutions.
* The changes to which metadata generator means that the generation of metadata will take longer time to complete and might not contain the same entities as before. However, it is now more deterministic and consistent.

### 0.6.0-beta - Januar 12 2018
* Fixed an issue in SetEntityProperty where ExchangeRate could be null
* Updated live-debugging feature to work again that was previous lost in refactoring
* Fixed an issue where making two XrmMockup Instance resulted in EnableProxyType not working on second instance
* Updated target framwork to .NETFramework 4.6.2
* Fixed an issue where money field were incorrectly calculated

### 0.5.1-beta - December 21 2017
* Added a public getter for the base currency.
* Fixed error with Name property not being set on entityReferences.
* Fixed https://github.com/delegateas/XrmMockup/issues/26 concerning duplicate detection of N:N relations.

#### 0.5.0-beta - December 21 2017
* Added support for activity pointers
* Fixed bugs related to statecode and statuscode attributes

#### 0.4.4-beta - December 08 2017
* It's now illegal to create an organization service for a non-existing user
* Removed excessive console output

#### 0.4.3-beta - October 10 2017
* Added support for testing with data from CRM

#### 0.4.2-beta - September 21 2017
* Fixed https://github.com/delegateas/XrmMockup/issues/15 concerning implementing RetrieveOptionSetRequest
* Fixed https://github.com/delegateas/XrmMockup/issues/14 concerning adding formatted values to retrieved records

#### 0.4.1-beta - September 15 2017
* Fixed https://github.com/delegateas/XrmMockup/issues/17 with loading assemblies
* Fixed https://github.com/delegateas/XrmMockup/issues/16 with id being invalid in LINQ for context

#### 0.4.0-beta - September 01 2017
* Most classes have been moved to the namespace `DG.Tools.XrmMockup`.
* Changed the internal database implementation to act more like it's real counter-part.
* MetadataGenerator can now also consume command-line arguments, instead of only those in the .config file.
* Fixed handling of generic OrganizationRequests given a RequestName.
* Fixed a problem with `AddHours(...)` on XrmMockupBase.
* Fixed a problem with the stack trace being cut-off when an error occurred inside a plugin.
* Added a setting, which enables developers to choose to ignore requests, in case they aren't implemented, instead of throwing an exception.

#### 0.3.5-beta - July 19 2017
* Fixed bug with status transitions when they were disabled

#### 0.3.4-beta - July 19 2017
* Added support for status transitions
* Fixed invalid cast exception when creating an activity with ActivityParty set

#### 0.3.3-beta - July 11 2017
* Fixed queries ignoring certain conditions like `NotEqual` if attribute was null
* Fixed queries assuming that the linked entity-table already exists

#### 0.3.2-beta - July 7 2017
* Added support for `RetrieveEntityRequest`

#### 0.3.1-beta - July 4 2017
* A lot of minor code refactoring
* Rename of a few files
* Security roles GUIDs are now found at `DG.Tools.XrmMockup.SecurityRoles`
* Added `TypeDeclarations.cs` to NuGet package so it is automatically added to the target project

#### 0.3.0-beta - June 30 2017
* Changed to beta
* XrmMockupSettings now takes enumerables of CodeActivity and Plugin types
* Added `On`, `OnOrAfter`, and `OnOrBefore` as valid operators in workflow execution
* MetadataGenerator uses `appSettings` instead of the Visual Studio Settings designer
* Waiting workflows now properly reset during tests

#### 0.2.28-alpha - June 30 2017
* Bug fixes

#### 0.2.27-alpha - June 29 2017
* Orderby can now contain attributes that aren't selected in the query

#### 0.2.26-alpha - June 23 2017
* Added D365 version

#### 0.2.25-alpha - June 09 2017
* Fixed error with the ids of entities being set to the wrong attribute

#### 0.2.24-alpha - June 09 2017
* Associate and Dissasociate can now trigger plugins
* Intersect entities can now be retrieved if they have metadata

#### 0.2.23-alpha - May 24 2017
* Fixed error with plugins not triggering on win or lose

#### 0.2.22-alpha - May 24 2017
* Fixed error with plugins not triggering on win or lose

#### 0.2.21-alpha - May 24 2017
* Fixed error with ownerid not being set on createRequest
* Added support for Win and Lose Opportunity requests

#### 0.2.20-alpha - May 11 2017
* Fixed error with null values in entitycollection in workflow

#### 0.2.19-alpha - May 11 2017
* Fixed error with otherwise branches in workflows

#### 0.2.18-alpha - May 11 2017
* Fixed error with assembly loading

#### 0.2.17-alpha - May 10 2017
* Fixed errors with workflows

#### 0.2.16-alpha - May 05 2017
* Fixed errors with orderby in retrieveMultiple

#### 0.2.15-alpha - May 05 2017
* Plugins can now be registered from CRM in addition to DAXIF.

#### 0.2.14-alpha - May 04 2017
* Plugins no longer trigger on unsettable attributes
* Plugins listening to base currency attributes now trigger correctly 

#### 0.2.13-alpha - April 28 2017
* Bug fixes

#### 0.2.12-alpha - April 28 2017
* Bug fixes

#### 0.2.11-alpha - April 27 2017
* Added the option to choose whether a service acts as SDK or UI
* Bug fixes


#### 0.2.10-alpha - April 21 2017
* Metadata generation no longer requires the project to be build. Looks for proxy types in build directories.

#### 0.2.9-alpha - April 06 2017
* Bug fix

#### 0.2.8-alpha - March 24 2017
* Bug fix

#### 0.2.7-alpha - March 24 2017
* Made workflows and security roles modular
* Bug fixes

#### 0.2.6-alpha - March 23 2017
* MetadataGen now looks in debug dlls instead of release
* Bug fixes

#### 0.2.5-alpha - March 16 2017
* Bug fixes

#### 0.2.4-alpha - March 10 2017
* Added logic for workflows with related entities
* Changed the way businessunits and security roles declarations works
* Minor bug fixes

#### 0.2.3-alpha - March 2 2017
* Added logic for workflows with timeouts

#### 0.2.2-alpha - March 2 2017
* Now only workflows that needs to be triggered gets parsed

#### 0.2.1-alpha - March 2 2017
* No longer build for you, looks in release dir instead

#### 0.2.0-alpha - February 28 2017
* Support for security roles
* Workflow execution for crm 2016
* Bug fixes

#### 0.1.7-alpha - Januar 20 2017
* Minor bug fixes

#### 0.1.6-alpha - December 09 2016
* Fixed errors with precision in currencies and added support for calculated and rollup fields

#### 0.1.5-alpha - November 09 2016
* Added support for precision in currencies and fixed errors with standard currencies

#### 0.1.4-alpha - November 02 2016
* Fixed errors with preimage

#### 0.1.3-alpha - November 02 2016
* Added additional cases for currency base updates

#### 0.1.2-alpha - November 02 2016
* Fixed errors with metadata.fsx and assembly loading

#### 0.1.1-alpha - October 28 2016
* Added support for currencies and fixed errors with context

#### 0.1.0-alpha - October 10 2016
* Added metadata usage

#### 0.0.6-alpha - October 7 2016
* Fix to delete functionality

#### 0.0.5-alpha - September 22 2016
* More general fixes

#### 0.0.4-alpha - September 21 2016
* Retrieve and upsert with alternate key fix

#### 0.0.3-alpha - September 19 2016
* Retrieve with alternate key fix

#### 0.0.2-alpha - September 2016
* Early alpha build
