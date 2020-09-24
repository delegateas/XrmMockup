(**
View Extender
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

View.GenerateFiles(Env.dev, Path.daxifRoot,
  solutions = [|
    SolutionInfo.name
    |],
  entities = [|
    // eg. "systemuser"
    |])


#load @"viewExtenderData\_ViewGuids.fsx" 
#load @"viewExtenderData\_EntityRelationships.fsx" 
#load @"viewExtenderData\_EntityAttributes.fsx"

open ViewGuids
open EntityAttributes
open EntityRelationships

// define extensions e.g.:
// Views.Account.MyParentView
// |> View.Parse Env.dev
// |> View.ChangeId Views.Account.FirstChildView
// |> View.AddRelatedColumnFirst Account.Relations.Primarycontactid_ContactContactid [Contact.Fields.Address1_City] [400]
// |> View.UpdateView Env.dev