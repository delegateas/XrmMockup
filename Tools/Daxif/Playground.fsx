(**
Playground
=================
*)

#load @"_Config.fsx"
open _Config
open System
open System.IO
open System.Collections.Generic
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Messages
open Microsoft.Xrm.Sdk.Query
open Microsoft.Crm.Sdk.Messages

open DG.Daxif
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open System.ServiceModel

let proxy = Env.lab4.connect().GetProxy()

let request = new AddToQueueRequest();
//request.SourceQueueId <- new Guid("ACEEE41E-0254-E611-80DC-C4346BADF018"); // Delegate Admin queue
request.DestinationQueueId <- new Guid("54329E24-0C79-E711-810C-5065F38BD431"); // Test user queue
request.Target <- new EntityReference("letter", new Guid("88CDB9F8-F146-E811-8131-3863BB365008")); // TEST LETTER
//request.Target <- new EntityReference("letter", new Guid("9BA0667B-FC46-E811-8131-3863BB365008")); // test user letter
//request.DestinationQueueId <- new Guid("ECEEE41E-0254-E611-80DC-C4346BADF018");  //new Guid("51F61EDF-C447-E611-80D9-C4346BADF080") // Delegated Admin queue;
let response = proxy.Execute(request)
response.Results