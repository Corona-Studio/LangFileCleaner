using System.Xml;

namespace LangFileCleaner.Helpers;

public static class LangFileHelper
{
    public static Dictionary<string, string> ParseXamlFile(string filePath)
    {
        var resourceDictionary = new Dictionary<string, string>();
        var dictionary = new XmlDocument();

        dictionary.Load(filePath);

        var nameSpace = new XmlNamespaceManager(dictionary.NameTable);
        nameSpace.AddNamespace("sys", "clr-namespace:System;assembly=System.Runtime");

        // Convert the dictionary to resx

        var dictionaryNodes = dictionary.SelectNodes("//sys:String", nameSpace);

        foreach (XmlNode node in dictionaryNodes)
        {
            var key = node.Attributes!["x:Key"]!.Value;
            var value = node.InnerText;

            resourceDictionary.Add(key, value);
        }

        return resourceDictionary;
    }

    public static IEnumerable<string> GetMultilineResourceContents(string[] contents, int startIndex)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(contents[startIndex].EndsWith("</sys:String>", StringComparison.Ordinal), true);

        // skip the first line
        startIndex++;

        while (!contents[startIndex].Trim().EndsWith("</sys:String>", StringComparison.Ordinal))
        {
            yield return contents[startIndex];
            startIndex++;
        }
    }

    public static (bool Oneliner, int Index) GetResourceKeyStartIndex(string[] contents, string key)
    {
        key = $"x:Key=\"{key}\"";

        for (var i = 0; i < contents.Length; i++)
        {
            var line = contents[i];
            if (line.Contains(key, StringComparison.OrdinalIgnoreCase))
                return (line.Trim().EndsWith("</sys:String>", StringComparison.Ordinal), i);
        }

        throw new ArgumentOutOfRangeException(nameof(key), "Key not found");
    }
}