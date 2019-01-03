namespace Example.EventBus.Tests 

open Example.Serialisation 

open Example.Messaging 

module Helpers = 
    
    let Serde () =
    
        let options =   
            SerdeOptions.Default
         
        let serde = 
            Serde.Make( options )
            
        serde.TryRegisterAssembly typeof<Envelope>.Assembly |> ignore
        //serde.TryRegisterAssembly typeof<BinaryProxy>.Assembly |> ignore
        serde.TryRegisterAssembly typeof<Mocks.SimpleEvent>.Assembly |> ignore
        
        serde                 
        
    let DefaultSerde = 
        Serde() 
