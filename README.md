# XrmMockup

XrmMockup is a tool which simulates ***your*** exact Dynamics 365/CRM instance locally including all of it's logic in the form of workflows, plugins, and the security model.

This means you can write tests towards XrmMockup as though it was the real CRM instance, and check that it behaves as expected with all of your customizations.

You can test (and debug!) the entirety of the functionality you have implemented in your CRM system, with all 
the plugins and workflows automatically being run inside your test cases if necessary -- along with all the standard out-of-the-box CRM logic. 

Find more in-depth information and examples in the [wiki](https://github.com/delegateas/XrmMockup/wiki).

## Features

* Simulates a target Dynamics 365/CRM instance by reading a local copy of its metadata.
* Supports executing most of the standard requests via an `IOrganizationService`.
* Automatically executes of plugins and workflows during requests which trigger them.
* Enforces the security model of CRM, thus allowing you to test your security roles and business unit setup.
* Enables changing the time of the internal clock of XrmMockup during tests, which enables testing of time-based events (i.e waiting workflows or other time-sensitive business logic).


## Still in beta

This project is still in beta, so beware that breaking changes may occur more frequently, as we continue to develop XrmMockup and it's usage.
