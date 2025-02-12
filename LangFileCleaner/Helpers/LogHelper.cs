using Serilog;
using Serilog.Core;

namespace LangFileCleaner.Helpers;

public static class LogHelper
{
    public static Logger GetLogger(bool verbose)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate:
                "[{Level:u3}]: {Message:lj}{NewLine}{Exception}");

        if (verbose)
        {
            loggerConfig.MinimumLevel.Debug();
        }
        else
        {
            loggerConfig.MinimumLevel.Information();
        }

        return loggerConfig.CreateLogger();
    }
}