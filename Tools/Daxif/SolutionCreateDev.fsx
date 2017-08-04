(**
SolutionCreateDev
=================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.CreatePublisher(Env.dev, PublisherInfo.name, PublisherInfo.displayName, PublisherInfo.prefix)

Solution.Create(Env.dev, SolutionInfo.name, SolutionInfo.displayName, PublisherInfo.prefix)
