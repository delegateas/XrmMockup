(**
SolutionPublishArg
=================

Configurable publish customization script, which is mainly intended for use by the build server.

Arguments:
  
  * `env=<name of environment>` (required)
*)

#load @"_Config.fsx"
open DG.Daxif
open DG.Daxif.Common.Utility

let args = fsi.CommandLineArgs |> parseArgs

let env =
  match args |> tryFindArg ["env"; "e"] with
  | Some arg -> Environment.Get arg
  | None -> failwithf "Missing 'env' argument needed to execute this script."

Solution.PublishAll(env)
