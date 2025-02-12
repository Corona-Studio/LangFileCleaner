using LangFileCleaner.Helpers;
using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog.Core;

namespace LangFileCleaner.Commands;

public static class RepairUnusedKeyCommand
{
    public static Command GetCommand()
    {
        var unusedCommand = new Command("unused", "Check if there are unused keys in the Lang file.")
        {
            CommandConstants.UnusedKeys.RootPathOption,
            CommandConstants.UnusedKeys.LangFilePathOption,
            CommandConstants.UnusedKeys.Repair.OutFilePathOption,
            CommandConstants.VerboseOption
        };

        unusedCommand.SetHandler(HandleRepairUnusedKeyAsync);

        return unusedCommand;
    }

    private static async Task HandleRepairUnusedKeyAsync(InvocationContext context)
    {
        var root = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.RootPathOption);
        var srcFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.LangFilePathOption);
        var outFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.Repair.OutFilePathOption);
        var verbose = (bool?)context.ParseResult.GetValueForOption(CommandConstants.VerboseOption) ?? false;

        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentException.ThrowIfNullOrEmpty(srcFilePath);
        ArgumentException.ThrowIfNullOrEmpty(outFilePath);

        var logger = LogHelper.GetLogger(verbose);

        logger.Information("Start repair process for lang file...");

        await RepairLangFileAsync(root, srcFilePath, outFilePath, logger);
    }

    public static async Task RepairLangFileAsync(string root, string srcFilePath, string outFilePath, Logger? logger)
    {
        root = $"{Path.GetFullPath(root)}{Path.DirectorySeparatorChar}";

        var langFilePath = Path.Combine(root, srcFilePath);
        langFilePath = Path.GetFullPath(langFilePath);

        ArgumentOutOfRangeException.ThrowIfEqual(File.Exists(langFilePath), false);

        var langFileContents = await File.ReadAllLinesAsync(langFilePath);
        var unusedKeys = await UnusedKeyCommand.ResolveAndCheckAsync(root, srcFilePath, logger);
    }
}