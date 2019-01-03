namespace Example.EventBus

open Microsoft.Extensions.Logging 

open Example.Messaging 
open Example.Serialisation

[<AutoOpen>]
module EventBusImpl = 

    type IHandler = 
        abstract Received : int with get 
        
    type Handler<'T> = {    
        Callback : 'T -> Async<unit> 
        Received : ref<int>
    }
    with 
        member this.Fn (message:IMessage) =
        
            async {
                let body = 
                    match message.Body with 
                    | Content(ts) -> 
                        ts 
                    | Error(e) -> 
                        failwithf "EventBus Handler received error instead of content! '%s'" e.Message 
                    
                let bodyT = 
                    match box(body) with 
                    | :? 'T as t -> 
                        t 
                    | _ -> 
                        failwithf "EventBus Handker expected to receive message of type '%O' but saw '%O'" (typeof<'T>) (body.GetType()) 
                
                System.Threading.Interlocked.Increment( this.Received ) |> ignore 
                 
                do! this.Callback( bodyT )
                
                return None
            }
                     
        static member Make( cb ) = 
            { Callback = cb; Received = ref 0 }
            
        interface IHandler 
            with 
                member this.Received = !this.Received            
     
type EventBusOptions = {
    Logger : ILogger option
    Recipient : IRecipient 
}
with 
    static member Make( logger, recipient ) = 
        { Logger = logger; Recipient = recipient }
                
type EventBus( serde:ISerde, options: EventBusOptions ) =

    let items = 
        new System.Collections.Generic.Dictionary<string,IHandler>()
        
    let sent = 
        new System.Collections.Generic.Dictionary<string,ref<int>>()

    member val Logger = options.Logger 
                            
    member val Recipient = options.Recipient 
                                
    static member Make( serialiser, options ) = 
        new EventBus( serialiser, options ) :> IEventBus

    member this.Dispose () =
        items.Keys 
        |> Seq.iter ( fun receiverId ->
            this.Recipient.RemoveReceiver receiverId |> ignore ) 
        
        items.Clear() 
        
    member this.Statistics (subject:string) =
    
        let sent = 
            match sent.TryGetValue subject with 
            | true, v -> !v
            | false, _ -> 0 
            
        let received =
            items.Values
            |> Seq.cast<IHandler>
            |> Seq.map ( fun handler -> 
                handler.Received ) 
            |> Seq.fold ( fun acc v -> acc + v ) 0
                
        Example.EventBus.Statistics.Make( sent, received )                
        
    member this.Publish (subject:string) (v:obj) =
    
        match sent.TryGetValue subject with 
        | false, _ -> 
            sent.Add( subject, ref 1 )
        | true, count ->    
            System.Threading.Interlocked.Increment( count ) |> ignore 
            
        let ts =    
            match v with 
            | :? ITypeSerialisable as ts -> ts 
            | _ -> failwithf "Unable to publish event with type that did not implement ITypeSerialisable" 
            
        let message =
           
            let header = 
                Header.Make( Some subject, None )
        
            let body = 
                Body.Content( ts )
                        
            Message.Make( header, body )
            
        this.Recipient.Send (Recipients.ToAll(None),message)
                        
    member this.Register (subject:string) (cb:'T->Async<unit>) =
     
        lock this ( fun _ ->
        
            let handler = 
                Handler<_>.Make( cb ) 

            let receiver = 
                Receiver.Make( Some subject, handler.Fn )

            items.Add( receiver.ReceiverId, handler )
                                            
            this.Recipient.AddReceiver receiver 
            
            receiver.ReceiverId )

    member this.Remove (id:string) =
        lock this ( fun _ ->
            if items.ContainsKey id then 
                items.Remove(id) |> ignore
                this.Recipient.RemoveReceiver id
            else 
                if this.Logger.IsSome then 
                    this.Logger.Value.LogError( "EventBus::Remove - Receiver {Id} was not found!", id )
                false )
                
                                            
    interface System.IDisposable
        with 
            member this.Dispose () = 
                this.Dispose()
                
    interface IEventBus 
        with
            member this.Statistics subject = 
                this.Statistics subject 
            
            member this.Remove id =     
                this.Remove id 
                
            member this.Register subject cb =
                this.Register subject cb
          
            member this.Publish subject v =
                this.Publish subject v
                
        