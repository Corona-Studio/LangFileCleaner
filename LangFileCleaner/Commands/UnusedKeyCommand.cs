using System.Collections.Frozen;
using System.CommandLine;
using System.CommandLine.Invocation;
using LangFileCleaner.Helpers;
using Serilog.Core;

namespace LangFileCleaner.Commands;

public static class UnusedKeyCommand
{
    private static readonly FrozenSet<string> Exclusions =
    [
        "bin",
        "obj",
        "Assets/Language"
    ];

    private static readonly FrozenSet<string> MatchExts =
    [
        ".cs",
        ".axaml"
    ];

    private static readonly FrozenSet<string> SearchPatterns =
    [
        "{{DynamicResource {0}}}",
        "ResourceKey=\"{0}\"",
        "LangHelper.{0}",
        "ErrorMessageResourceName = \"{0}\"",
        "AddTitle(\"{0}\")",
        "AddDescription(\"{0}\")"
    ];

    public static Command GetCommand()
    {
        var unusedCommand = new Command("unused", "Check if there are unused keys in the Lang file.")
        {
            CommandConstants.UnusedKeys.RootPathOption,
            CommandConstants.UnusedKeys.LangFilePathOption,
            CommandConstants.UnusedKeys.FailWhenHasUnusedOption,
            CommandConstants.VerboseOption
        };

        unusedCommand.SetHandler(HandleUnusedKeyCheckAsync);

        return unusedCommand;
    }

    private static async Task HandleUnusedKeyCheckAsync(InvocationContext context)
    {
        var root = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.RootPathOption);
        var srcFilePath = (string?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.LangFilePathOption);
        var failOnUnused = (bool?)context.ParseResult.GetValueForOption(CommandConstants.UnusedKeys.FailWhenHasUnusedOption) ?? false;
        var verbose = (bool?)context.ParseResult.GetValueForOption(CommandConstants.VerboseOption) ?? false;

        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentException.ThrowIfNullOrEmpty(srcFilePath);

        var logger = LogHelper.GetLogger(verbose);
        var results = await ResolveAndCheckAsync(root, srcFilePath, logger);

        logger.Information("Unused keys: {@Result}", results);

        if (failOnUnused && results.Count != 0)
        {
            logger.Error("Lang file has unused lang keys, please use repair command to cleanup the file!");
            Environment.Exit(-1);
        }
    }

    private static FrozenSet<(string Raw, string Pattern)> GetNormalizedMatchPatterns(FrozenSet<string> langKeys)
    {
        return langKeys
            .Select(key => SearchPatterns.Select(p => (key, string.Format(p, key).Replace("{{", "{").Replace("}}", "}"))))
            .SelectMany(ps => ps)
            .ToFrozenSet();
    }

    private static async IAsyncEnumerable<string> SearchForPatternsAsync(FrozenSet<(string Raw, string Pattern)> patterns, string[] files, Logger logger)
    {
        foreach (var file in files)
        {
            logger.Debug("Searching in {File}", file);

            var content = await File.ReadAllLinesAsync(file);
            var matches = content
                .AsParallel()
                .Select(line => patterns.Select(p => line.Contains(p.Pattern, StringComparison.OrdinalIgnoreCase) ? p.Raw : null))
                .SelectMany(ms => ms)
                .OfType<string>()
                .Distinct()
                .ToArray();

            foreach (var match in matches) yield return match;
        }
    }

    public static async Task<FrozenSet<string>> ResolveAndCheckAsync(string root, string srcFilePath, Logger logger)
    {
        root = $"{Path.GetFullPath(root)}{Path.DirectorySeparatorChar}";

        var langFilePath = Path.Combine(root, srcFilePath);
        langFilePath = Path.GetFullPath(langFilePath);

        ArgumentOutOfRangeException.ThrowIfEqual(File.Exists(langFilePath), false);

        logger.Debug("Resolving lang keys from {LangFilePath}", langFilePath);

        var langKeys = LangFileHelper.ParseXamlFile(langFilePath).Keys.ToFrozenSet();

        ArgumentOutOfRangeException.ThrowIfEqual(langKeys.Count, 0);

        var normalizedMatchPatterns = GetNormalizedMatchPatterns(langKeys);

        logger.Debug("Searching for files in {Root}", root);

        // BFS
        var usedKeys = new HashSet<string>();
        var dirQueue = new Queue<string>([root]);

        while (dirQueue.Count != 0)
        {
            var dir = dirQueue.Dequeue();

            if (Exclusions.Any(exclude => dir[root.Length..].StartsWith(exclude, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (!Directory.Exists(dir))
                continue;

            logger.Debug("Searching in {Dir}", dir);

            var files = Directory.EnumerateFiles(dir)
                .Where(f => MatchExts.Contains(Path.GetExtension(f)))
                .ToArray();

            await foreach (var match in SearchForPatternsAsync(normalizedMatchPatterns, files, logger))
                usedKeys.Add(match);

            var dirs = Directory.EnumerateDirectories(dir);

            foreach (var subDir in dirs)
                dirQueue.Enqueue(subDir);
        }

        var resultDic = langKeys.ToDictionary(k => k, _ => 0);

        foreach (var key in usedKeys)
            resultDic[key]++;

        return resultDic
            .Where(p => p.Value == 0)
            .Select(p => p.Key)
            .ToFrozenSet();
    }
}