using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using static HimbeertoniRaidTool.Plugin.Modules.Core.ChangeLogEntryCategory;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

public class ChangeLog
{
    public static readonly IReadOnlyList<SingleVersionChangelog> Entries = new List<SingleVersionChangelog>
    {
        new(new Version(1, 8, 0, 4))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(
                    General,
                    "Home world transfer /name changes of known characters are now automatically applied with gear updates"),
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "Some changes to item handling which should reduce size of database files"),
            },
        },
        new(new Version(1, 8, 0, 3))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(
                    NewFeature,
                    "Stats can now take a party bonus into account (adjust the default in settings)")
                {
                    NewSetting = true,
                },
            },
        },
        new(new Version(1, 8, 0, 2))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(UserInterface, "Add quick select for job/character/player (similar to gear sets)"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "Improved usability of BiS section in gear edit"),
                new ChangeLogEntry(
                    Bugfix, "2nd,3rd,... set from XivGear.app sheets are now fetched correctly, not always the 1st"),
            },
        },
        new(new Version(1, 8, 0, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "Added food information wherever relevant"),
                new ChangeLogEntry(UserInterface, "Tooltips for food now contain the food's effects"),
                new ChangeLogEntry(Bugfix, "Fix Gear/BiS equality with materia enabled"),
                new ChangeLogEntry(Bugfix, "Fixed crash on startup in certain cases"),
            },
        },
        new(new Version(1, 8, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(
                    Gear,
                    "GearSets now support selection of food. You need to update all set from etro.gg and XivGear.app to get the food"),
                new ChangeLogEntry(Options, "You can trigger updates for all gear sets from the options")
                {
                    NewSetting = true,
                },
                new ChangeLogEntry(Bugfix, "XivGear.app sets now behave as expected (on update and edit)"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "A lot of changes on underlying systems. If you notice anything behaving strangely let me know via the official discord"),
            },
        },
        new(new Version(1, 7, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(
                    NewFeature,
                    "Gearsets can have an alias. If an alias is set it is shown instead of the name in the Ui\n"
                  + "This is editable even for sets from etro or XivGearApp"),
                new ChangeLogEntry(UserInterface, "Gearset origin is now shown in selection dropdown"),
                new ChangeLogEntry(General, "Updated for 7.1"),
                new ChangeLogEntry(UserInterface, "New layout for the stats"),
                new ChangeLogEntry(UserInterface, "Overhauled item tooltips"),
                new ChangeLogEntry(Bugfix, "Deleted sets get correctly removed from the gear/bis list"),
                new ChangeLogEntry(Bis, "Update gear set buttons now works for XivGearApp (like they did for etro)"),
                new ChangeLogEntry(UserInterface, "Searching gear from database now actually shows useful information"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "New layout for inventory/wallet"),
                new ChangeLogEntry(UserInterface,
                                   "Job selection buttons are bigger to have enough space for level 100 jobs"),
            },
            KnownIssues =
            {
                new ChangeLogEntry(KnownIssues, "Previous wallet and inventory data is lost"),
            },
        },
        new(new Version(1, 6, 2, 10))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(NewFeature, "Automatically switch character in solo tab to currently logged in one"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Ignoring lower ilvl gear works again"),
            },
        },
        new(new Version(1, 6, 2, 9))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(
                    Bugfix,
                    $"\"{CoreLoc.ConfigUi_cb_ignorePrevTierGear}\" option now works correctly if last raid tier was in a previous expansion"),
                new ChangeLogEntry(
                    Bugfix,
                    $"Updating gear by examining now works again if \"{CoreLoc.ConfigUi_cb_ownData}\" is disbaled"),
            },
        },
        new(new Version(1, 6, 2, 8))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(UserInterface,
                                   "You can now adjust the way character names are displayed (see config)"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "Make all buttons accessible in smaller windows"),
            },
        },
        new(new Version(1, 6, 2, 7))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fix update from lodestone", 151),
            },
        },
        new(new Version(1, 6, 2, 6))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fix loot being inaccurate"),
            },
        },
        new(new Version(1, 6, 2, 5))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fix missing materia in etro sets"),
            },
        },
        new(new Version(1, 6, 2, 4))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fix wrongly displayed Tome gear"),
            },
        },
        new(new Version(1, 6, 2, 3))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Lootmaster, "Add savage loot information"),
            },
        },
        new(new Version(1, 6, 2, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fix being unable to change tabs in config"),
            },
        },
        new(new Version(1, 6, 2, 1))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(Bis, "Add auto updates for XivGear.app (see config)"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "Added headlines to multi item tooltips"),
            },
        },
        new(new Version(1, 6, 2, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(General, "Now supports switching back to Endwalker raid tiers"),
                new ChangeLogEntry(Bis, "Added support for XivGear.app"),
                new ChangeLogEntry(KnownIssues, "XivGear.app sets are not automatically updated yet"),
            },
        },
        new(new Version(1, 6, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(General, "Added loot information for normal raids and extremes"),
                new ChangeLogEntry(General, "Corrected HP calculation for levels above 90"),
                new ChangeLogEntry(Lodestone, "PCT and VIP fixed"),
            },
        },
        new(new Version(1, 6, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(General, "Updated for 7.0"),
                new ChangeLogEntry(Bis, "Automatically converts non existent etro sets to local sets"),
                new ChangeLogEntry(
                    KnownIssues, "Item categorization and raid infos will be amended once available"),
                new ChangeLogEntry(KnownIssues, "Stat calculations (e.g. HP) are most likely not correct yet"),
            },
        },
        new(new Version(1, 5, 3, 0))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "Changes to data storage (drops support for data from versions < 1.4.0)"),
                new ChangeLogEntry(UserInterface, "New interface for searching characters from database"),
                new ChangeLogEntry(General, "Fix \"Dmg\" Calc being slightly off"),
                new ChangeLogEntry(UserInterface, "Some minor Ui improvements"),
            },
        },
        new(new Version(1, 5, 2, 6))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fixes being unable to add new gear sets"),
                new ChangeLogEntry(UserInterface,
                                   "Add job selection (for BiS and item selection) to gear set edit user interface"),
            },
        },
        new(new Version(1, 5, 2, 5))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(
                    Bugfix,
                    "Fixed broken materia in etro.gg sets\n(affected sets need to be updated by pressing the button\n  or automatic updates if activated)"),
            },
        },
        new(new Version(1, 5, 2, 3))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(Translation, "Fixed edit buttons tooltip to not say \"add\""),
                new ChangeLogEntry(Gear,
                                   "You can restrict automatic overrides for irrelevant gear (see config)"),
                new ChangeLogEntry(Translation, "German (Deutsch) translation updated"),
            },
        },
        new(new Version(1, 5, 2, 2))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(Bugfix, "Selecting gear from database now works"),
                new ChangeLogEntry(Bugfix, "Adding gear sets in solo view works again"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(General,
                                   "Gear updates by examining now use the same restrictions as own data collection"),
                new ChangeLogEntry(Translation, "Redone translation"),
            },
        },
        new(new Version(1, 5, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(Bis, "Add support for relic weapons in etro.gg sets"),
                new ChangeLogEntry(UserInterface, "Added ability to change relic stats when editing gear"),
                new ChangeLogEntry(General,
                                   "You can now specify which types of jobs get automatically updated/created.\n"
                                 + "If you want single jobs to not show up, you can hide these in character edit"),
            },
            KnownIssues =
            {
                new ChangeLogEntry(Gear,
                                   "Stats for relic weapons are not correctly read from the Lodestone or Examine.\n"
                                 + "Your manual edits will NOT be overwritten"),
            },
        },
        new(new Version(1, 5, 0, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(General, "Remove unused gear sets from database"),
                new ChangeLogEntry(Bugfix,
                                   "You are now able to change to gear sets with the same name"),
                new ChangeLogEntry(Bugfix,
                                   "Automatically updated gear was sometimes not saved correctly"),
                new ChangeLogEntry(Performance, "Optimized load time on slow connections"),
            },
        },
        new(new Version(1, 5, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(NewFeature, "Manage multiple gear/bis sets per job", 132),
                new ChangeLogEntry(NewFeature,
                                   "Automatically updates own characters data (can be disabled in the config)"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "Made it more pretty"),
                new ChangeLogEntry(UserInterface, "You can now hide jobs (select classes when editing a character)"),
                new ChangeLogEntry(General, "Correctly handle materia for previous expansions"),
            },
        },
        new(new Version(1, 4, 2, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix,
                                   "Adding new players from target resulted in an empty player"),
                new ChangeLogEntry(General, "Changed command in new user window", 127),
            },
        },
        new(new Version(1, 4, 2, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(NewFeature, "Added in-game changelog"),
            },
        },
        new(new Version(1, 4, 1, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Lootmaster Ui crashing or being partly empty", 130),
            },
        },
        new(new Version(1, 4, 1, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Lootmaster crashing and spamming log"),
                new ChangeLogEntry(Bugfix,
                                   "Newly created players were potentially not saved correctly"),
            },
        },
        new(new Version(1, 4, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(NewFeature,
                                   "You can now track multiple characters per player"),
                new ChangeLogEntry(UserInterface, "Reworked group view to improve user experience")
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
                new ChangeLogEntry(UserInterface, "Reworked windows for editing players and characters"),
                new ChangeLogEntry(Bugfix, "Corrected behaviour when deleting main job"),
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
                new ChangeLogEntry(Bis, "Switched to using BiS sets curated by etro.gg"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(Bis, "Can create BiS from etro link as well as the etro id"),
                new ChangeLogEntry(Bis, "Fixed an issue with BiS being empty for new jobs"),
                new ChangeLogEntry(Bis, "Removed user curated defaults from config"),
            },
        },
        new(new Version(1, 3, 4, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(General, "Updated for FFXIV 6.5"),
                new ChangeLogEntry(General, "Updated for Dalamud API 9"),
            },
        },
        new(new Version(1, 3, 3, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(UserInterface,
                                   "You can now manage jobs directly in solo and detail view"),
                new ChangeLogEntry(LootSession,
                                   "You can ignore players/jobs based on certain rules"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(UserInterface, "Old Examine button is now Quick Compare"),
                new ChangeLogEntry(Bugfix, "Fixed loot rule \"Can Buy\""),
                new ChangeLogEntry(Options, "Reworked Ui for loot rules"),
                new ChangeLogEntry(Lootmaster,
                                   "You can now edit name + role priority for the Solo group"),
                // ReSharper disable once StringLiteralTypo
                new ChangeLogEntry(Translation,
                                   "Updated French translation (Thanks to Arganier)"),
            },
        },
        new(new Version(1, 3, 2, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(LootSession, "Added new rules")
                {
                    BulletPoints =
                    {
                        "\"Can use now\" (Tome Upgrades)",
                        "\"Can buy\" (for books)",
                    },
                },
                new ChangeLogEntry(General, "You can now delete gear sets"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "Remove unused entries from database (old gear sets and characters)"),
                new ChangeLogEntry(Bis,
                                   "Import crafted items as HQ from etro.gg  (broken since 1.2.x)", 126),
                new ChangeLogEntry(Bugfix, "Fix extremely rare crash on startup"),
            },
        },
        new(new Version(1, 3, 1, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Fix a crash when using wine"),
            },
        },
        new(new Version(1, 3, 1, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(LootSession,
                                   "%DPS gain now properly takes SKS/SPS into account"),
                new ChangeLogEntry(Bugfix,
                                   "Fixed wrong stat calculations due to unintentionally capping stats lower than intended"),
            },
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix, "Only cap applicable stats on items"),
                new ChangeLogEntry(LootSession, "Removed manually curated DPS for players"),
                new ChangeLogEntry(ChangeLogEntryCategory.System,
                                   "Properly handle local and etro.gg sets (Etro sets cannot be edited and need to be converted to local to edit)"),
                new ChangeLogEntry(UserInterface, "Slightly reworked Ui for editing gear"),
                new ChangeLogEntry(General, "You can now edit the names of gear sets"),
            },
        },
        new(new Version(1, 3, 0, 5))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix,
                                   "Merge infos for multiple database entries for one character", 123),
            },
        },
        new(new Version(1, 3, 0, 4))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix,
                                   "Multiple characters unintentionally sharing gear sets", 124),
                new ChangeLogEntry(LootSession,
                                   "Rings can now be assigned to a slot explicitly"),
                new ChangeLogEntry(UserInterface, "Added button to update BiS in group overview"),
                new ChangeLogEntry(Bis, "Always update empty sets (with valid ID) at startup"),
            },
        },
        new(new Version(1, 3, 0, 2))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bugfix,
                                   // ReSharper disable once StringLiteralTypo
                                   "Corrected loot for Anabaseios Savage (Thanks to Zeppy for helping)"),
                new ChangeLogEntry(Bugfix, "Dungeon/Trial Gear is now shown correctly"),
                new ChangeLogEntry(Bugfix,
                                   "Fixed an issue with potentially overriding gear sets (since 1.2.x.x)"),
                new ChangeLogEntry(Bis, "Added more BiS (AST, SCH, SGE)"),
            },
        },
        new(new Version(1, 3, 0, 1))
        {
            MinorFeatures =
            {
                new ChangeLogEntry(Bis, "Added available BiS sets"),
                new ChangeLogEntry(Bugfix, "Ring coffer now actually contains rings"),
            },
        },
        new(new Version(1, 3, 0, 0))
        {
            NotableFeatures =
            {
                new ChangeLogEntry(General, "Updated for 6.4 (Added Anabaseios Raids)"),
            },
        },
    };
    private readonly ChangeLogUi _ui;
    public readonly IConfigOptions Config;
    public ChangeLog(IHrtModule module, IConfigOptions config)
    {
        Config = config;
        _ui = new ChangeLogUi(module.Services.UiSystem, this);
        module.UiReady += OnStartup;
    }
    public static Version CurrentVersion => Entries[0].Version;
    public IEnumerable<SingleVersionChangelog> UnseenChangeLogs =>
        Entries.Where(e => e.Version > Config.LastSeenChangelog);
    public IEnumerable<SingleVersionChangelog> SeenChangeLogs =>
        Entries.Where(e => e.Version <= Config.LastSeenChangelog);
    public void ShowUi() => _ui.Show();
    private void OnStartup()
    {
        if (Config.LastSeenChangelog is { Major: 0, Minor: 0, Revision: 0, Build: 0 })
            Config.LastSeenChangelog = Entries.First(e => e.Version.Minor != CurrentVersion.Minor).Version;
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

public readonly struct SingleVersionChangelog(Version version)
{
    public Version Version { get; } = version;
    public List<ChangeLogEntry> NotableFeatures { get; } = [];
    public List<ChangeLogEntry> MinorFeatures { get; } = [];
    public List<ChangeLogEntry> KnownIssues { get; } = [];

    public bool HasNotableFeatures => NotableFeatures.Count > 0;
    public bool HasMinorFeatures => MinorFeatures.Count > 0;
    public bool HasKnownIssues => KnownIssues.Count > 0;
}

public readonly struct ChangeLogEntry(ChangeLogEntryCategory category, string description, int issueNr = 0)
{
    public ChangeLogEntryCategory Category { get; init; } = category;
    public string Description { get; init; } = description;
    public IList<string> BulletPoints { get; } = new List<string>();
    public int GitHubIssueNumber { get; } = issueNr;
    public bool HasGitHubIssue => GitHubIssueNumber > 0;

    public bool NewSetting { get; init; } = false;
}

public enum ChangeLogEntryCategory
{
    General,
    NewFeature,
    Bugfix,
    Options,
    UserInterface,
    Lootmaster,
    LootSession,
    Bis,
    System,
    Translation,
    Performance,
    Gear,
    KnownIssues,
    Lodestone,
}

public enum ChangelogShowOptions
{
    ShowAll = 0,
    ShowNotable = 10,
    ShowNone = 100,
}