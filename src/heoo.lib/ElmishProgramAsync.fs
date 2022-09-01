module heoo.lib.ElmishProgramAsync

open System
//For managing the program.
type private programMessage<'ModelT,'MessageT> =
    | ProgramInstruction of 'MessageT
    | Dispose
    
///Create a message loop program
///Remember to set OnModelUpdated to subscribe to model updates (and maybe do thread synch there?).
/// Message processing stops when the program is disposed.
type T<'ModelT,'MessageT>(initialModel:'ModelT,updateFun:'ModelT -> 'MessageT -> 'ModelT) =
    let mutable onModelUpdated: Action<'ModelT> option = None
    let mutable _isDisposed:bool = false
    
    let rec programLoop (inbox:MailboxProcessor<programMessage<'ModelT,'MessageT>>) model = async {
        let! message = inbox.Receive()
        match message with
        | ProgramInstruction messageT ->
            let newModel = updateFun model messageT
            match onModelUpdated with
            | Some x -> x.Invoke(newModel)
            | None -> ()
            return! programLoop inbox newModel
        | Dispose -> _isDisposed <- true
        if _isDisposed |> not then
            return! programLoop inbox model
    }
    let mBoxProcessor = MailboxProcessor.Start(fun inbox -> programLoop inbox initialModel)
     
    member _.PostMessage mess = mBoxProcessor.Post(ProgramInstruction mess)
    member this.OnModelUpdated
        with get() = onModelUpdated
        and set v = onModelUpdated <- v
        
    interface IDisposable with
        member this.Dispose() = mBoxProcessor.Post(Dispose) 
