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
  usr = @"exampleUser"
  pwd = @"examplePass"
  dmn = @""
  url = @"https://exampleURL/XRMServices/2011/Organization.svc"
  entities = ["team";"calendar"]
  location = "Metadata"
}


