#load "ThrottlingAgent.fs"
open BlogBackup

//Note, these aren't really tests. We're just trying the agents out to see if they work the way we expect.
let delayedReturn(v) = 
    async {
        do! Async.Sleep(v * 100)
        return v }
        
let time msg fn vl =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    fn vl |> ignore
    printfn (Printf.TextWriterFormat<_>(msg)) sw.ElapsedMilliseconds
    () 

let throttleLimit limit =
    let throttleAgent = ThrottleAgent<int>(limit)
    [1..20] |> Seq.ofList |> Seq.map delayedReturn |> Seq.map throttleAgent.AddJob |> Async.Parallel |> Async.RunSynchronously

let testThrottleAgent limit = 
    let msg = "Throttle delayedReturn 1..20 with max " + (string limit) + " concurrent takes %A ms"
    time msg throttleLimit limit
    
// these get increasingly slower, but not much difference between 10/15/20
testThrottleAgent 20
testThrottleAgent 15
testThrottleAgent 10
testThrottleAgent 5
testThrottleAgent 1

#time
//try serially
[1..20] |> Seq.ofList |> Seq.iter (fun i -> System.Threading.Thread.Sleep(i*100); printfn "%d" i)
//try in parallel
[1..20] |> Seq.ofList |> Seq.map delayedReturn |> Async.Parallel |> Async.RunSynchronously

//try  parallel agent
let parallelAgent = ParallelAgent<int>(5)
[1..20] |> Seq.ofList |> Seq.map delayedReturn |> parallelAgent.Work
//try the run-parallel method
[1..20] |> Seq.ofList |> Seq.map delayedReturn |> ThrottleAgent<int>.RunParallel 3
