using System.CommandLine;

namespace LangFileCleaner.Helpers;

public static class CommandHelper
{
    public static Option<T> Required<T>(this Option<T> option)
    {
        option.IsRequired = true;
        return option;
    }

    public static Option<T> WithDefault<T>(this Option<T> option, T? value)
    {
        option.SetDefaultValue(value);
        return option;
    }
}