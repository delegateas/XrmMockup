# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XrmMockup is a simulation engine that replicates a specific Dynamics 365/CRM instance locally, including all business logic such as plugins, workflows, and the security model. This allows for comprehensive testing and debugging of CRM customizations in isolation.

## Common Development Commands

### Building
- Build entire solution: `dotnet build`
- Build specific project: `dotnet build src/XrmMockup365/XrmMockup365.csproj`
- Build metadata generator: `dotnet build src/MetadataGen/MetadataGenerator365/MetadataGenerator365.csproj`

### Testing
- Run all tests: `dotnet test`
- Run specific test project: `dotnet test tests/XrmMockup365Test/XrmMockup365Test.csproj`
- Run tests for specific framework: `dotnet test -f net462` or `-f net6.0` or `-f net8.0`
- Run single test class: `dotnet test --filter "TestCreate"`
- Run single test method: `dotnet test --filter "TestCreateSimple"`

### NuGet Package Creation
- Create packages: `dotnet pack src/XrmMockup365/XrmMockup365.csproj`

## Architecture Overview

### Multi-Targeting Strategy
The project targets three frameworks: .NET Framework 4.6.2, .NET 6.0, and .NET 8.0. This is achieved through shared project files (.projitems) that contain common code compiled into each target framework.

### Shared Project Architecture
The codebase extensively uses MSBuild shared projects (.projitems files) to share code across multiple assemblies:

**Core Shared Projects:**
- **XrmMockupShared** - Contains the main simulation engine including:
  - Core.cs and XrmMockupBase - Base simulation functionality
  - MockupService classes - IOrganizationService implementation
  - Database/ - In-memory database abstraction (XrmDb, DbTable, DbRow)
  - Requests/ - Handler classes for each CRM operation (Create, Update, Delete, etc.)
  - Plugin/ - Plugin execution framework (PluginManager, MockupPlugin)
  - Security.cs - Privilege and security model implementation
  - Workflow/ - Workflow execution management

- **XrmMockupWorkflow** - Workflow execution engine and activity handlers

- **MetadataSkeleton** - Metadata structure definitions shared across metadata tools

- **MetadataShared** - Common utilities for metadata generation and processing

**Test Shared Projects:**
- **SharedTests** - Common test cases imported by test projects (TestCreate, TestSecurity, etc.)
- **SharedPluginsAndCodeactivites** - Test plugins and custom workflow activities used across tests

### Key Components

**XrmMockup365 Class:**
- Singleton pattern with instance caching based on settings
- Inherits from XrmMockupBase (in XrmMockupShared)
- Primary entry point for creating mock CRM instances

**Request Handler Pattern:**
- Each CRM operation (Create, Update, Delete, Retrieve, etc.) has a dedicated handler class in Requests/
- Handlers process OrganizationRequest objects and simulate CRM behavior
- Examples: CreateRequestHandler, UpdateRequestHandler, RetrieveMultipleRequestHandler

**Plugin Architecture:**
- PluginManager orchestrates plugin execution during requests
- MockupPlugin provides base functionality for plugin simulation
- Supports both synchronous and asynchronous plugin execution
- SystemPlugins/ contains built-in CRM plugins (DefaultBusinessUnitTeams, etc.)

**Database Abstraction:**
- XrmDb represents the in-memory CRM database
- DbTable and DbRow classes simulate CRM entities and records
- Supports relationships, metadata-driven validation, and security filtering

**Security Model:**
- Full simulation of CRM security including business units, security roles, and privileges
- Security.cs contains privilege checking logic
- Supports record-level security and field-level security

### Development Notes

**Framework-Specific Code:**
- Use `#if DATAVERSE_SERVICE_CLIENT` preprocessor directives for .NET 6.0+ specific code
- .NET Framework 4.6.2 uses Microsoft.CrmSdk.* packages
- .NET 6.0+ uses Microsoft.PowerPlatform.Dataverse.Client

**Test Structure:**
- Tests inherit from UnitTestBase class
- Use XrmMockupFixture for test setup and teardown
- SharedTests project contains the actual test implementations
- Test projects reference both SharedTests and SharedPluginsAndCodeactivites

**Metadata Generation:**
- MetadataGenerator365.exe extracts metadata from live CRM instances
- Generated metadata is consumed by XrmMockup for simulation
- TypeDeclarations.cs provides strongly-typed entity classes