


// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.NuGet.NuGet
open Fake.IO
open Fake.IO.Globbing.Operators
open System
open System.IO


Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let (++) x y = Path.Combine(x,y)

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "XrmMockup"
let projectPaths =
  [|
    "src" ++ "XrmMockup365"
  |]
let metaDataGeneratorProjectPaths =
  [|
    "src" ++ "MetadataGen" ++ "MetadataGenerator365"
    "src" ++ "MetadataGen" ++ "MetadataGenerator.Tool"
    "src" ++ "MetadataGen" ++ "MetadataGenerator.Core"
    "src" ++ "MetadataGen" ++ "MetadataGenerator.Context"
  |]
let solutionFile = "XrmMockup.sln"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A simulation engine that can mock a specific MS CRM instance. Useful for testing and debugging business logic."

let company = "Context& A/S"
let copyright = @"Copyright (c) Context& A/S 2017 - 2025"
// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = ReleaseNotes.parse (File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
// Excludes MetadataGenerator projects which have their own versioning via CHANGELOG.md
Target.create "AssemblyInfo" (fun _ ->
    !! "src/**/*.csproj"
    -- "src/MetadataGen/MetadataGenerator.Tool/*.csproj"
    -- "src/MetadataGen/MetadataGenerator.Context/*.csproj"
    -- "src/MetadataGen/MetadataGenerator.Core/*.csproj"
    -- "src/MetadataGen/MetadataGenerator.Tool.Tests/*.csproj"
    |> Seq.map Path.GetDirectoryName
    |> Seq.iter (fun projectPath ->
      let fileName = projectPath + "/AssemblyInfo.fs"
      AssemblyInfoFile.createCSharp fileName
        [
          AssemblyInfo.Title project
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Company company
          AssemblyInfo.Copyright copyright
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion
        ])
)

// Set version for MetadataGenerator projects from their CHANGELOG.md
Target.create "MetadataGeneratorVersion" (fun _ ->
    let result =
      Command.RawCommand("powershell", Arguments.OfArgs [| "-ExecutionPolicy"; "Bypass"; "-File"; "src/MetadataGen/scripts/Set-MetadataGeneratorVersion.ps1" |])
      |> CreateProcess.fromCommand
      |> Proc.run
    if result.ExitCode <> 0 then failwith "Failed to set MetadataGenerator version"
)

// --------------------------------------------------------------------------------------
// Setting up VS for building with FAKE
let commonBuild solution =
  let packArgs (defaults:MSBuild.CliArguments) = 
    { defaults with
        NoWarn = Some(["NU5100"])
        Properties = 
        [
          "Version", release.NugetVersion
          "ReleaseNotes", String.Join(Environment.NewLine, release.Notes)
        ] 
    }
  solution
  |> DotNet.build (fun buildOp -> 
    { buildOp with 
          MSBuildParams = packArgs buildOp.MSBuildParams
    })

// --------------------------------------------------------------------------------------
// Clean build results

Target.create "Clean" (fun _ ->
  projectPaths
  |> Array.append metaDataGeneratorProjectPaths
  |> Array.map (fun path -> path ++ "bin")
  |> Array.append [|"bin"; "temp"|]
  |> Shell.cleanDirs 
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    commonBuild solutionFile |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target.create "Test" (fun _ ->
  let result =
    Command.RawCommand("dotnet", Arguments.OfArgs [| "test" |])
    |> CreateProcess.fromCommand
    |> Proc.run

  if result.ExitCode <> 0 then failwith "Test failed"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package
Target.create "NuGet" (fun _ ->
  let packArgs (defaults:MSBuild.CliArguments) =
    { defaults with
        NoWarn = Some(["NU5100"])
        Properties =
        [
          "Version", release.NugetVersion
          "ReleaseNotes", String.Join(Environment.NewLine, release.Notes)
        ]
    }

  projectPaths
  |> Array.iter (fun projectPath ->
    DotNet.pack (fun def ->
      { def with
          NoBuild = true
          MSBuildParams = packArgs def.MSBuildParams
          OutputPath = Some("bin")

      }) projectPath)

  // Pack MetadataGenerator.Tool (version comes from csproj, set by MetadataGeneratorVersion target)
  DotNet.pack (fun def ->
    { def with
        Configuration = DotNet.BuildConfiguration.Release
        NoBuild = true
        OutputPath = Some("bin")
    }) ("src" ++ "MetadataGen" ++ "MetadataGenerator.Tool")
  )


Target.create "PublishNuGet" (fun _ ->
  let setNugetPushParams (defaults:NuGetPushParams) =
    { defaults with
        ApiKey = Fake.Core.Environment.environVarOrDefault "delegateas-nugetkey" "" |> Some
        Source = Some "https://api.nuget.org/v3/index.json"
    }
  let setParams (defaults:DotNet.NuGetPushOptions) =
      { defaults with
          PushParams = setNugetPushParams defaults.PushParams
       }

  let tryPushPackage nugetPath =
    try
      DotNet.nugetPush setParams nugetPath
      printfn "Successfully published %s" nugetPath
      Choice1Of3 nugetPath // Published
    with
    | ex when ex.Message.Contains("409") || ex.Message.Contains("already exists") ->
      printfn "Skipping %s - version already exists on NuGet" nugetPath
      Choice2Of3 nugetPath // Skipped
    | ex ->
      Choice3Of3 (nugetPath, ex) // Error

  let results =
    !!("bin/*.nupkg")
    |> Seq.map tryPushPackage
    |> Seq.toList

  let errors =
    results
    |> List.choose (function Choice3Of3 err -> Some err | _ -> None)

  let published =
    results
    |> List.choose (function Choice1Of3 path -> Some path | _ -> None)

  if not (List.isEmpty errors) then
    errors |> List.iter (fun (path, ex) -> printfn "Failed to publish %s: %s" path ex.Message)
    failwith "Some packages failed to publish"

  if List.isEmpty published then
    printfn "No new packages were published - all versions already exist"
  )



Target.create "CleanDocs" ignore
Target.create "GenerateReferenceDocs" ignore
Target.create "GenerateHelp" ignore
Target.create "GenerateHelpDebug" ignore
Target.create "KeepRunning" ignore
Target.create "GenerateDocs" ignore
Target.create "AddLangDocs" ignore
Target.create "BuildPackage" ignore

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

"Clean"
  ==> "AssemblyInfo"
  ==> "MetadataGeneratorVersion"
  ==> "Build"
  ==> "Test"
  ==> "NuGet"
  ==> "BuildPackage"
  ==> "All"
  
"BuildPackage"
  ==> "PublishNuget"
  
Target.runOrDefault "Build"
