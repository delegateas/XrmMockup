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

let queue = new Entity("queue")
queue.Id <- proxy.Create(queue)
queue.Id

let letter = new Entity("letter")
letter.Id <- proxy.Create(letter)
letter.Id

let queueItem = new Entity("queueitem")
let queueItemId = Guid.NewGuid();
queueItem.Attributes.Add(new KeyValuePair<string, obj>("objectid", new EntityReference("letter", letter.Id)))
queueItem.Attributes.Add(new KeyValuePair<string, obj>("queueitemid", queueItemId))
queueItem.Attributes.Add(new KeyValuePair<string, obj>("queueid", new EntityReference("queue", queue.Id)))
queueItem.Id <- proxy.Create(queueItem)
queueItem.Id

let request = new RouteToRequest()
request.Target <- new EntityReference("queue", new Guid("ECEEE41E-0254-E611-80DC-C4346BADF018"))
request.QueueItemId <- queueItem.Id

proxy.Execute(request)