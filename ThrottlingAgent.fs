namespace BlogBackup

//Outstanding questions:
// when do you use MailboxProcessors?
// how do you decide between sending parameters in loop call and sending them in messages?
// how do you decide between one loop with multiple messages and mutually recursive loops?
// how do you decide between a callback and an async reply?
//http://stackoverflow.com/questions/6219726/throttled-async-download-in-f
type ThrottleMessage<'a> = 
    | AddJob of (Async<'a> * AsyncReplyChannel<'a>) 
    | DoneJob of ('a * AsyncReplyChannel<'a>) 
    | Stop

/// This agent accumulates 'jobs' but limits the number which run concurrently.
type ThrottleAgent<'a>(limit) = 
    let myagent = MailboxProcessor<ThrottleMessage<'a>>.Start(fun inbox ->
        let rec loop(jobs, count) = async {
            let! msg = inbox.Receive()  //get next message
            match msg with
            | AddJob(job) -> 
                if count < limit then   //if not at limit, we work, else loop
                    return! work(job::jobs, count)
                else
                    return! loop(job::jobs, count)
            | DoneJob(result, reply) -> 
                reply.Reply(result)           //send back result to caller
                return! work(jobs, count - 1) //no need to check limit here
            | Stop -> return () }
        and work(jobs, count) = async {
            match jobs with
            | [] -> return! loop(jobs, count) //if no jobs left, wait for more
            | (job, reply)::jobs ->           //run job, post Done when finished
                async { let! result = job 
                        inbox.Post(DoneJob(result, reply)) }
                |> Async.Start
                return! loop(jobs, count + 1) //job started, go back to waiting       
        }
        loop([], 0)
    )

    member m.AddJob(job) = myagent.PostAndAsyncReply(fun reply -> AddJob(job, reply))
    member m.Stop() = myagent.Post(Stop)

    ///Static function to run a seq of jobs without having to explicitly instantiate the agent
    static member RunParallel limit jobs = 
        let agent = ThrottleAgent<'a>(limit)
        let res = jobs |> Seq.map (fun job -> agent.AddJob(job))
                       |> Async.Parallel
                       |> Async.RunSynchronously
        agent.Stop()
        res

/// This agent takes a seq of jobs, runs them but limits the number which run concurrently
/// returns the result sychronously
type ParallelAgent<'a>(limit) =
    let agent = ThrottleAgent<'a>(limit)
    member m.Work(jobs) = 
        jobs |> Seq.map (fun job -> agent.AddJob(job))
             |> Async.Parallel
             |> Async.RunSynchronously
    member m.Stop() = agent.Stop()
    