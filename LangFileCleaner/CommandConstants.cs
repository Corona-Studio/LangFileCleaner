using LangFileCleaner.Helpers;
using System.CommandLine;

namespace LangFileCleaner;

public static class CommandConstants
{
    public static readonly Option VerboseOption =
        new Option<bool?>(["--verbose", "-v"], "Print Verbose log for debug.")
            .Required()
            .WithDefault(false);

    public static readonly Option OutFilePathOption =
        new Option<string>(["--out_file", "-o"], "Output path for the repaired Lang file.")
            .Required();

    public static class UnusedKeys
    {
        public static readonly Option RootPathOption =
            new Option<string>(["--root", "-r"], "Project root folder")
                .Required();

        public static readonly Option LangFilePathOption =
            new Option<string>(["--lang_file", "-f"], "Lang file for checking.")
                .Required();

        public static readonly Option FailWhenHasUnusedOption =
            new Option<bool?>(["--fail_unused", "-fu"], "Return non-zero code when has unused Lang keys.")
                .WithDefault(false);
    }

    public static class Sync
    {
        public static readonly Option SourceFilePathOption =
            new Option<string>(["--src", "-s"], "Lang file sync source.")
                .Required();

        public static readonly Option TargetFilePathOption =
            new Option<string>(["--target", "-t"], "Target file to sync to.")
                .Required();
    }
}