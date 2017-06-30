(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#r "../../bin/XrmContext/Microsoft.Xrm.Sdk.dll"
#r "../../bin/XrmContext/XrmContext.exe"

(**
Usage of XrmContext 
===========================

The executable can be used from a command prompt, but also directly from code, 
if you want the generation of the context files to be a part of your 
workflow.


Arguments
-------------------------------

The arguments are similar to those given to the [CrmSvcUtil][crmsvcutil] tool,
but with a few additions. Here is the full list of arguments:

| Argument            | Short-hand  | Description   
| :-                  | :-          |:-             
| url                 |             | URL to the Organization.svc
| username            | u, usr      | CRM Username
| password            | p, pwd      | CRM Password
| domain              | d, dmn      | Domain to use for CRM
| ap                  |             | Authentication Provider Type
| out                 | o           | Output directory for the generated files.
| solutions           | ss          | Comma-separated list of solutions names. Generates code for the entities found in these solutions.
| entities            | es          | Comma-separated list of logical names of the entities it should generate code for. This is additive with the entities gotten via the ***solutions*** argument.
| namespace           | ns          | The namespace for the generated code. The default is the global namespace.
| servicecontextname  | scn         | The name of the generated organization service context class. If no value is supplied, no service context is created.
| deprecatedprefix    | dp          | Marks all attributes with the given prefix in their display name as deprecated.
| sdkversion          | sv          | The version of `CrmSdk.CoreAssemblies` used by your library. Automatically finds the matching version from CRM if nothing is specified.
| intersect           | is          | Comma-separated list of named semicolon-separated lists of entity logical names that should be intersected. Example: <br /> <code>ICustomer:account;contact, IActivity: phonecall;email;task</code>
                      
You can also view this list of arguments using the "***/help***" argument.
                      
### Configuration file

If no arguments are given to the executable, it will check if there is an configuration file in the same folder with arguments it can use instead.

If you want to generate a dummy configuration file to use for arguments, you can use the "***/genconfig***" argument.<br />
If you want to use a mix of the arguments from the configuration file and arguments passed to the executable, 
you can specify the "***/useconfig***" argument in the command-line.

  [crmsvcutil]: https://msdn.microsoft.com/en-us/library/gg327844.aspx

Command prompt
-------------------------------
Example usage from a command prompt:

    [lang=bash]
    XrmContext.exe /url:http://<serverName>/<organizationName>/XRMServices/2011/Organization.svc  
            /out:..\path\to\BusinessDomain /username:<username> /password:<password> /domain:<domainName>


Simple Generation Example in F#
-------------------------------

It can also be run through code by referencing the executable and calling the 
`GetContext` function.
*)

open Microsoft.Xrm.Sdk.Client
open DG.XrmContext

XrmContext.GenerateFromCrm(
  "http://<serverName>/<organizationName>/XRMServices/2011/Organization.svc", 
  "username", "password", 
  out = @"..\path\to\BusinessDomain")


