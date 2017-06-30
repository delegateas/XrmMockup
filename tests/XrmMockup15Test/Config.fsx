module CustomConfig

type Config = {
  usr: string
  pwd: string
  dmn: string
  url: string
  entities: string list
  location: string
}

  
let defaultConfig = {
  usr = @"dgadmin@delegatelab4.onmicrosoft.com"
  pwd = @"S3nnheiser"
  dmn = @""
  url = @"https://delegatelab4.crm4.dynamics.com/XRMServices/2011/Organization.svc"
  entities = ["team";"calendar"]
  location = "Metadata"
}


