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

let lab4 = Env.lab4.connect().GetProxy()

let request = new RouteToRequest()
request.Target <- new EntityReference("queue", new Guid())
request.QueueItemId <- new Guid()

lab4.Execute(request)