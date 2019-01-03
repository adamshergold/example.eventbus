namespace Example.EventBus

open Example.Serialisation 

type IStatistics =
    abstract Sent : int with get
    abstract Received : int with get 
    
type IEventBus = 
    inherit System.IDisposable
    abstract Publish : subject:string -> 'T -> unit
    abstract Register : subject:string ->  ('T -> Async<unit>) -> string
    abstract Remove : id:string -> bool
    abstract Statistics : subject:string -> IStatistics 

