(**
SolutionUpdateCustomContext
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common.Utility

let xrmContext = Path.toolsFolder ++ @"XrmContext\XrmContext.exe"
let targetPath vName = Path.solutionRoot ++ (sprintf @"tests\TestPluginAssembly%s\Context" vName)


crmNamesWithVersions
|> Seq.iter (fun (vName, sdkVersion) -> 
  Solution.GenerateCSharpContext(Env.dev, xrmContext, targetPath vName,
  solutions = [
    SolutionInfo.name
   ],
   entities = entitiesToGenerate,
   extraArguments = [
    "deprecatedprefix", "ZZ_"
    "sdkVersion", sdkVersion
    "lm", "\u2714\uFE0F: checkmark"
    ])
)



// Uncomment to generate XrmMockup metadata whenever you generate CSharp Context
//let xrmMockupMetadataGen = Path.metdataFolder ++ "MetadataGenerator365.exe"
//Solution.GenerateXrmMockupMetadata(Env.dev, xrmMockupMetadataGen, Path.metdataFolder,
//  solutions = [
//    SolutionInfo.name
//  ],
//  entities = [
//    // eg. "systemuser"
//    ],
//  extraArguments = [
//    ]
//)