namespace FSharpDeepDives.ExampleApp

open NLog
open NLog.Config
open NLog.Targets

///Provides a simple implementation of a logger that writes to the console. 
module Logger =

    do
        let config = new LoggingConfiguration();
        let consoleTarget = new ColoredConsoleTarget();
        config.AddTarget("console", consoleTarget);
        let rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
        config.LoggingRules.Add(rule1);
        LogManager.Configuration <- config

    let logger = LogManager.GetLogger("Example App")

    let logStageError stage err = 
        logger.Error(sprintf "Error @ stage %s: %A" stage err)

    let log (msg:string) = 
        logger.Info(msg)

