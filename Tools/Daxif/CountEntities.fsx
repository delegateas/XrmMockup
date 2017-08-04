(**
Count entities
===============
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.Count(Env.dev, SolutionInfo.name)
