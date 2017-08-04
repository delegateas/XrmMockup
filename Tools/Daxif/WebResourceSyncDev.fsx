(**
WebResouresSyncDev
=================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

WebResource.Sync(Env.dev, Path.webResourceFolder, SolutionInfo.name)
