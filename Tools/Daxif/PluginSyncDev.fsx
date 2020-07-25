(**
PluginSyncDev
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let pluginProjFile = Path.solutionRoot ++ @"Plugins\Plugins.csproj"
let pluginDll = Path.solutionRoot ++ @"Plugins\bin\Release\ILMerged.Delegate.XrmOrg.XrmSolution.Plugins.dll"

Plugin.Sync(Env.dev, pluginDll, pluginProjFile, SolutionInfo.name)
