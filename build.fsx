// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
open Fake.VSTest
open System
open System.IO
//#if MONO
//#else
//#load "packages/build/SourceLink.Fake/tools/Fake.fsx"
//open SourceLink
//#endif

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

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A simulation engine that can mock a specific MS CRM instance. Useful for testing and debugging business logic."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = "A simulation engine that can mock a specific MS CRM instance. Useful for testing and debugging business logic."

let company = "Delegate A/S"

// List of author names (for NuGet package)
let authors = [ company; "Martin Kasban Tange"; "Magnus Gether Sørensen" ]

// Tags for your project (for NuGet package)
let tags = "microsoft crm xrm dynamics mockup fake c# csharp test testing unittest xrmmockup"

// File system information
let solutionFile  = "XrmMockup.sln"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Test.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "delegateas" 
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "Delegate.XrmMockup"

// The profile where the docs project is posted 
let docsGitHome = "https://github.com/delegateas"
// The name of the project on GitHub
let docsGitName = "delegateas.github.io"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|Shproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | f when f.EndsWith("shproj") -> Shproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

let vstestPaths = 
    [| @"[ProgramFilesX86]\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow";
       @"[ProgramFilesX86]\Microsoft Visual Studio\2017\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow";
       @"[ProgramFilesX86]\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow";
       @"[ProgramFilesX86]\Microsoft Visual Studio 12.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow";
       @"[ProgramFilesX86]\Microsoft Visual Studio 11.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow"|]

let vsTestExe = 
    if isMono then failwith "VSTest is not supported on the mono platform"
    else "vstest.console.exe"

let vstestToolPath =
       match tryFindFile vstestPaths vsTestExe with
       | Some path -> path
       | None -> ""

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title projectName
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion
          Attribute.Company company]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName </> "My Project") </> "AssemblyInfo.vb") attributes
        | Shproj -> ()
        )
)

// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    -- "src/**/*.shproj"
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) </> "bin/Release", "bin" </> (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! solutionFile
#if MONO
    |> MSBuildReleaseExt "" [ ("DefineConstants","MONO"); ("nowarn", "1591,0108") ] "Rebuild"
#else
    |> MSBuildReleaseExt "" [ ("nowarn", "1591,0108") ] "Rebuild"
#endif
    |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunTests" (fun _ ->
    !! testAssemblies
    |> VSTest (fun p ->
        { p with
            ToolPath = vstestToolPath
        })
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    Paket.Pack(fun p ->
        { p with
            OutputPath = "bin"
            Version = release.NugetVersion
            
            ReleaseNotes = toLines release.Notes})
)

Target "PublishNuget" (fun _ ->
    Paket.Push(fun p ->
        { p with
            ApiKey = getBuildParamOrDefault "delegateas-nugetkey" ""
            WorkingDir = "bin" })
)


// --------------------------------------------------------------------------------------
// Generate the documentation


let fakePath = "packages" </> "build" </> "FAKE" </> "tools" </> "FAKE.exe"
let fakeStartInfo script workingDirectory args fsiargs environmentVars =
    (fun (info: System.Diagnostics.ProcessStartInfo) ->
        info.FileName <- System.IO.Path.GetFullPath fakePath
        info.Arguments <- sprintf "%s --fsiargs -d:FAKE %s \"%s\"" args fsiargs script
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in environmentVars do
            setVar k v
        setVar "MSBuild" msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

/// Run the given buildscript with FAKE.exe
let executeFAKEWithOutput workingDirectory script fsiargs envArgs =
    let exitCode =
        ExecProcessWithLambdas
            (fakeStartInfo script workingDirectory "" fsiargs envArgs)
            TimeSpan.MaxValue false ignore ignore
    System.Threading.Thread.Sleep 1000
    exitCode

// Documentation
let buildDocumentationTarget fsiargs target =
    trace (sprintf "Building documentation (%s), this could take some time, please wait..." target)
    let exit = executeFAKEWithOutput "docs/tools" "generate.fsx" fsiargs ["target", target]
    if exit <> 0 then
        failwith "generating reference documentation failed"
    ()

Target "GenerateReferenceDocs" (fun _ ->
    buildDocumentationTarget "-d:RELEASE -d:REFERENCE" "Default"
)

let generateHelp' fail debug =
    let args =
        if debug then "--define:HELP"
        else "--define:RELEASE --define:HELP"
    try
        buildDocumentationTarget args "Default"
        traceImportant "Help generated"
    with
    | e when not fail ->
        traceImportant "generating help documentation failed"

let generateHelp fail =
    generateHelp' fail false

Target "GenerateHelp" (fun _ ->
    DeleteFile "docs/content/release-notes.md"
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp true
)

Target "GenerateHelpDebug" (fun _ ->
    DeleteFile "docs/content/release-notes.md"
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp' true true
)

Target "KeepRunning" (fun _ ->
    use watcher = !! "docs/content/**/*.*" |> WatchChanges (fun changes ->
         generateHelp' true true
    )

    traceImportant "Waiting for help edits. Press any key to stop."

    System.Console.ReadKey() |> ignore

    watcher.Dispose()
)

Target "GenerateDocs" DoNothing

let createIndexFsx lang =
    let content = """(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../../bin"

(**
F# Project Scaffold ({0})
=========================
*)
"""
    let targetDir = "docs/content" </> lang
    let targetFile = targetDir </> "index.fsx"
    ensureDirectory targetDir
    System.IO.File.WriteAllText(targetFile, System.String.Format(content, lang))

Target "AddLangDocs" (fun _ ->
    let args = System.Environment.GetCommandLineArgs()
    if args.Length < 4 then
        failwith "Language not specified."

    args.[3..]
    |> Seq.iter (fun lang ->
        if lang.Length <> 2 && lang.Length <> 3 then
            failwithf "Language must be 2 or 3 characters (ex. 'de', 'fr', 'ja', 'gsw', etc.): %s" lang

        let templateFileName = "template.cshtml"
        let templateDir = "docs/tools/templates"
        let langTemplateDir = templateDir </> lang
        let langTemplateFileName = langTemplateDir </> templateFileName

        if System.IO.File.Exists(langTemplateFileName) then
            failwithf "Documents for specified language '%s' have already been added." lang

        ensureDirectory langTemplateDir
        Copy langTemplateDir [ templateDir </> templateFileName ]

        createIndexFsx lang)
)

// --------------------------------------------------------------------------------------
// Release Scripts

//Target "ReleaseDocs" (fun _ ->
//    let tempDocsDir = "temp/gh-pages"
//    CleanDir tempDocsDir
//    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

//    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
//    StageAll tempDocsDir
//    Git.Commit.Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
//    Branches.push tempDocsDir
//)

//#load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"
//open Octokit

//Target "Release" (fun _ ->
//    let user =
//        match getBuildParam "github-user" with
//        | s when not (String.IsNullOrWhiteSpace s) -> s
//        | _ -> getUserInput "Username: "
//    let pw =
//        match getBuildParam "github-pw" with
//        | s when not (String.IsNullOrWhiteSpace s) -> s
//        | _ -> getUserPassword "Password: "
//    let remote =
//        Git.CommandHelper.getGitResult "" "remote -v"
//        |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
//        |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
//        |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

//    StageAll ""
//    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
//    Branches.pushBranch "" remote (Information.getBranchName "")

//    Branches.tag "" release.NugetVersion
//    Branches.pushTag "" remote release.NugetVersion

//    // release on github
//    createClient user pw
//    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
//    // TODO: |> uploadFile "PATH_TO_FILE"
//    |> releaseDraft
//    |> Async.RunSynchronously
//)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "RunTests"
//  ==> "GenerateReferenceDocs"
//  ==> "GenerateDocs"
//#if MONO
//#else
//  =?> ("SourceLink", Pdbstr.tryFind().IsSome )
//#endif
  ==> "NuGet"
  ==> "BuildPackage"
  ==> "All"
  //=?> ("ReleaseDocs",isLocalBuild)

"CleanDocs"
  ==> "GenerateHelp"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"

"CleanDocs"
  ==> "GenerateHelpDebug"

"GenerateHelpDebug"
  ==> "KeepRunning"

"BuildPackage"
  ==> "PublishNuget"
  //==> "Release"

//"ReleaseDocs"
  //==> "Release"
  
RunTargetOrDefault "Build"
