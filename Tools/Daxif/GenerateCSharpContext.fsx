(**
SolutionUpdateCustomContext
=====================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let xrmContext = Path.toolsFolder ++ @"XrmContext\XrmContext.exe"
let targetPath vName = Path.solutionRoot ++ (sprintf @"tests\TestPluginAssembly%s\Context" vName)


crmNamesWithVersions
|> Seq.iter (fun (vName, sdkVersion) ->

  Solution.GenerateCSharpContext(Env.lab4, xrmContext, targetPath vName,
    solutions = [
      SolutionInfo.name
      ],
    entities = entitiesToGenerate,
    extraArguments = [
      "deprecatedprefix", "ZZ_"
      "sdkVersion", sdkVersion
      ])
)
