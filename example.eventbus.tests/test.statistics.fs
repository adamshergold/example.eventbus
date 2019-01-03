namespace Example.EventBus.Tests

open Microsoft.Extensions.Logging 

open Xunit
open Xunit.Abstractions 

open Example.EventBus

type StatisticsShould( oh: ITestOutputHelper ) = 

    [<Fact>]
    member this.``CreateAndReportCorrectly`` () = 
       
        let sut = 
            Statistics.Make( 123, 456 )
            
        Assert.Equal( 123, sut.Sent )
        Assert.Equal( 456, sut.Received )                

                     
     