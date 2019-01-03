namespace Example.EventBus.Tests

open Microsoft.Extensions.Logging 

open Xunit
open Xunit.Abstractions 

open Example.Messaging
open Example.Messaging.Rabbit
 
open Example.EventBus

open Example.EventBus.Tests.Mocks  

type MessagingCreator = {
    Name : string 
    Creator : ILogger -> IMessaging
}
with 
    static member Make( name, creator ) = 
        { Name = name; Creator = creator }

    override this.ToString() = this.Name 

type EventBusShould( oh: ITestOutputHelper ) = 

    let stopWaitMilliseconds = 1000 
    
    let logger =
     
        let options = 
            { Logging.Options.Default with OutputHelper = Some oh }
        
        Logging.CreateLogger( options )
        
    static member Memory (logger:ILogger) = 
    
        let options = 
            { MemoryMessagingOptions.Default with Logger = Some logger } 
            
        MemoryMessaging.Make( Helpers.DefaultSerde, options )

    static member Rabbit (logger:ILogger) = 
    
        let options = 
            { RabbitMessagingOptions.Default with Logger = Some logger } 
            
        RabbitMessaging.Make( Helpers.DefaultSerde, options )

    static member Messaging 
        with get () = 
            seq { 
                yield [| MessagingCreator.Make( "memory", EventBusShould.Memory ) |]
                //yield [| MessagingCreator.Make( "rabbit", EventBusShould.Rabbit ) |]
            } 
        
    [<Theory>]
    [<MemberData("Messaging")>]
    member this.``Test`` (v:MessagingCreator) = 
    
        let serialiser = 
            Helpers.DefaultSerde 
            
        let messaging = 
            v.Creator logger

        messaging.Start() 
                        
        let subject = "top-secret" 
                    
        let label = "default" 
                            
        use broadcastBus =
        
            let options = 
                EventBusOptions.Make( Some logger, messaging.CreateRecipient label "broadcast" ) 
         
            EventBus.Make( serialiser, options )
        
        use receiveBus = 

            let options = 
                EventBusOptions.Make( Some logger, messaging.CreateRecipient label "receive") 
        
            EventBus.Make( serialiser, options ) 
            
        let onEvent (se:Mocks.SimpleEvent) = 
            async {
                logger.LogInformation( "OnEvent {SimpleEvent}", se )
            }
                                
        let onEventReceiver = 
            receiveBus.Register subject onEvent 
                         
        let item = 
            SimpleEvent.Make( "Hello!") 
            
        broadcastBus.Publish subject item 
        
        System.Threading.Thread.Sleep( stopWaitMilliseconds )
        messaging.Stop() 
        
        Assert.Equal( 1, (broadcastBus.Statistics subject).Sent )
        Assert.Equal( 0, (broadcastBus.Statistics subject).Received )

        Assert.Equal( 0, (receiveBus.Statistics subject).Sent )
        Assert.Equal( 1, (receiveBus.Statistics subject).Received )
        
                     
     