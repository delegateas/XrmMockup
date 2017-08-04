(**
SolutionPack
============
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility

// Unmanaged
Solution.Pack(
  Utility.addEndingToFilename Path.Daxif.unmanagedSolution "_packed", 
  Path.SolutionPack.customizationsFolder, 
  Path.SolutionPack.xmlMappingFile, 
  managed = false)

// Managed
Solution.Pack(
  Utility.addEndingToFilename Path.Daxif.managedSolution "_packed", 
  Path.SolutionPack.customizationsFolder, 
  Path.SolutionPack.xmlMappingFile, 
  managed = true
)
