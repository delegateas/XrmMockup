(**
SolutionUpdateCustomContext
=====================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let metadataGenPath vName = 
  Path.solutionRoot ++ 
    (sprintf @"src\MetadataGen\MetadataGenerator%s\bin\Release\MetadataGenerator%s.exe" vName vName)

let targetPath vName = Path.solutionRoot ++ (sprintf @"tests\XrmMockup%sTest\Metadata" vName)


crmNamesWithVersions
|> Seq.iter (fun (vName, _) ->

  Env.lab4.executeProcess(
    metadataGenPath vName,

    [ // Args for MetadataGenerator
      "out", targetPath vName
      "projectPath", targetPath vName
      "solutions", SolutionInfo.name
      "entities", 
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
        ] |> String.concat ","
    ],

    // Argument names for login details for the environment
    "url",
    "usr",
    "pwd",
    "ap",
    "dmn"
  )
)
