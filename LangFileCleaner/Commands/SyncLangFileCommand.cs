using System.Collections.Frozen;
using System.Collections.Immutable;
using LangFileCleaner.Helpers;
using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog.Core;
using System.Text.RegularExpressions;

namespace LangFileCleaner.Commands;

public static partial class SyncLangFileCommand
{
    [GeneratedRegex("x:Key=\"([\\W\\w]+)\"")]
    private static partial Regex KeyRegex { get; }

    public static Command GetCommand()
    {
        var unusedCommand = new Command("sync", "Using the source file to sync all the missing resource into the target Lang file.")
        {
            CommandConstants.Sync.SourceFilePathOption,
            CommandConstants.Sync.TargetFilePathOption,
            CommandConstants.OutFilePathOption,
            CommandConstants.VerboseOption
        };

        unusedCommand.SetHandler(HandleSyncLangFileAsync);

        return unusedCommand;
    }

    private static async Task HandleSyncLangFileAsync(InvocationContext context)
    {
        var srcFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.Sync.SourceFilePathOption);
        var targetFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.Sync.TargetFilePathOption);
        var outFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.OutFilePathOption);
        var verbose = (bool?)context.ParseResult.GetValueForOption(CommandConstants.VerboseOption) ?? false;

        ArgumentException.ThrowIfNullOrEmpty(srcFilePath);
        ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
        ArgumentException.ThrowIfNullOrEmpty(outFilePath);

        var logger = LogHelper.GetLogger(verbose);

        logger.Information("Start syncing lang file...");

        await SyncLangFIleAsync(srcFilePath, targetFilePath, outFilePath, logger);
    }

    private static void AddContent(List<string> resultFileContent, string[] contents, string keyName)
    {
        var keyInfo = LangFileHelper.GetResourceKeyStartIndex(contents, keyName);

        if (keyInfo.Oneliner)
        {
            resultFileContent.Add(contents[keyInfo.Index]);
            return;
        }

        // tag start
        resultFileContent.Add(contents[keyInfo.Index]);

        var targetContents = LangFileHelper.GetMultilineResourceContents(contents, keyInfo.Index).ToImmutableArray();
        resultFileContent.AddRange(targetContents);

        // tag end
        resultFileContent.Add(contents[keyInfo.Index + targetContents.Length + 1]);
    }

    private static async Task SyncLangFIleAsync(string srcFile, string targetFile, string outFile, Logger logger)
    {
        srcFile = Path.GetFullPath(srcFile);
        targetFile = Path.GetFullPath(targetFile);
        outFile = Path.GetFullPath(outFile);

        ArgumentOutOfRangeException.ThrowIfEqual(File.Exists(srcFile), false);
        ArgumentOutOfRangeException.ThrowIfEqual(File.Exists(targetFile), false);

        var srcFileKeys = LangFileHelper.ParseXamlFile(srcFile).Keys.ToFrozenSet();
        var targetFileKeys = LangFileHelper.ParseXamlFile(targetFile).Keys.ToFrozenSet();

        var srcFileContent = await File.ReadAllLinesAsync(srcFile);
        var targetFileContent = await File.ReadAllLinesAsync(targetFile);

        var resultFileContent = new List<string>();
        var isInCommentRange = false;

        for (var lineNum = 0; lineNum < srcFileContent.Length; lineNum++)
        {
            var line = srcFileContent[lineNum];

            if (string.IsNullOrWhiteSpace(line))
            {
                resultFileContent.Add(line);
                continue;
            }

            var trimmedLine = line.Trim();

            // jump out the comment range
            if (isInCommentRange && trimmedLine.EndsWith("-->", StringComparison.Ordinal))
            {
                resultFileContent.Add(line);
                isInCommentRange = false;
                continue;
            }

            // entering multi-line comment
            if (trimmedLine.StartsWith("<!--", StringComparison.Ordinal))
            {
                resultFileContent.Add(line);

                // skip comment line(s)
                if (!trimmedLine.EndsWith("-->", StringComparison.Ordinal))
                {
                    // this is a multi-line comment
                    isInCommentRange = true;
                    continue;
                }
            }

            // we skip this line if it is in comment range
            if (isInCommentRange)
            {
                resultFileContent.Add(line);
                continue;
            }

            var match = KeyRegex.Match(trimmedLine);

            // skip the line if it is not a key line
            if (!match.Success) continue;

            var keyName = match.Groups[1].Value;

            // If the target has the existing key, we are using this key and the value
            if (targetFileKeys.Contains(keyName))
            {
                AddContent(resultFileContent, targetFileContent, keyName);
                continue;
            }

            // If the target does not have the existing key, we are using this key and the value from the source
            AddContent(resultFileContent, srcFileContent, keyName);

            logger.Debug("Add missing key {KeyName} from source file", keyName);
        }

        await File.WriteAllLinesAsync(outFile, resultFileContent);
        logger.Information("Sync completed.");
    }
}