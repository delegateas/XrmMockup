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
    entities = [
        "businessunit"
        "systemuser"
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
      ],
    extraArguments = [
      "deprecatedprefix", "ZZ_"
      "sdkVersion", sdkVersion
      ])
)
