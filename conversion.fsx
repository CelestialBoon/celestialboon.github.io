open System.IO
open System.Text.RegularExpressions

let writeAllLines dest lines = File.WriteAllLines(dest, lines)

let prompt param def =
    printfn "Valore di %s? (%s)" param (
        match def with
        | None -> "Nessun default"
        | Some str -> sprintf "Default: %s" str )

    //def |> Option.iter System.Windows.Forms.SendKeys.SendWait

    match System.Console.ReadLine () with
        | "" -> def
        | str -> Some str
    |> Option.map (fun value -> param, value)

let trimTitle lines =
    let trimmed =
        lines
        |> Array.skipWhile System.String.IsNullOrWhiteSpace
    let titleMatch = (Regex "^# (.*)").Match trimmed.[0]
    if titleMatch.Success then
        (Some <| titleMatch.Groups.[1].Value.Trim()), Array.skip 1 trimmed
        else None, trimmed

let dir = __SOURCE_DIRECTORY__
let draftsDir = Path.Combine(dir, "_drafts")
let postsDir = Path.Combine(dir, "_posts")

let args = fsi.CommandLineArgs

let draftPath = 
    match Array.tryItem 1 args with 
    | Some path -> Path.Combine(dir, path)
    | None ->
        let drafts = Directory.GetFiles draftsDir
        if Array.isEmpty drafts then failwith "No draft available in the drafts folder" else
            printfn "Select draft to convert:"
            drafts |> Array.iteri (fun i draft -> printfn "%i: %s" i (Path.GetFileName draft))
            let pick = (System.Console.ReadLine ()) |> int
            drafts.[pick]
(*
---
layout: post
title:  "A title"
date:   2018-07-08 17:22:16 +0200
categories: cat1 cat2
tags: tag1 tag2 tag3
---
*)

let defTitle, body = trimTitle <| File.ReadAllLines draftPath
let defDate = Regex.Replace(System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss zzz"), ":([0-9]{2})$", "$1")
//extract what you can: title, date
let optFileName = prompt "fileName" <| Some (Path.GetFileName draftPath)
let layout = prompt "layout" <| Some "post"
let title = 
    prompt "title" defTitle
    |> Option.map (fun (a, title) -> a, sprintf "\"%s\"" title)
let date = prompt "date" <| Some defDate
let categories = prompt "categories" None
let tags = prompt "tags" None

let categoriesList = 
    categories 
    |> Option.map (fun (_, cat) -> cat.Split([|' '|])) 
    |> Option.toArray 
    |> Array.collect id 
    |> Array.toList

let header = 
    ["---"] @ ( 
        [layout; title; date; categories; tags] 
        |> List.choose (fun elem -> elem |> Option.map (fun (tag, value) -> sprintf "%s: %s" tag value))
    ) @ ["---"]

let fileName = 
    date
    |> Option.map (fun (_, date) -> sprintf "%s-%s" (date.Remove 10) (optFileName |> Option.get |> snd)) 
    |> Option.get
let destPath = Path.Combine(List.toArray([postsDir] @ categoriesList @ [fileName]))

Directory.CreateDirectory <| Path.GetDirectoryName destPath

body
|> Array.append (header |> List.toArray)
|> writeAllLines destPath

System.Diagnostics.Process.Start destPath

File.Delete draftPath