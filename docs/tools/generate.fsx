// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Web site location for the generated documentation
let website = "/Delegate.XrmContext"

let githubLink = "http://github.com/delegateas/Delegate.XrmContext"

// Specify more information about your project
let info =
  [ "project-name", "XrmContext"
    "project-author", "Delegate A/S"
    "project-summary", "Tool to generate early-bound .NET framework classes and enumerations for MS CRM Dynamics server-side coding."
    "project-page", "http://delegateas.github.io/"
    "project-github", githubLink
    "project-nuget", "http://nuget.org/packages/Delegate.XrmContext" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

open System.IO


#I "../../packages/FAKE/tools/"
#load "../../packages/FSharp.Formatting/FSharp.Formatting.fsx"
#r "NuGet.Core.dll"
#r "FakeLib.dll"
open Fake
open System
open System.IO
open System.Reflection
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat


// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "../../bin"
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files      = __SOURCE_DIRECTORY__ @@ "../files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/FSharp.Formatting/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

let version =
  let a = Assembly.LoadFile(Path.Combine(bin, @"XrmMockup2011/XrmMockup2011.dll"))
  let dots = a.GetName().Version.ToString().Split('.')
  String.Join(".", dots.[0..2])

// Where to look for *.csproj templates (in this order)
let layoutRootsAll = new System.Collections.Generic.Dictionary<string, string list>()
layoutRootsAll.Add("en",[ templates; formatting @@ "templates"
                          formatting @@ "templates/reference" ])
subDirectories (directoryInfo templates)
|> Seq.iter (fun d ->
                let name = d.Name
                if name.Length = 2 || name.Length = 3 then
                    layoutRootsAll.Add(
                            name, [templates @@ name
                                   formatting @@ "templates"
                                   formatting @@ "templates/reference" ]))


// Folder and static file
let rec clearDirectory path = 
    Directory.EnumerateFiles(path)
    |> Seq.iter File.Delete
    Directory.EnumerateDirectories(path)
    |> Seq.iter(fun f -> clearDirectory f; Directory.Delete f);;


let clearOutputDirectory () =
    ensureDirectory output
    printfn "Clearing output folder.."
    clearDirectory output

let copyFiles () =
  clearOutputDirectory ()
  CopyRecursive files output true |> Log "Copying file: "

let references =
  if isMono then
    // Workaround compiler errors in Razor-ViewEngine
    let d = RazorEngine.Compilation.ReferenceResolver.UseCurrentAssembliesReferenceResolver()
    let loadedList = d.GetReferences () |> Seq.map (fun r -> r.GetFile()) |> Seq.cache
    // We replace the list and add required items manually as mcs doesn't like duplicates...
    let getItem name = loadedList |> Seq.find (fun l -> l.Contains name)
    [ (getItem "FSharp.Core").Replace("4.3.0.0", "4.3.1.0")
      Path.GetFullPath "./../../packages/FSharp.Compiler.Service/lib/net40/FSharp.Compiler.Service.dll"
      Path.GetFullPath "./../../packages/FSharp.Formatting/lib/net40/System.Web.Razor.dll"
      Path.GetFullPath "./../../packages/FSharp.Formatting/lib/net40/RazorEngine.dll"
      Path.GetFullPath "./../../packages/FSharp.Formatting/lib/net40/FSharp.Literate.dll"
      Path.GetFullPath "./../../packages/FSharp.Formatting/lib/net40/FSharp.CodeFormat.dll"
      Path.GetFullPath "./../../packages/FSharp.Formatting/lib/net40/FSharp.MetadataFormat.dll" ]
    |> Some
  else None

let binaries =
    directoryInfo bin 
    |> subDirectories
    |> Array.map (fun d -> d.FullName @@ (sprintf "%s.dll" d.Name))
    |> List.ofArray

let libDirs =
    directoryInfo bin 
    |> subDirectories
    |> Array.map (fun d -> d.FullName)
    |> List.ofArray

let extendedInfo = ("root", root) :: ("project-version", version) :: info

// Build API reference from XML comments
let buildReference () =
  CleanDir (output @@ "reference")
  MetadataFormat.Generate
    ( binaries, output @@ "reference", layoutRootsAll.["en"],
      parameters = extendedInfo,
      sourceRepo = githubLink @@ "tree/master",
      sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
      ?assemblyReferences = references,
      publicOnly = true,libDirs = libDirs )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    let langSpecificPath(lang, path:string) =
        path.Split([|'/'; '\\'|], System.StringSplitOptions.RemoveEmptyEntries)
        |> Array.exists(fun i -> i = lang)
    let layoutRoots =
        let key = layoutRootsAll.Keys |> Seq.tryFind (fun i -> langSpecificPath(i, dir))
        match key with
        | Some lang -> layoutRootsAll.[lang]
        | None -> layoutRootsAll.["en"] // "en" is the default language
    Literate.ProcessDirectory
      ( dir, docTemplate, output @@ sub, replacements = extendedInfo,
        layoutRoots = layoutRoots,
        ?assemblyReferences = references,
        generateAnchors = true )
    

// Generate
clearOutputDirectory()
copyFiles()
#if HELP
buildDocumentation()
#endif
#if REFERENCE
buildReference()
#endif
