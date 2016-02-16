#r @"..\packages\FSharp.Data\lib\net40\FSharp.Data.dll"
#r @"..\packages\NodaTime\lib\portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1+XamariniOS1\NodaTime.dll"

#load "calendar.fsx"
#load "common.fsx"

open System

open FSharp.Data
open FSharp.Data.HtmlNode
open NodaTime

open Common
open Calendar
open Calendar.IcsEvent
open Calendar.IcsCalendar

type Availability = | Yesterday | Today | Tomorrow | In2Days | In3Days | In4Days
type Episode = { Title: string; When: Availability option }

type private FoldState = { When: Availability option; Episodes: Episode list }

let tryParseAvailability (s: string) =
    match (s |> toUpper) with
    | "ВЧЕРА" -> Some Yesterday
    | "СЕГОДНЯ" -> Some Today
    | "ЗАВТРА" -> Some Tomorrow
    | "ЧЕРЕЗ 2 ДНЯ" -> Some In2Days
    | "ЧЕРЕЗ 3 ДНЯ" -> Some In3Days
    | "ЧЕРЕЗ 4 ДНЯ" -> Some In4Days
    | _ -> None

let tryCreateEpisode (root: HtmlNode) =
    let lastTwo =
        root
        |> elements
        |> Seq.rev
        |> Seq.take 2
        |> Seq.rev
        |> List.ofSeq

    let title =
        match lastTwo with
            | [a; t] -> innerText a + innerText t
            | _ -> ""
        |> trim

    let aWhen =
        root
        |> findChildNodes (hasName "h2")
        |> Seq.tryHead
        |> Option.map innerText
        |> Option.map trim
        |> Option.bind tryParseAvailability

    match title.Length with
    | l when l > 0 -> Some { Title = title; When = aWhen }
    | _ -> None

let createEpisodes (htmlNode: HtmlNode) =
    let rawEpisodes =
        htmlNode
        |> findChildNodes (hasName "div" <<&>> hasId "shedule")
        |> Seq.collect (findChildNodes (hasName "li"))
        |> List.ofSeq
        |> List.collect (fun li ->
            match tryCreateEpisode li with
            | Some episode -> [episode]
            | _ -> [])

    let folder (state: FoldState) (episode: Episode) =
        match episode.When with
        | Some w ->
                { When = Some w; Episodes = state.Episodes @ [ episode ] }
        | None ->
                { When = state.When; Episodes = state.Episodes @ [ { episode with When = state.When } ] }

    let state = { When = List.tryHead rawEpisodes |> Option.bind (fun e -> e.When); Episodes = [] }
    let folded = rawEpisodes |> List.fold folder state

    folded.Episodes

let toServerLocalDate (serverDateUtc: DateTime) =
    let someTimezone =
        DateTimeZoneProviders.Tzdb.GetZoneOrNull "Europe/Moscow" |> ofNull

    match someTimezone with
    | None -> serverDateUtc
    | Some timezone ->
                    let instant = Instant.FromDateTimeUtc serverDateUtc
                    let zonedDateTime = ZonedDateTime (instant, timezone)
                    zonedDateTime.ToDateTimeUnspecified ()

let toCalendarEvent (serverDateUtc: DateTime) (e: Episode) =
    let shiftDays =
        match e.When with
        | None -> 0.0
        | Some episodeWhen ->
            match episodeWhen with
            | Yesterday -> -1.0
            | Today -> 0.0
            | Tomorrow -> 1.0
            | In2Days -> 2.0
            | In3Days -> 3.0
            | In4Days -> 4.0

    let serverLocalDate = toServerLocalDate serverDateUtc
    let eventDate = serverLocalDate |> addDays shiftDays

    let uid = e.Title + "_" + eventDate.ToString ("yyyyMMdd") |> hashSha256
    let description = sprintf "Last update %s UTC" (serverDateUtc.ToString ("MM/dd/yyyy HH:mm"))

    { EventHeader = Event.Header();
      EventFooter = Event.Footer();
      EventBody =
        [ Event.Uid(uid);
          Event.Class(Public);
          Event.BusyStatus(Busy);
          Event.Transp(Opaque);
          Event.Sequence(0);
          Event.DateStart(eventDate);
          Event.LastModified(DateTime.UtcNow);
          Event.Summary(e.Title);
          Event.Description(description) ] }
