#r "packages/FAKE/tools/FakeLib.dll"
#load "src/app.fsx"

open Fake
open App
open System
open Suave

let serverConfig =
  let port = int (getBuildParam "port")
  { defaultConfig with
      homeFolder = Some __SOURCE_DIRECTORY__
      logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Warn
      bindings = [ HttpBinding.mkSimple HTTP "127.0.0.1" port ] }

startWebServer serverConfig app
