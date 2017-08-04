(**
SolutionExtract
===============
*)
#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.Extract(
  Path.Daxif.unmanagedSolution, 
  Path.SolutionPack.customizationsFolder, 
  Path.SolutionPack.xmlMappingFile, 
  Path.SolutionPack.projFile
)
