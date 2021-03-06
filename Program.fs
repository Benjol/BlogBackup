﻿open System.Diagnostics
open System.IO
open System.Net
open System.Text.RegularExpressions
open Microsoft.FSharp.Control.WebExtensions
open BlogBackup

///Possible states of an asset: before, during or after download
type AssetState =
    | Pending of string * string                //Asset waiting to be downloaded (url, local path)
    | Skipped of string                         //Already on local disk (url)
    | Blacklisted of string                     //Explicitly excluded via last parameter of 'backup' function (url)
    | SuccessfulDownload of string * byte[]     //Downloaded, waiting to be saved to disk (url, imageData)
    | FailedDownload of string * string         //Something went wrong during download (url, reason)
    | FailedSave of string * string             //Downloaded ok, can't save to disk (local path, reason)
    | Done of string * string                   //Downloaded, saved to disk (url, local path)
    override is.ToString() = sprintf "%A" is

///This is a way of having an AsyncWebRequest which can detect timeouts
///See http://stackoverflow.com/q/5713330/11410
type System.Net.WebRequest with
  member req.AsyncGetResponseWithTimeout () =
    let impl = async {
      let iar = req.BeginGetResponse (null, null)
      let! success = Async.AwaitIAsyncResult (iar, req.Timeout)
      return if success then req.EndGetResponse iar
             else req.Abort ()
                  raise (System.Net.WebException "The operation has timed out") }
    Async.TryCancelled (impl, fun _ -> req.Abort ())

///download an asset asynchronously, return an AssetState
///Inspired/copied from http://fdatamining.blogspot.com/2010/07/f-async-workflow-application-flickr.html
let getAsset (assetUrl:string) =
    async {
        try
            let req = WebRequest.Create(assetUrl) :?> HttpWebRequest
            req.UserAgent <- "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322)";
            req.Method <- "GET";
            req.ServicePoint.ConnectionLimit <- 1000;
            req.AllowAutoRedirect <- true;
            req.MaximumAutomaticRedirections <- 4;
            req.Timeout <- 3000; //3 seconds
            let! response1 = req.AsyncGetResponseWithTimeout()
            let response = response1 :?> HttpWebResponse
            use stream = response.GetResponseStream()
            let ms = new MemoryStream()
            //stream.CopyTo ms // in .Net 4.0 this replaces following 5 lines
            let bytesRead = ref 1
            let buffer = Array.create 0x1000 0uy
            while !bytesRead > 0 do
                bytesRead := stream.Read(buffer, 0, buffer.Length)
                ms.Write(buffer, 0, !bytesRead)
            return SuccessfulDownload(assetUrl, ms.ToArray())
 
        with
            ex -> return FailedDownload(assetUrl, ex.Message)
    }

///Given a path to a text file, return a seq containing anything that looks like an image url
let getImageURLs filePath =
    let matches pat str = Regex.Matches(str, pat, RegexOptions.IgnoreCase) |> Seq.cast<Match>
    let value (m:Match) = m.Value
    filePath |> File.ReadAllText |> matches @"http://[^\s'""]+?\.(jpg|gif|png)" |> Seq.map value |> Seq.distinct

///Home-spun UrlDecode for video urls
let urlDecode (url:string) = 
    url.Replace("%3D","=").Replace("%26", "&").Replace("%25","%")

///Given a path to a text file, return a seq containing anything that looks like a video url
let getVideoURLs filePath = 
    let matches pat str = Regex.Matches(str, pat, RegexOptions.IgnoreCase) |> Seq.cast<Match>
    let value (m:Match) = m.Value
    let topntail (str:string) = str.Substring(7, str.Length - 8) // "flvurl=".Length = 7, final & = 1
    filePath |> File.ReadAllText |> matches @"flvurl=.*?&" |> Seq.map value |> Seq.distinct |> Seq.map urlDecode |> Seq.map topntail

///Save byte[] content to given path on disk (asynchronously)
let asyncSaveAssetToDisk path content =
    //make sure directory path exists
    Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
    async {
        use s = new FileStream(path, FileMode.OpenOrCreate)
        do! s.AsyncWrite(content)
        }

///This is the function that does the 'real' work. Given a list of assetUrls, downloads them to disk path
/// (using the urltopath function to determine disk path), unless they correpond to blackpattern, or
/// if they are already present on disk. Runs request in parallel, returns a list of AssetStates
let getAssets urltopath blackpattern assetUrls =
    let blackreg = Regex(blackpattern, RegexOptions.IgnoreCase)
    assetUrls
        |> Seq.map (fun url -> if blackreg.IsMatch(url) then Blacklisted(url) else Pending(url,""))  //exclude blacklisted urls
        |> Seq.map (function | Pending(url, _) -> Pending(url, urltopath url) | b -> b)              //calculate disk path for pending assets
        |> Seq.map (function                                                                         //change Pending to Skipped if already on disk
                    | Pending(url, path) as p -> if File.Exists(path) then Skipped(url) else p 
                    | b -> b)
        |> Seq.mapi (fun i state -> async {                                                          //now go async
                match state with
                | Pending(url, path) -> 
                            printfn "%d. Starting %A" i url
                            let! result = getAsset url
                            match result with
                            | SuccessfulDownload(url, content) ->
                                printfn "%d. Done reading %A" i url
                                try
                                    do! asyncSaveAssetToDisk path content 
                                    printfn "%d. Done writing to %A" i path
                                    return Done(url, path)
                                with ex ->
                                    printfn "%d. Failed! %A" i ex.Message
                                    return FailedSave(path, ex.Message)
                            | other -> printfn "%d. Download failed" i; return other
                | other -> return other
                    })
        |> ThrottleAgent<AssetState>.RunParallel 10   //Limit to 10 parallel downloads at a time

///Extracts all media links from xml file, saves to rootPath, except for those corresponding to blackpattern.
/// Returns a list of AssetStates representing any failed downloads
let backup xmlPath rootPath blackpattern =
    let sw = Stopwatch.StartNew()

    let urltopath (url:string) = Path.Combine(rootPath, url.Replace("http://", ""))
    let imageresults = xmlPath |> getImageURLs |> getAssets urltopath blackpattern

    let videourltopath (url:string) = 
        let (eq,nd) = (url.IndexOf('=') + 1, url.IndexOf('&'))
        Path.Combine(Path.Combine(rootPath, "VIDEO"), url.Substring(eq, nd - eq) + ".flv")
    let videoresults = xmlPath |> getVideoURLs |> getAssets videourltopath blackpattern

    let results = Array.append imageresults videoresults
    printfn "Done"
    //Ugly code to fold results into 'statistics' for printing to console, each element of the tuple represents an ImageState
    let (t, s, b, fd, fs, d) = 
        Seq.fold (fun (t, s, b, fd, fs, d) st -> 
            match st with 
            | Skipped(_) -> (t + 1, s + 1, b, fd, fs, d)
            | Blacklisted(_) -> (t + 1, s, b + 1, fd, fs, d)
            | FailedDownload(_) -> (t + 1, s, b, fd + 1, fs, d)
            | FailedSave(_) -> (t + 1, s, b, fd, fs + 1, d)
            | Done(_) -> (t + 1, s, b, fd, fs, d + 1)
            | _ -> failwith "shouldn't happen!") (0,0,0,0,0,0) results
    printfn "%d total, %d skipped, %d blacklisted, %d failed download, %d failed save, %d completed successfully, in %ums" t s b fd fs d sw.ElapsedMilliseconds
    results |> Array.filter (function | Blacklisted(_) | FailedDownload(_) | FailedSave(_) -> true | _ -> false)

//Todo: recreate html files locally on disk
[<EntryPoint>]
let main args =
    match args with
    | [|source;target|] -> 
                            let res = backup source target "localhost:"
                            File.WriteAllLines(target + @"\BlogBackupLog.txt", res |> Array.map string) |> ignore
                            printfn "%A" res
    | _ -> printfn @"Usage: [bloggerexportfile] [targetfolder] \n Example: BlogBackup.exe C:\Users\Windows7\Desktop\blog.xml C:\BackupFolder"

    System.Console.ReadKey() |> ignore
    0

