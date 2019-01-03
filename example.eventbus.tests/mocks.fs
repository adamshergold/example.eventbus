namespace Example.EventBus.Tests

open Example.Serialisation
open Example.Serialisation.Json
open Example.Serialisation.Binary

open Example.Messaging 

module Mocks = 

    type SimpleEvent = {
        Message : string 
    }
    with 
        static member Make( message ) = 
            { Message = message } 
            
        static member Example () = 
            {
                Message = "Hello, world!" 
            }
        
        interface ITypeSerialisable
            with 
                member this.Type with get () = typeof<SimpleEvent>

        static member JSONSerialiser 
            with get () = 
                { new ITypeSerialiser<SimpleEvent>
                    with
                        member this.TypeName =
                            "SimpleEvent"
        
                        member this.Type
                            with get () = typeof<SimpleEvent>
        
                        member this.ContentType
                            with get () = "json"
        
                        member this.Serialise (serialiser:ISerde) (stream:ISerdeStream) (v:SimpleEvent) =
        
                            use js =
                                JsonSerialiser.Make( serialiser, stream, this.ContentType )
        
                            js.WriteStartObject()
                            js.WriteProperty "@type"
                            js.WriteValue this.TypeName
        
                            js.WriteProperty "Message"
                            js.Serialise v.Message
    
                            js.WriteEndObject()
        
                        member this.Deserialise (serialiser:ISerde) (stream:ISerdeStream) =
        
                            use jds =
                                JsonDeserialiser.Make( serialiser, stream, this.ContentType, this.TypeName )
        
                            jds.Handlers.On "Message" ( jds.ReadString )
                            
                            jds.Deserialise()
        
                            let result =
                                {
                                    Message = jds.Handlers.TryItem<_>( "Message" ).Value
                                }
        
                            result }
                            
        static member BinarySerialiser 
            with get () = 
                { new ITypeSerialiser<SimpleEvent>
                    with
                        member this.TypeName =
                            "SimpleEvent"
        
                        member this.Type
                            with get () = typeof<SimpleEvent>
        
                        member this.ContentType
                            with get () = "binary"
        
                        member this.Serialise (serialiser:ISerde) (s:ISerdeStream) (v:SimpleEvent) =
                        
                            use bs = 
                                BinarySerialiser.Make( serialiser, s, this.TypeName )
                            
                            bs.Write(v.Message)
                        
                        member this.Deserialise (serialiser:ISerde) (s:ISerdeStream) =
                        
                            use bds = 
                                BinaryDeserialiser.Make( serialiser, s, this.TypeName )

                            let message = 
                                bds.ReadString()
                        
                            SimpleEvent.Make( message ) }   
