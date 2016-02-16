open System

module IcsEvent =
    type IcsEventHeader = IcsEventHeader of string * string
    type IcsEventFooter = IcsEventFooter of string * string
    type IcsEventBodyItem = IcsEventBodyItem of string * string

    type IcsEventClass = Public
    type IcsBusyStatusMs = Busy
    type IcsTransp = Opaque

    type IcsEvent =
        { EventHeader: IcsEventHeader;
          EventFooter: IcsEventFooter;
          EventBody: IcsEventBodyItem list; }

    module Event =
        let Header () =
            IcsEventHeader ("BEGIN", "VEVENT")
        let Footer () =
            IcsEventFooter ("END", "VEVENT")
        let Uid (uid: string) =
            IcsEventBodyItem ("UID", uid)
        let Class (ec: IcsEventClass) =
            IcsEventBodyItem ("CLASS", match ec with | Public -> "PUBLIC")
        let BusyStatus (bs: IcsBusyStatusMs) =
            IcsEventBodyItem ("X-MICROSOFT-CDO-BUSYSTATUS", match bs with | Busy -> "BUSY")
        let Transp (tr: IcsTransp) =
            IcsEventBodyItem ("TRANSP", match tr with | Opaque -> "OPAQUE")
        let Sequence (s: int) =
            IcsEventBodyItem ("SEQUENCE", string s)
        let DateStart (d: DateTime) =
            IcsEventBodyItem ("DTSTART;VALUE=DATE", d.ToString("yyyyMMdd"))
        let DateEnd (d: DateTime) =
            IcsEventBodyItem ("DTEND;VALUE=DATE", d.ToString("yyyyMMdd"))
        let LastModified (d: DateTime) =
            IcsEventBodyItem ("LAST-MODIFIED", d.ToString("yyyyMMddTHHMMssZ"))
        let Summary (s: string) =
            IcsEventBodyItem ("SUMMARY", s)
        let Description (d: string) =
            IcsEventBodyItem ("DESCRIPTION", d)
        let Priority (p: int) =
            IcsEventBodyItem ("PRIORITY", string p)

    let toString (e: IcsEvent) =
        let sb = new Text.StringBuilder()
        let append (key, value) = sb.AppendLine(sprintf "%s:%s" key value)
        let appendHeader (h: IcsEventHeader) = match h with IcsEventHeader (key, value) -> append(key, value) |> ignore
        let appendFooter (f: IcsEventFooter) = match f with IcsEventFooter (key, value) -> append(key, value) |> ignore
        let appendBodyItem (i: IcsEventBodyItem) = match i with IcsEventBodyItem (key, value) -> append(key, value) |> ignore

        e.EventHeader   |> appendHeader
        e.EventBody     |> List.iter appendBodyItem
        e.EventFooter   |> appendFooter

        sb.ToString()

module IcsCalendar =
    open IcsEvent

    type IcsCalendarHeader = IcsCalendarHeader of string * string
    type IcsCalendarFooter = IcsCalendarFooter of string * string
    type IcsCalendarBodyItem =
        | IcsCalendarItem of string * string
        | IcsEvent of IcsEvent

    type IcsMethod = Publish

    type IcsCalendar =
        { CalendarHeader: IcsCalendarHeader;
          CalendarFooter: IcsCalendarFooter;
          CalendarBody: IcsCalendarBodyItem list}

    module Calendar =
        let Header () =
            IcsCalendarHeader ("BEGIN", "VCALENDAR")
        let Footer () =
            IcsCalendarFooter ("END", "VCALENDAR")
        let Method (m: IcsMethod) =
            IcsCalendarItem ("METHOD", match m with Publish -> "PUBLISH")
        let Version (v: string) =
            IcsCalendarItem ("VERSION", v)
        let ProdId (id: string) =
            IcsCalendarItem ("PRODID", id)
        let RefreshInterval (i: string) =
            IcsCalendarItem ("REFRESH-INTERVAL;VALUE=DURATION", i)
        let PublishedTtl (i: string) =
            IcsCalendarItem ("X-PUBLISHED-TTL", i)

    let toString (c: IcsCalendar) =
        let sb = new Text.StringBuilder()
        let append (key, value) = sb.AppendLine(sprintf "%s:%s" key value)
        let appendHeader (h: IcsCalendarHeader) = match h with IcsCalendarHeader (key, value) -> append(key, value) |> ignore
        let appendFooter (f: IcsCalendarFooter) = match f with IcsCalendarFooter (key, value) -> append(key, value) |> ignore
        let appendBodyItem (i: IcsCalendarBodyItem) =
            match i with
            | IcsCalendarItem (key, value) -> append(key, value) |> ignore
            | IcsEvent event -> sb.Append(event |> toString) |> ignore

        c.CalendarHeader   |> appendHeader
        c.CalendarBody     |> List.iter appendBodyItem
        c.CalendarFooter   |> appendFooter

        sb.ToString()
