// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

if (args.Length < 2) return -1;

string fileName = args[0];
string internalName = args[1];

SingleVersionChangelog currentVersion = args.Length > 2
    ? ChangeLog.Entries.First(entry => entry.Version == new Version(args[2])) : ChangeLog.Entries[0];
StringBuilder programOutput = new();

foreach (ChangeLogEntry entry in currentVersion.NotableFeatures)
{
    AppendChangelogEntry(programOutput, entry);
}
foreach (ChangeLogEntry entry in currentVersion.MinorFeatures)
{
    AppendChangelogEntry(programOutput, entry);
}
if (currentVersion.HasKnownIssues)
{
    programOutput.AppendLine("Known Issues:");
    foreach (ChangeLogEntry entry in currentVersion.KnownIssues)
    {
        AppendChangelogEntry(programOutput, entry);
    }
}
Console.WriteLine(programOutput.ToString());
var manifest = new Deserializer().Deserialize<PluginManifest>(File.ReadAllText(fileName));
if (!manifest.internal_name.Equals(internalName)) return -1;
manifest.changelog = programOutput.ToString();
File.WriteAllText(fileName, YamlSerializer.Default.Serialize(manifest));
return 0;

void AppendChangelogEntry(StringBuilder output, ChangeLogEntry entry)
{
    output.AppendLine($"    {entry.Category.Localized()}: {entry.Description}");
    foreach (string bulletPoint in entry.BulletPoints)
    {
        output.AppendLine($"        - {bulletPoint}");
    }
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "NotAccessedField.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class PluginManifest
{
    public string author;
    public string changelog;
    public string description;
    public string internal_name;
    public string name;
    public string punchline;
    public string repo_url;
    public List<string> tags;
}

public class SortedTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector _innerTypeInspector;

    public SortedTypeInspector(ITypeInspector innerTypeInspector)
    {
        _innerTypeInspector = innerTypeInspector;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) =>
        _innerTypeInspector.GetProperties(type, container).Order(new CustomComparer());

    private class CustomComparer : IComparer<IPropertyDescriptor>
    {
        public int Compare(IPropertyDescriptor? x, IPropertyDescriptor? y) => Val(x) - Val(y);
        private int Val(IPropertyDescriptor? desc) => desc?.Name switch
        {
            "author"        => 0,
            "name"          => 1,
            "punchline"     => 2,
            "description"   => 3,
            "tags"          => 4,
            "internal_name" => 5,
            "repo_url"      => 6,
            "icon_url"      => 7,
            "image_urls"    => 8,
            "changelog"     => 9,
            _               => int.MaxValue,
        };
    }
}

public static class YamlSerializer
{
    public static readonly ISerializer Default =
        new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                               .WithTypeInspector(x => new SortedTypeInspector(x))
                               .Build();
}