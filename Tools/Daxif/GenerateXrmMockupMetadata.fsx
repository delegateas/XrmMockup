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
  printfn "Executing for %s" vName
  Env.lab4.executeProcess(
    metadataGenPath vName,

    [ // Args for MetadataGenerator
      "out", targetPath vName
      "projectPath", targetPath vName
      "solutions", SolutionInfo.name
      "entities", entitiesToGenerate |> String.concat ","
    ],

    // Argument names for login details for the environment
    "url",
    "usr",
    "pwd",
    "ap",
    "dmn"
  )
)
