(**
SolutionImportArg
=================

Configurable import script, which is mainly intended for use by the build server.

Arguments:
  
  * `env=<name of environment>` (required)
  * `dir=<path to solution folder>` (recommended for build server to point at artifacts)
  * `managed` (optional, defaults to unmanaged)

For example:
  
  * Managed import to test :  `fsi SolutionImportArg.fsx /env:Test /dir:"path/to/folder/with/solutions" managed`
  * Unmanaged import to dev:  `fsi SolutionImportArg.fsx /env:Development`
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility


let args = fsi.CommandLineArgs |> parseArgs

let env =
  match args |> tryFindArg ["env"; "e"] with
  | Some arg -> Environment.Get arg
  | None     -> failwithf "Missing 'env' argument needed to execute this script."

let solutionFolder =
  args |> tryFindArg ["dir"; "d"] ?| Path.Daxif.crmSolutionsFolder
  
let solutionZipPath = 
  match args |> tryFindArg ["managed"] with
  | Some _ -> "_managed"
  | None   -> ""
  |> sprintf "%s%s.zip" SolutionInfo.name
  |> (++) solutionFolder


Solution.Import(env, solutionZipPath, activatePluginSteps = true (*, extended = true *))
Seq.sortWith (fun a b -> System.String.Compare(a, b, System.StringComparison.InvariantCulture))