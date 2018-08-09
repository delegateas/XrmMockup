(**
Config
======

Sets up all the necessary variables and functions to be used for the other scripts. 
*)
#r @"bin\Microsoft.Xrm.Sdk.dll"
#r @"bin\Delegate.Daxif.dll"
#r @"bin\Microsoft.Crm.Sdk.Proxy.dll"
#r @"bin\System.ServiceModel.dll"

open System
open Microsoft.Xrm.Sdk.Client
open DG.Daxif
open DG.Daxif.Common.Utility

(** 
CRM Environment Setup 
---------------------
*)
   
// Prompts the developer for a username and password the first time a script is run.
// It then stores these credentials in a local .daxif-file.
let creds = Credentials.FromKey("Lab4Creds")


module Env =
  let lab4 = 
    Environment.Create(
      name = "DelegateLab4",
      url = "https://delegatelab4.crm4.dynamics.com/XRMServices/2011/Organization.svc",
      ap = AuthenticationProviderType.OnlineFederation,
      creds = creds,
      args = fsi.CommandLineArgs
    )


let crmNamesWithVersions = 
  [
    "11", "5"
    "13", "6"
    "15", "7"
    "16", "8"
    "365", "8.2"
  ]


let entitiesToGenerate = 
  [
    "businessunit"
    "systemuser"
    "team"
    "transactioncurrency"
    "workflow"
    "account"
    "contact"
    "dg_bus"
    "dg_child"
    "dg_hasridden"
    "lead"
    "opportunity"
    "email"
    "activitypointer"
    "task"
    "opportunityclose"
    "dg_man"
    "calendar"
    "invoice"
    "queueitem"
    "incident"
    "incidentresolution"
    "queue"
  ]

(** 
CRM Solution Setup 
------------------
*)
module SolutionInfo =
  let name = @"Lab4"
  let displayName = @"Lab4"

module PublisherInfo =
  let prefix = @"dg"
  let name = @"delegateas"
  let displayName = @"Delegate A/S"


(** 
Path and project setup 
----------------------
*)
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

module Path =
  let daxifRoot = __SOURCE_DIRECTORY__
  let solutionRoot = daxifRoot ++ @"..\.."
  let toolsFolder = daxifRoot ++ @".."
  
  let webResourceProject = solutionRoot ++ @"WebResources"
  let webResourceFolder = 
    webResourceProject ++ @"src" ++ (sprintf "%s_%s" PublisherInfo.prefix SolutionInfo.name)


  /// Path information used by the SolutionPackager scripts
  module SolutionPack =
    let projName = "SolutionBlueprint"
    let projFolder = solutionRoot ++ projName
    let xmlMappingFile = projFolder ++ (sprintf "%s.xml" SolutionInfo.name)
    let customizationsFolder = projFolder ++ @"customizations"
    let projFile = projFolder ++ (sprintf @"%s.csproj" projName)

  /// Paths Daxif uses to store/load files
  module Daxif =
    let crmSolutionsFolder = daxifRoot ++ "solutions"
    let unmanagedSolution = crmSolutionsFolder ++ (sprintf "%s.zip" SolutionInfo.name)
    let managedSolution = crmSolutionsFolder ++ (sprintf "%s_managed.zip" SolutionInfo.name)

    let translationsFolder = daxifRoot ++ "translations"
    let metadataFolder = daxifRoot ++ "metadata"
    let dataFolder = daxifRoot ++ "data"
    let stateFolder = daxifRoot ++ "state"
    let associationsFolder = daxifRoot ++ "associations"
    let mappingFolder = daxifRoot ++ "mapping"
    let importedFolder = daxifRoot ++ "imported"
  