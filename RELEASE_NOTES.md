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
