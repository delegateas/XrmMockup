(**
SolutionUpdateTsContext
=====================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let xrmDefinitelyTyped = Path.toolsFolder ++ @"XrmDefinitelyTyped\XrmDefinitelyTyped.exe"
let xrmTypings = Path.webResourceProject ++ @"typings\XRM"
let jsLib = Path.webResourceFolder ++ "lib"

Solution.GenerateTypeScriptContext(Env.dev, xrmDefinitelyTyped, xrmTypings,
  solutions = [
    SolutionInfo.name
    ],
  entities = [
    // eg. "systemuser"
    ],
  extraArguments = [
    "web", ""
    "jsLib", jsLib
    ])