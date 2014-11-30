namespace FSharpDeepDives.ExampleApp

module Models = 
    
    open System
    open FSharpDeepDives

    /// <summary>
    /// A an agent that computes aggregate results over collections of FuelType data
    /// and persists the result as an event
    /// </summary>
    /// <param name="name">The name of the agent</param>
    /// <param name="eventType">The name of the event to store the data under</param>
    /// <param name="token">The cancellation token</param>
    /// <param name="op">The function to compute the statistic over the data</param>
    let stats name eventType token (op : (seq<_> -> FuelTypeStats)) = 
        Agent(name, (fun ref ->
                let rec loop (state:FuelTypeStats) (ref:AgentRef<Message>) = 
                    async {
                        let! (Data(fsp)) = ref.Receive()
                        let result = op fsp                                       
                        do! DataAccess.storeEvent eventType result
                        return! loop result ref
                    }
                loop FuelTypeStats.Zero ref), ?token = token)
    
    /// <summary>
    /// Computes the TOTAL statistic
    /// </summary>
    /// <param name="token">The cancellation token</param>
    let total token =
        stats "total" TotalStatisticUpdated token (Seq.map FuelTypeStats.Create >> Seq.sum)

    /// <summary>
    /// Computes the average statistic
    /// </summary>
    /// <param name="token">The cancellation token</param>
    let average token =
        stats "average" AverageStatisticUpdated token (Seq.map FuelTypeStats.Create >> Seq.average)

