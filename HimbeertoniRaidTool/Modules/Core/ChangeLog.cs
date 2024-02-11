﻿using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

public class ChangeLog
{
    public static readonly IReadOnlyList<SingleVersionChangelog> Entries = new List<SingleVersionChangelog>
    {
        new(new Version(1, 5, 2, 2))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Selecting gear from database now works"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Adding gear sets in solo view works again"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.General,
                                   "Gear updates by examining now use the same restrictions as own data collection"),
                new ChangeLogEntry(ChangeLogEntryCategory.Translation, "Redone translation"),
            },
        },
        new(new Version(1, 5, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Add support for relic weapons in etro.gg sets"),
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Added ability to change relic stats when editing gear"),
                new ChangeLogEntry(ChangeLogEntryCategory.General,
                                   "You can now specify which types of jobs get automatically updated/created.\n"
                                 + "If you want single jobs to not show up, you can hide these in character edit"),
            },
            KnownIssues =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Gear,
                                   "Stats for relic weapons are not correctly read from the Lodestone or Examine.\n"
                                 + "Your manual edits will NOT be overwritten"),
            },
        },
        new(new Version(1, 5, 0, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.General, "Remove unused gear sets from database"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "You are now able to change to gear sets with the same name"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Automatically updated gear was sometimes not saved correctly"),
                new ChangeLogEntry(ChangeLogEntryCategory.Performance, "Optimized load time on slow connections"),
            },
        },
        new(new Version(1, 5, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.NewFeature, "Manage multiple gear/bis sets per job", 132),
                new ChangeLogEntry(ChangeLogEntryCategory.NewFeature,
                                   "Automatically updates own characters data (can be disabled in the config)"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Made it more pretty"),
                new ChangeLogEntry(ChangeLogEntryCategory.Ui,
                                   "You can now hide jobs (select classes when editing a character)"),
                new ChangeLogEntry(ChangeLogEntryCategory.General, "Correctly handle materia for previous expansions"),
            },
        },
        new(new Version(1, 4, 2, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Adding new players from target resulted in an empty player"),
                new ChangeLogEntry(ChangeLogEntryCategory.General, "Changed command in new user window", 127),
            },
        },
        new(new Version(1, 4, 2, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.NewFeature, "Added in-game changelog"),
            },
        },
        new(new Version(1, 4, 1, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Lootmaster Ui crashing or being partly empty", 130),
            },
        },
        new(new Version(1, 4, 1, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Lootmaster crashing and spamming log"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Newly created players were potentially not saved correctly"),
            },
        },
        new(new Version(1, 4, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.NewFeature,
                                   "You can now track multiple characters per player"),
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Reworked group view to improve user experience")
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
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Reworked windows for editing players and characters"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Corrected behaviour when deleting main job"),
            },
        },
        new(new Version(1, 4, 0, 0))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.System, "Changed how groups and players are stored"),
            },
        },
        new(new Version(1, 3, 4, 1))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Switched to using BiS sets curated by etro.gg"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Can create BiS from etro link as well as the etro id"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Fixed an issue with BiS being empty for new jobs"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Removed user curated defaults from config"),
            },
        },
        new(new Version(1, 3, 4, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.General, "Updated for FFXIV 6.5"),
                new ChangeLogEntry(ChangeLogEntryCategory.General, "Updated for Dalamud API 9"),
            },
        },
        new(new Version(1, 3, 3, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Ui,
                                   "You can now manage jobs directly in solo and detail view"),
                new ChangeLogEntry(ChangeLogEntryCategory.LootSession,
                                   "You can ignore players/jobs based on certain rules"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Old Examine button is now Quick Compare"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Fixed loot rule \"Can Buy\""),
                new ChangeLogEntry(ChangeLogEntryCategory.Options, "Reworked Ui for loot rules"),
                new ChangeLogEntry(ChangeLogEntryCategory.Lootmaster,
                                   "You can now edit name + role priority for the Solo group"),
                // ReSharper disable once StringLiteralTypo
                new ChangeLogEntry(ChangeLogEntryCategory.Translation,
                                   "Updated French translation (Thanks to Arganier)"),
            },
        },
        new(new Version(1, 3, 2, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.LootSession, "Added new rules")
                {
                    BulletPoints =
                    {
                        "\"Can use now\" (Tome Upgrades)",
                        "\"Can buy\" (for books)",
                    },
                },
                new ChangeLogEntry(ChangeLogEntryCategory.General, "You can now delete gear sets"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "Remove unused entries from database (old gear sets and characters)"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bis,
                                   "Import crafted items as HQ from etro.gg  (broken since 1.2.x)", 126),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Fix extremely rare crash on startup"),
            },
        },
        new(new Version(1, 3, 1, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Fix a crash when using wine"),
            },
        },
        new(new Version(1, 3, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.LootSession,
                                   "%DPS gain now properly takes SKS/SPS into account"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Fixed wrong stat calculations due to unintentionally capping stats lower than intended"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Only cap applicable stats on items"),
                new ChangeLogEntry(ChangeLogEntryCategory.LootSession, "Removed manually curated DPS for players"),
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "Properly handle local and etro.gg sets (Etro sets cannot be edited and need to be converted to local to edit)"),
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Slightly reworked Ui for editing gear"),
                new ChangeLogEntry(ChangeLogEntryCategory.General, "You can now edit the names of gear sets"),
            },
        },
        new(new Version(1, 3, 0, 5))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Merge infos for multiple database entries for one character", 123),
            },
        },
        new(new Version(1, 3, 0, 4))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Multiple characters unintentionally sharing gear sets", 124),
                new ChangeLogEntry(ChangeLogEntryCategory.LootSession,
                                   "Rings can now be assigned to a slot explicitly"),
                new ChangeLogEntry(ChangeLogEntryCategory.Ui, "Added button to update BiS in group overview"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Always update empty sets (with valid ID) at startup"),
            },
        },
        new(new Version(1, 3, 0, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Corrected loot for Anabaseios Savage (Thanks to Zeppy for helping)"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Dungeon/Trial Gear is now shown correctly"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix,
                                   "Fixed an issue with potentially overriding gear sets (since 1.2.x.x)"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Added more BiS (AST, SCH, SGE)"),
            },
        },
        new(new Version(1, 3, 0, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.Bis, "Added available BiS sets"),
                new ChangeLogEntry(ChangeLogEntryCategory.Bugfix, "Ring coffer now actually contains rings"),
            },
        },
        new(new Version(1, 3, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.General, "Updated for 6.4 (Added Anabaseios Raids)"),
            },
        },
    };
    private readonly ChangeLogUi _ui;
    public readonly IConfigOptions Config;
    public ChangeLog(IHrtModule module, IConfigOptions config)
    {
        Config = config;
        _ui = new ChangeLogUi(this);
        module.WindowSystem.AddWindow(_ui);
        module.UiReady += OnStartup;
    }
    public Version CurrentVersion => Entries[0].Version;
    public Version LastMinor => Entries.First(e => e.Version.Minor != CurrentVersion.Minor).Version;
    public IEnumerable<SingleVersionChangelog> UnseenChangeLogs =>
        Entries.Where(e => e.Version > Config.LastSeenChangelog);
    public IEnumerable<SingleVersionChangelog> SeenChangeLogs =>
        Entries.Where(e => e.Version <= Config.LastSeenChangelog);
    public void ShowUi() => _ui.Show();
    private void OnStartup()
    {
        if (Config.LastSeenChangelog is { Major: 0, Minor: 0, Revision: 0, Build: 0 })
            Config.LastSeenChangelog = LastMinor;
        switch (Config.ChangelogNotificationOptions)
        {
            case ChangelogShowOptions.ShowAll when UnseenChangeLogs.Any():
            case ChangelogShowOptions.ShowNotable
                when UnseenChangeLogs.Any(e => e.HasNotableFeatures):
                _ui.Show();
                break;
            case ChangelogShowOptions.ShowNone:
                Config.LastSeenChangelog = CurrentVersion;
                break;
            default:
                return;
        }
    }
    public void Dispose(IHrtModule module) => module.UiReady -= OnStartup;

    public interface IConfigOptions
    {
        public Version LastSeenChangelog { get; set; }
        public ChangelogShowOptions ChangelogNotificationOptions { get; set; }
    }
}

public readonly struct SingleVersionChangelog
{
    public Version Version { get; }
    public List<ChangeLogEntry> NotableFeatures { get; } = new();
    public List<ChangeLogEntry> MinorFeatures { get; } = new();
    public List<ChangeLogEntry> KnownIssues { get; } = new();

    public bool HasNotableFeatures => NotableFeatures.Count > 0;
    public bool HasMinorFeatures => MinorFeatures.Count > 0;
    public bool HasKnownIssues => KnownIssues.Count > 0;
    public SingleVersionChangelog(Version version)
    {
        Version = version;
    }
}

public readonly struct ChangeLogEntry
{
    public ChangeLogEntryCategory Category { get; init; }
    public string Description { get; init; }
    public IList<string> BulletPoints { get; } = new List<string>();
    public int GitHubIssueNumber { get; }
    public bool HasGitHubIssue => GitHubIssueNumber > 0;
    public ChangeLogEntry(ChangeLogEntryCategory category, string description, int issueNr = 0)
    {
        Category = category;
        Description = description;
        GitHubIssueNumber = issueNr;
    }
}

public enum ChangeLogEntryCategory
{
    General,
    NewFeature,
    Bugfix,
    Options,
    Ui,
    Lootmaster,
    LootSession,
    Bis,
    System,
    Translation,
    Performance,
    Gear,
}

public enum ChangelogShowOptions
{
    ShowAll = 0,
    ShowNotable = 10,
    ShowNone = 100,
}