using LangFileCleaner.Commands;
using System.CommandLine;

namespace LangFileCleaner;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand();

        rootCommand.AddCommand(UnusedKeyCommand.GetCommand());
        rootCommand.AddCommand(RepairUnusedKeyCommand.GetCommand());
        rootCommand.AddCommand(SyncLangFileCommand.GetCommand());

        await rootCommand.InvokeAsync(args);
    }
}