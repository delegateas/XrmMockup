(**
WorkflowSyncDev
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let workflowDll = Path.solutionRoot ++ @"Workflow\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Workflow.dll"

Workflow.Sync(Env.dev, workflowDll, SolutionInfo.name)
