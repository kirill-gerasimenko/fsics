#r @"..\packages\Suave\lib\net40\Suave.dll"
#r @"..\packages\FSharp.Data\lib\net40\FSharp.Data.dll"
#r @"..\libs\FSharp.Configuration\FSharp.Configuration.dll"

#load "common.fsx"
#load "calendar.fsx"
#load "episode.fsx"

open System
open System.Net

open FSharp.Configuration

open Suave
open Suave.Successful
open Suave.Filters
open Suave.Operators
open Suave.Writers
open FSharp.Data
open FSharp.Data.HtmlDocument
open FSharp.Data.HtmlNode
open FSharp.Data.HttpRequestHeaders

open Common
open Episode
open Calendar
open Calendar.IcsEvent
open Calendar.IcsCalendar

let [<Literal>] loginUrl = "https://soap4.me/login/"
let [<Literal>] myEpisodesUrl = "https://soap4.me/shedule/my/"

type Config = YamlConfig< @"..\config.yaml" >
let config = Config()

let headers = [
    UserAgent "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36";
    Host "soap4.me";
    Origin "https://soap4.me";
    Referer "https://soap4.me/login/" ]

let credentials = [("login", config.Credentials.Login); ("password", config.Credentials.Password)]

let createCalendarEvents serverDate html =
    let someEvents =
        html
        |> tryParseHtml
        |> Option.bind tryGetBody
        |> Option.map createEpisodes
        |> Option.map (List.map (toCalendarEvent serverDate))
        |> Option.map (List.map IcsCalendarBodyItem.IcsEvent)

    match someEvents with
    | Some events -> events
    | None -> []

let fetchSchedule ctx =
    async {
        let cc = CookieContainer()

        let! loginResponse =
            Http.AsyncRequest
                (loginUrl,
                 httpMethod="POST",
                 headers = headers,
                 body = FormValues credentials,
                 cookieContainer = cc)

        let serverDate =
            loginResponse.Headers.TryFind(HttpResponseHeaders.Date)
            |> Option.bind tryParse
            |> Option.map (fun d -> d.ToUniversalTime ())

        return!
            match serverDate with
            | None -> OK "NO DATE" ctx
            | Some date ->
                let someEvents =
                    Http.AsyncRequest(myEpisodesUrl, httpMethod="GET", cookieContainer = cc)
                    |> Async.RunSynchronously
                    |> tryGetReponseText
                    |> Option.map (createCalendarEvents date)

                let events =
                    match someEvents with
                    | Some events -> events
                    | None -> []

                let calendar =
                    { CalendarHeader = Calendar.Header();
                      CalendarFooter = Calendar.Footer();
                      CalendarBody =
                        [ Calendar.Method Publish;
                          Calendar.Version "2.0";
                          Calendar.ProdId "-//fsics//soap4mecalendar//EN";
                          Calendar.RefreshInterval config.CalendarUpdateInterval;
                          Calendar.PublishedTtl config.CalendarUpdateInterval ] @ events }

                OK (calendar |> toString) ctx
    }

let noCache =
  setHeader "Cache-Control" "private, max-age=0, no-cache, no-store, must-revalidate"
  >=> setHeader "Pragma" "no-cache"
  >=> setHeader "Expires" "0"

let app =
    choose
        [ GET >=> choose
            [ path "/calendar"
                >=> noCache
                >=> setMimeType "text/calendar"
                >=> fetchSchedule;
              path "/" >=> OK "Nothing here" ] ]
