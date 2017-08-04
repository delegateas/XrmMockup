(**
DataExportSource
=================
*)
#load @"_Config.fsx"
open _Config
open DG.Daxif

let entities = 
  [|
    "account"
    "contact"
  |]

Data.Export(Env.dev, entities, Path.Daxif.dataFolder)