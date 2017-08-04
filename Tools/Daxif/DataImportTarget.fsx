(**
DataImportTarget
=================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Data.Import(Env.test, Path.Daxif.dataFolder, serialize = Serialize.JSON)
