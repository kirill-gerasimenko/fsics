#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open System
open System.Text
open System.Security.Cryptography

open FSharp.Data
open FSharp.Data.HtmlDocument
open FSharp.Data.HtmlNode

let findChildNodes = descendants false
let trim (s: string) = s.Trim()
let toUpper (s: string) = s.ToUpperInvariant()
let tryParse (s: string) =
    match DateTime.TryParse(s) with
    | true, dt -> Some dt
    | false, _ -> None

let ofNull x = match x with null -> None | value -> Some value
let addDays n (date: DateTime) = date.AddDays (n)

let (|ResponseText|_|) (resp: HttpResponse) =
    match resp.Body with Text text -> Some text | _ -> None

let tryGetReponseText (resp: HttpResponse) =
    match resp with ResponseText t -> Some t | _ -> None

let tryParseHtml html =
    try Some (HtmlDocument.Parse html) with | _ -> None

let (<<&>>) (p1:'T -> bool) (p2:'T -> bool) :('T -> bool) =
    fun x -> p1 x && p2 x

let (<<|>>) (p1:'T -> bool) (p2:'T -> bool) :('T -> bool) =
    fun x -> p1 x || p2 x

let hashSha256 (v: string) =
    use sha = new SHA256Managed()
    Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(v)))
