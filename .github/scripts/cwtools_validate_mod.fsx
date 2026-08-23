// Validates a CK3 mod folder using the CWTools library directly.
//
// Why not CWToolsCLI ("cwtools validate")? The CLI passes no scriptFolders setting,
// so CK3Game falls back to an empty folder list and always parses zero files
// (upstream bug since 2021). This script drives the same library with proper settings.
//
// The generated mod embeds copies of vanilla folders, and the cwtools ck3 rules/metadata
// cache lag behind current game versions, so rule-based findings are dominated by false
// positives from vanilla content. Therefore this script reports findings and only fails
// (non-zero exit) when run with --strict.
//
// Expects the CWTools assemblies (from the CWTools.CLI dotnet tool) in
// ./cwtools_tools next to this script (i.e. .github/scripts/cwtools_tools when
// run from the repository root).
//
// Usage:
//   dotnet fsi cwtools_validate_mod.fsx <modFolder> <cwtoolsRulesFolder> <metadataCacheFile> [<outputJson>] [--strict]

#I "cwtools_tools"
#r "Aether.dll"
#r "Chiron.dll"
#r "CSharpHelpers.dll"
#r "DotNet.Glob.dll"
#r "FParsecCS.dll"
#r "FParsec.dll"
#r "FSharp.Collections.ParallelSeq.dll"
#r "FSharpPlus.dll"
#r "FSharpx.Collections.dll"
#r "FsPickler.dll"
#r "ICSharpCode.SharpZipLib.dll"
#r "Sandwych.QuickGraph.Core.dll"
#r "Shared.dll"
#r "CWTools.dll"
#r "Argu.dll"
#r "Chiron.dll"
#r "CWToolsCLI.dll"

open System
open System.IO
open CWTools.Games
open CWTools.Games.CK3
open CWTools.Games.Files
open CWTools.Common
open CWTools.Utilities.Position

let argList = fsi.CommandLineArgs |> Array.skip 1 |> List.ofArray
let strict = argList |> List.exists (fun a -> a = "--strict")
let positional = argList |> List.filter (fun a -> a <> "--strict") |> Array.ofList

if positional.Length < 3 then
    eprintfn "Usage: dotnet fsi cwtools_validate_mod.fsx <modFolder> <cwtoolsRulesFolder> <metadataCacheFile> [<outputJson>] [--strict]"
    exit 2

let modDir = Path.GetFullPath positional.[0]
let rulesDir = Path.GetFullPath positional.[1]
let cacheFile = Path.GetFullPath positional.[2]
let outputFile = if positional.Length > 3 then Some positional.[3] else None

if not (Directory.Exists modDir) then eprintfn "Mod folder not found: %s" modDir; exit 2
if not (Directory.Exists rulesDir) then eprintfn "Rules folder not found: %s" rulesDir; exit 2
if not (File.Exists cacheFile) then eprintfn "Cache file not found: %s" cacheFile; exit 2

printfn "CWTools CK3 mod validation"
printfn "  mod:   %s" modDir
printfn "  rules: %s" rulesDir
printfn "  cache: %s" cacheFile

// Collect rule files (.cwt and .log), mirroring CWToolsCLI's getConfigFiles.
let rec getAllFolders dirs =
    [ for d in dirs do
        yield d
        yield! getAllFolders (Directory.EnumerateDirectories d |> List.ofSeq) ]

let configFiles =
    getAllFolders [ rulesDir ]
    |> Seq.collect Directory.EnumerateFiles
    |> Seq.filter (fun f -> Path.GetExtension f = ".cwt" || Path.GetExtension f = ".log")
    |> List.ofSeq

let configs = configFiles |> List.map (fun f -> f, File.ReadAllText f)
printfn "  loaded %d rule files" configs.Length

let metadata = CWToolsCLI.Serializer.deserializeMetadata cacheFile

let workspaceDir : WorkspaceDirectory = { path = modDir; name = "game" }

let settings : CK3Settings =
    { rootDirectories = [ WD workspaceDir ]
      embedded = Metadata metadata
      validation =
        { langs = [ CK3 CK3Lang.English ]
          validateVanilla = false
          experimental = true }
      rules =
        Some { ruleFiles = configs
               validateRules = false
               debugRulesOnly = false
               debugMode = false }
      // CWToolsCLI never provides these for CK3 (it falls back to an empty array),
      // which is why every CLI validation of a CK3 mod parses zero files.
      scriptFolders = Some [ "common"; "events"; "gfx"; "gui"; "localization"; "history"; "map_data" ]
      excludeGlobPatterns = None
      modFilter = Some ""
      maxFileSize = Some 8
      debugSettings = DebugSettings.Default }

let game = CK3Game(settings)
let api = game :> IGame<CWTools.Games.JominiComputedData>

type Finding =
    { file: string
      severity: string
      category: string
      message: string
      line: int
      column: int }

let severityToString (s: Severity) =
    match s with
    | Severity.Error -> "error"
    | Severity.Warning -> "warning"
    | Severity.Information -> "information"
    | _ -> "hint"

let normalizeFile (f: string) =
    f.Replace('\\', '/').Replace(modDir.Replace('\\', '/') + "/", "")

let parserFindings =
    [ for (file, message, pos) in api.ParserErrors() ->
        { file = normalizeFile file
          severity = "error" // parse failures are always errors
          category = "parse"
          message = message
          line = int pos.Line
          column = int pos.Column } ]

let validationFindings =
    [ for e in api.ValidationErrors() ->
        { file = (if e.range.FileName = "" then "<lookup>" else normalizeFile e.range.FileName)
          severity = severityToString e.severity
          category = e.code
          message = e.message
          line = e.range.StartLine
          column = e.range.StartColumn } ]

let localisationFindings =
    [ for e in api.LocalisationErrors(true, true) ->
        { file = (if e.range.FileName = "" then "<localisation>" else normalizeFile e.range.FileName)
          severity = severityToString e.severity
          category = e.code
          message = e.message
          line = e.range.StartLine
          column = e.range.StartColumn } ]

let findings = parserFindings @ validationFindings @ localisationFindings

let sortedFindings = findings |> List.sortBy (fun f -> f.file, f.line, f.column)

let errorCount = sortedFindings |> List.filter (fun f -> f.severity = "error") |> List.length
let warningCount = sortedFindings |> List.filter (fun f -> f.severity = "warning") |> List.length

for f in sortedFindings do
    printfn "%s(%d,%d): %s %s: %s" f.file f.line f.column f.severity f.category f.message

printfn ""
printfn "Summary: %d findings (%d errors, %d warnings)" sortedFindings.Length errorCount warningCount

sortedFindings
|> List.countBy (fun f -> f.severity, f.category)
|> List.sortByDescending snd
|> List.iter (fun ((sev, cat), n) -> printfn "  %-11s %-8s %d" sev cat n)

printfn ""
printfn "Top files:"
sortedFindings
|> List.filter (fun f -> f.severity = "error")
|> List.countBy (fun f -> f.file)
|> List.sortByDescending snd
|> List.truncate 20
|> List.iter (fun (file, n) -> printfn "  %6d  %s" n file)

match outputFile with
| Some path ->
    let escape (s: string) =
        let sb = Text.StringBuilder(s.Length)
        for c in s do
            match c with
            | '"' -> sb.Append("\\\"") |> ignore
            | '\\' -> sb.Append("\\\\") |> ignore
            | '\n' -> sb.Append("\\n") |> ignore
            | '\r' -> sb.Append("\\r") |> ignore
            | '\t' -> sb.Append("\\t") |> ignore
            | c when int c < 32 -> sb.Append(sprintf "\\u%04x" (int c)) |> ignore
            | c -> sb.Append(c) |> ignore
        sb.ToString()
    let json =
        [ for f in sortedFindings ->
            sprintf "{\"file\":\"%s\",\"severity\":\"%s\",\"category\":\"%s\",\"line\":%d,\"column\":%d,\"message\":\"%s\"}"
                (escape f.file)
                f.severity f.category f.line f.column
                (escape f.message) ]
    File.WriteAllText(path, sprintf "[\n%s\n]" (String.Join(",\n", json)))
    printfn "Results written to %s" path
| None -> ()

if strict && errorCount > 0 then
    eprintfn "Validation failed: %d errors found" errorCount
    exit 1
exit 0
