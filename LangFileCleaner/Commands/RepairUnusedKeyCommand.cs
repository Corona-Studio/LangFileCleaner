using System.Collections.Frozen;
using LangFileCleaner.Helpers;
using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog.Core;

namespace LangFileCleaner.Commands;

public static class RepairUnusedKeyCommand
{
    public static Command GetCommand()
    {
        var unusedCommand = new Command("repair", "Repair Lang file by commenting all the unused Lang key.")
        {
            CommandConstants.UnusedKeys.RootPathOption,
            CommandConstants.UnusedKeys.LangFilePathOption,
            CommandConstants.OutFilePathOption,
            CommandConstants.VerboseOption
        };

        unusedCommand.SetHandler(HandleRepairUnusedKeyAsync);

        return unusedCommand;
    }

    private static async Task HandleRepairUnusedKeyAsync(InvocationContext context)
    {
        var root = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.RootPathOption);
        var srcFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.LangFilePathOption);
        var outFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.OutFilePathOption);
        var verbose = (bool?)context.ParseResult.GetValueForOption(CommandConstants.VerboseOption) ?? false;

        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentException.ThrowIfNullOrEmpty(srcFilePath);
        ArgumentException.ThrowIfNullOrEmpty(outFilePath);

        var logger = LogHelper.GetLogger(verbose);

        logger.Information("Start repair process for lang file...");

        await RepairLangFileAsync(root, srcFilePath, outFilePath, logger);
    }

    private static async Task RepairLangFileAsync(string root, string srcFilePath, string outFilePath, Logger logger)
    {
        root = $"{Path.GetFullPath(root)}{Path.DirectorySeparatorChar}";
        outFilePath = Path.GetFullPath(outFilePath);

        var langFilePath = Path.Combine(root, srcFilePath);
        langFilePath = Path.GetFullPath(langFilePath);

        ArgumentOutOfRangeException.ThrowIfEqual(File.Exists(langFilePath), false);

        var langFileContents = await File.ReadAllLinesAsync(langFilePath);
        var unusedKeys = await UnusedKeyCommand.ResolveAndCheckAsync(root, srcFilePath, logger);
        var unusedKeyPatterns = unusedKeys.Select(k => $"x:Key=\"{k}\"").ToFrozenSet();
        var isInCommentRange = false;
        var isInMultipleLineResourceRange = false;

        for (var lineNum = 0; lineNum < langFileContents.Length; lineNum++)
        {
            var line = langFileContents[lineNum];

            if (string.IsNullOrWhiteSpace(line)) continue;

            var trimmedLine = line.Trim();

            // jump out the comment range
            if (isInCommentRange && trimmedLine.EndsWith("-->", StringComparison.Ordinal))
            {
                isInCommentRange = false;
                continue;
            }

            // entering multi-line comment
            if (trimmedLine.StartsWith("<!--", StringComparison.Ordinal))
            {
                // skip comment line(s)
                if (!trimmedLine.EndsWith("-->", StringComparison.Ordinal))
                {
                    // this is a multi-line comment
                    isInCommentRange = true;
                    continue;
                }
            }

            var paddingStr = line[..^trimmedLine.Length];

            // jump out the multi-line resource range
            if (isInMultipleLineResourceRange && trimmedLine.EndsWith("</sys:String>", StringComparison.Ordinal))
            {
                langFileContents[lineNum] = $"{paddingStr}{trimmedLine} -->";
                isInMultipleLineResourceRange = false;
                continue;
            }

            // we skip this line if it is in comment range
            if (isInCommentRange) continue;

            var isMatched = unusedKeyPatterns.Any(p => line.Contains(p, StringComparison.OrdinalIgnoreCase));

            if (!isMatched) continue;

            // this resource is no longer used, so we add a comment to it
            var isOneLiner = trimmedLine.EndsWith("</sys:String>", StringComparison.Ordinal);

            logger.Debug("Found unused {Oneliner} key: {Line}",
                isOneLiner ? "one-liner" : string.Empty,
                trimmedLine);

            if (isOneLiner)
            {
                langFileContents[lineNum] = $"{paddingStr}<!-- {trimmedLine} -->";
                continue;
            }

            // this is a multi-line resource
            langFileContents[lineNum] = $"{paddingStr}<!-- {trimmedLine}";
            isInMultipleLineResourceRange = true;
        }

        logger.Information("Writing repaired lang file to {OutFilePath}", outFilePath);
        await File.WriteAllLinesAsync(outFilePath, langFileContents);
    }
}