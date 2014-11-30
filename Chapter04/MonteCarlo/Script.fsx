// ======================================================================================
// Set up F# interactive   
// ======================================================================================
#time
fsi.FloatingPointFormat <- "f5"

// ======================================================================================
// Add references to MathNet.Numerics and FSharp.Charting
// ======================================================================================
#r @"..\packages\MathNet.Numerics.2.6.2\lib\Net40\MathNet.Numerics.dll"
#r @"..\packages\FSharp.Charting.0.90.6\lib\net40\FSharp.Charting.dll"
#r "System.Windows.Forms.DataVisualization.dll"
open MathNet.Numerics.Distributions
open MathNet.Numerics.Statistics 

// ======================================================================================
// All listings  
// ======================================================================================
module Listings =

    // ==================================================================================
    // Listing 1: Generation of Brownian increments
    // ==================================================================================
    let get_dW rnd dt N = 
        let dW = Normal.WithMeanVariance(0.0, dt)
        dW.RandomSource <- rnd              
        (fun () -> Array.init N (fun _ -> dW.Sample()))

    // ==================================================================================
    // Listing 2: Generation of GBM paths
    // ==================================================================================
    let generate_GBM_paths_by_log rnd S0 r sigma T N M =
        let dt = T / (float N)
        let drift = (r - 0.5 * (sigma**2.0)) * dt
        let generator = get_dW rnd dt N               
                           
        Array.init M (fun _ -> generator()
                            |> Array.map (fun dWt -> drift + sigma * dWt) 
                            |> Array.scan (+) 0.0 
                            |> Array.map (fun x -> S0 * exp(x)) ) 

    
    // ==================================================================================
    // Listing 3: Payoff Functions
    // ==================================================================================
    let S_T (path:float array) = path.[path.Length - 1]                     

    let european_call K (path:float array) =                                
        max ((S_T path) - K) 0.0      

    let up_and_out_call K H (path:float array) =
        if Array.max path.[1..] >= H then 0.0 
        else european_call K path                            

    // ==================================================================================
    // Listing 4: simulate_payoffs function
    // ==================================================================================
    let simulate_payoffs rnd S0 r sigma T N M payoff = 
        [| for path in generate_GBM_paths_by_log rnd S0 r sigma T N M -> 
              let currentPayoff = payoff path
              (exp (-r*T)) * currentPayoff |]

    // ==================================================================================
    // Listing 5: price_option function
    // ==================================================================================
    let price_option rnd S0 r sigma T N M payoff = 
        simulate_payoffs rnd S0 r sigma T N M payoff
        |> Array.average     

    // ==================================================================================
    // Listing 6: price_option_v2 function
    // ==================================================================================
    let price_option_v2 rnd S0 r sigma T N M payoff =
        let Ys = simulate_payoffs rnd S0 r sigma T N M payoff
        let C_estimate = Ys |> Array.average
        let Y_var = Ys.Variance()
        let std_error = sqrt(Y_var / (float M))                             
        (C_estimate, Y_var, std_error) 

    // ==================================================================================
    // Listing 7: asian_call function
    // ==================================================================================
    let asian_call K (path:float array) =
        let S_avg = path.[1..] |> Array.average
        max (S_avg - K) 0.0   

    // ==================================================================================
    // Listing 8: up_and_out_call function
    // ==================================================================================
    // included in Listing 3

    // ==================================================================================
    // Listing 9: generate_GBM_paths_by_log_AV  function
    // ==================================================================================
    let generate_GBM_paths_by_log_AV rnd S0 r sigma T N M =
        let dt = T / (float N)
        let drift = (r - 0.5 * (sigma**2.0)) * dt
        let dW = get_dW rnd dt N
        let dWs = Array.init M (fun _ -> dW())
        let negated_dWs = 
            dWs |> Array.map (fun x ->
            x |> Array.map (fun y -> -y))
        let generate_path dWs = 
            dWs
            |> Array.map (fun dWt -> drift + sigma * dWt) 
            |> Array.scan (+) 0.0 
            |> Array.map (fun x -> S0 * exp(x))
        Array.map2 (fun x y -> (generate_path x, generate_path y)) dWs negated_dWs  

    // ==================================================================================
    // Listing 10: simulate_payoffs_AV  and price_option_v2_AV function
    // ==================================================================================
    let simulate_payoffs_AV rnd S0 r sigma T N M payoff =
        generate_GBM_paths_by_log_AV rnd S0 r sigma T N M
        |> Array.map (fun (x,y) ->
                0.5*((payoff x)+(payoff y))*(exp (-r*T)))

    let price_option_v2_AV rnd S0 r sigma T N M payoff =
        let Ys = simulate_payoffs_AV rnd S0 r sigma T N M payoff
        let C_estimate = Ys |> Array.average
        let Y_var = Ys.Variance() 
        let std_error = sqrt(Y_var / (float M))
        (C_estimate, Y_var, std_error) 

// ======================================================================================
// Example in 4.1.1: test Array.average, Mean(), and Variance()  
// ======================================================================================
let x = [|1.0..10000000.0|]
let avg1 = x |> Array.average
let avg2 = x.Mean()
let var1 = x.Variance()

// ======================================================================================
// Example in 4.2.1: Plot Stock Price Paths using FSharp.Charting 
// ======================================================================================
module Example_4_2_1 =
    open System
    open System.Drawing
    open FSharp.Charting
    open Listings

    // Monte Carlo parameters
    let T = 0.25            
    let M = 3               
    let N = 200             

    // underlying dynamics
    let S0 = 50.0         
    let sigma = 0.2       
    let r = 0.01          

    // function to generate a chart object for each price path
    let plot_path (T:float) (N:int) (path:float array) color =
        let dt = T/ (float N)
        path |> Array.mapi (fun n p -> ((float n)*dt, p)) 
        |> Chart.Line 
        |> Chart.WithStyling(Color = color, BorderWidth = 2)

    // simulate GBM paths
    let rnd = new System.Random()
    let paths = generate_GBM_paths_by_log rnd S0 r sigma T N M

    // determine maximum and minimum for Y-axis
    let mx, mn = paths |> Array.fold (fun (mx,mn) p -> 
                                (max mx (Array.max p), min mn (Array.min p))) 
                                (Double.MinValue,Double.MaxValue)

    // assign a color to each path chart 
    let colors = [| Color.Green; Color.Red; Color.Blue |] 
    let path_charts = Array.map2 (plot_path T N) paths colors 

    // generate the combined chart
    let title = sprintf 
                    "3 simulated GBM paths with S0=%.2f, r=%.2f, sigma=%.2f, T=%.2f, N=%d"
                    S0 r sigma T N
    let chart = Chart.Combine path_charts
                |> Chart.WithStyling(Margin=(2.0, 12.0, 2.0, 2.0))
                |> Chart.WithTitle(Text=title, FontName = "Arial", FontSize = 14.0, 
                                    FontStyle = FontStyle.Bold, InsideArea = false) 
                |> Chart.WithXAxis(Title="time in years", Max=T, Min=0.0, 
                                    TitleAlignment = StringAlignment.Center, 
                                    TitleFontName = "Arial", 
                                    TitleFontSize = 14.0, TitleFontStyle = FontStyle.Bold)
                |> Chart.WithYAxis(Title="price in $", 
                                    Max=(Math.Round(mx) + 1.0), Min=(Math.Round(mn) - 1.0), 
                                    TitleAlignment = StringAlignment.Center, 
                                    TitleFontName = "Arial", 
                                    TitleFontSize = 14.0, TitleFontStyle = FontStyle.Bold)
    chart.ShowChart()

// ======================================================================================
// Example in 4.2.2: Price European call
// ======================================================================================
module Example_4_2_2 =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 1, 10000000
    let rnd = new System.Random()
    let payoff = european_call K
    let C = price_option rnd S0 r sigma T N M payoff

// ======================================================================================
// Example in 4.2.3: Analyzing Monte Carlo estimates using variance
// ======================================================================================
module Example_4_2_3 =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 1, 10000000
    let rnd = new System.Random()
    let payoff = european_call K
    let (C,Y_var,std_error) = price_option_v2 rnd S0 r sigma T N M payoff

// ======================================================================================
// Example in 4.2.4 (Asian): Price Asian call
// ======================================================================================
module Example_4_2_4_asian =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 3, 10000000
    let rnd = new System.Random()
    let payoff = asian_call K
    let (C,Y_var,std_error) = price_option_v2 rnd S0 r sigma T N M payoff

// ======================================================================================
// Example in 4.2.4 (Barrier): Price Barrier call
// ======================================================================================
module Example_4_2_4_barrier =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 3, 10000000
    let H = 125.0
    let rnd = new System.Random()
    let payoff = up_and_out_call K H
    let (C,Y_var,std_error) = price_option_v2 rnd S0 r sigma T N M payoff

// ======================================================================================
// Example in 4.3 (European) : Variance reduction using antithetic variates  
// ======================================================================================
module Example_4_3_european =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 1, 5000000
    let rnd = new System.Random()
    let payoff = european_call K
    let (C,Y_var,std_error) = price_option_v2_AV rnd S0 r sigma T N M payoff

// ======================================================================================
// Example in 4.3 (Asian) : Variance reduction using antithetic variates  
// ======================================================================================
module Example_4_3_asian =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 3, 5000000
    let rnd = new System.Random()
    let payoff = asian_call K
    let (C,Y_var,std_error) = price_option_v2_AV rnd S0 r sigma T N M payoff

// ======================================================================================
// Example in 4.3 (Barrier) : Variance reduction using antithetic variates  
// ======================================================================================
module Example_4_3_barrier =
    open Listings

    let K, T = 100.0, 0.25
    let S0, r, sigma = 100.0, 0.02, 0.40
    let N, M = 3, 5000000
    let H = 125.0
    let rnd = new System.Random()
    let payoff = up_and_out_call K H
    let (C,Y_var,std_error) = price_option_v2_AV rnd S0 r sigma T N M payoff