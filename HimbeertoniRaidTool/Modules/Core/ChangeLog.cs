using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal class ChangeLog
{
    private readonly CoreConfig _config;
    public readonly ChangeLogUi Ui;
    public readonly List<SingleVersionChangelog> Entries = new()
    {
        new SingleVersionChangelog(new Version(1, 4, 1, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Lootmaster Ui crashing or being partly empty", 130),
            },
        },
        new SingleVersionChangelog(new Version(1, 4, 1, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Lootmaster crashing and spamming log"),
                new ChangeLogEntry("Bugfix", "Newly created players were potentially not saved correctly"),
            },
        },
        new SingleVersionChangelog(new Version(1, 4, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("Feature", "You can now track multiple characters per player"),
                new ChangeLogEntry("Lootmaster Ui", "Reworked group view to improve user experience")
                {
                    BulletPoints =
                    {
                        "One click reordering of players",
                        "Create new player from target",
                        "Quickly add existing players or characters from the database",
                    },
                },
            },
            MinorFeatures =
            {
                new ChangeLogEntry("Ui", "Reworked windows for editing players and characters"),
                new ChangeLogEntry("Bugfix", "Corrected behaviour when deleting main job"),
            },
        },
        new SingleVersionChangelog(new Version(1, 4, 0, 0))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("System", "Changed how groups and players are stored"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 4, 1))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("BiS", "Switched to using BiS sets curated by etro.gg"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry("BiS", "Can create BiS from etro link as well as the etro id"),
                new ChangeLogEntry("BiS", "Fixed an issue with BiS being empty for new jobs"),
                new ChangeLogEntry("BiS", "Removed user curated defaults from config"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 4, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("General", "Updated for FFXIV 6.5"),
                new ChangeLogEntry("General", "Updated for Dalamud API 9"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 3, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("Ui", "You can now manage jobs directly in solo and detail view"),
                new ChangeLogEntry("LootSession", "You can ignore players/jobs based on certain rules"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry("Ui", "Old Examine button is now Quick Compare"),
                new ChangeLogEntry("Bugfix", "Fixed loot rule \"Can Buy\""),
                new ChangeLogEntry("Options", "Reworked Ui for loot rules"),
                new ChangeLogEntry("LootMaster", "You can now edit name + role priority for the Solo group"),
                // ReSharper disable once StringLiteralTypo
                new ChangeLogEntry("Translation", "Updated French translation (Thanks to Arganier)"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 2, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("Loot session", "Added new rules")
                {
                    BulletPoints =
                    {
                        "\"Can use now\" (Tome Upgrades)",
                        "\"Can buy\" (for books)",
                    },
                },
                new ChangeLogEntry("Gear sets", "You can now delete gear sets"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry("Database", "Remove unused entries (old gear sets and characters)"),
                new ChangeLogEntry("Etro.gg", "Import crafted items as HQ again  (broken since 1.2.x)", 126),
                new ChangeLogEntry("Bugfix", "Fix extremely rare crash on startup"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 1, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Fix a crash when using wine"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("Loot Session", "%DPS gain now properly takes SKS/SPS into account"),
                new ChangeLogEntry("Bugfix",
                    "Fixed wrong stat calculations due to unintentionally capping stats lower than intended"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Only cap applicable stats on items"),
                new ChangeLogEntry("Loot Session", "Removed manually curated DPS for players"),
                new ChangeLogEntry("Edit Gear",
                    "Properly handle local and etro.gg sets (Etro sets cannot be edited and need to be converted to local to edit)"),
                new ChangeLogEntry("Edit Gear", "Slightly reworked Ui"),
                new ChangeLogEntry("Edit Gear", "You can now edit the names of sets"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 0, 5))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Merge infos for multiple database entries for one character", 123),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 0, 4))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Multiple characters unintentionally sharing gear sets", 124),
                new ChangeLogEntry("LootSession", "Rings can now be assigned to a slot explicitly"),
                new ChangeLogEntry("Ui", "Added button to update BiS in group overview"),
                new ChangeLogEntry("BiS", "Always update empty sets (with valid ID) at startup"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 0, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("Bugfix", "Corrected loot for Anabaseios Savage (Thanks to Zeppy for helping)"),
                new ChangeLogEntry("Bugfix", "Dungeon/Trial Gear is now shown correctly"),
                new ChangeLogEntry("Bugfix", "Fixed an issue with potentially overriding gear sets (since 1.2.x.x)"),
                new ChangeLogEntry("BiS", "Added more BiS (AST, SCH, SGE)"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 0, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry("BiS", "Added available BiS sets"),
                new ChangeLogEntry("Bugfix", "Ring coffer now actually contains rings"),
            },
        },
        new SingleVersionChangelog(new Version(1, 3, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry("General", "Updated for 6.4 (Added Anabaseios Raids)"),
            },
        },
    };
    internal ChangeLog(IHrtModule module, CoreConfig config)
    {
        _config = config;
        Ui = new ChangeLogUi(this);
        module.WindowSystem.AddWindow(Ui);
    }
}

internal readonly struct SingleVersionChangelog
{
    public Version Version { get; }
    public List<ChangeLogEntry> NotableFeatures { get; } = new();
    public List<ChangeLogEntry> MinorFeatures { get; } = new();
    public bool HasNotableFeatures => NotableFeatures.Count > 0;
    public bool HasMinorFeatures => MinorFeatures.Count > 0;
    public SingleVersionChangelog(Version version)
    {
        Version = version;
    }
}

internal readonly struct ChangeLogEntry
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IList<string> BulletPoints { get; } = new List<string>();
    public int GitHubIssueNumber { get; }
    public ChangeLogEntry(string category, string description, int issueNr = 0)
    {
        Category = category;
        Description = description;
        GitHubIssueNumber = issueNr;
    }
}

internal enum ChangelogShowOptions
{
    ShowAll = 0,
    ShowNotable = 10,
    ShowNone = 100,
}