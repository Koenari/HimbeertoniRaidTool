using System.Numerics;
using Dalamud.Interface;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace HimbeertoniRaidTool.Plugin.UI;

public class EditWindowFactory(IHrtModule module)
{
    private IHrtModule Module => module;
    private HrtDataManager DataManager => Module.Services.HrtDataManager;
    public void Create<TData>(HrtId id, Action<TData>? onSave = null,
                              Action? onCancel = null, Action? onDelete = null,
                              object? param = null)
        where TData : IHasHrtId, new()
    {
        var type = new TData().IdType;
        if (id.Type != type) return;
        switch (type)
        {
            case HrtId.IdType.Group:
                if (DataManager.RaidGroupDb.TryGet(id, out var group))
                    Create(group, onSave as Action<RaidGroup>, onCancel, onDelete, param);
                break;
            case HrtId.IdType.Player:
                if (DataManager.PlayerDb.TryGet(id, out var player))
                    Create(player, onSave as Action<Player>, onCancel, onDelete, param);
                break;
            case HrtId.IdType.Character:
                if (DataManager.CharDb.TryGet(id, out var character))
                    Create(character, onSave as Action<Character>, onCancel, onDelete, param);
                break;
            case HrtId.IdType.Gear:
                if (DataManager.GearDb.TryGet(id, out var gearSet))
                    Create(gearSet, onSave as Action<GearSet>, onCancel, onDelete, param);
                break;
            case HrtId.IdType.None:
            default:
                return;
        }
    }
    public void Create<TData>(TData data, Action<TData>? onSave = null,
                              Action? onCancel = null, Action? onDelete = null,
                              object? param = null)
        where TData : IHasHrtId
    {
        HrtWindow? window = data.IdType switch
        {
            HrtId.IdType.Player when data is Player p => new EditPlayerWindow(this,
                                                                              p, onSave as Action<Player>, onCancel,
                                                                              onDelete),
            HrtId.IdType.Character when data is Character c => new EditCharacterWindow(this,
                c, onSave as Action<Character>, onCancel, onDelete),
            HrtId.IdType.Gear when data is GearSet gs => new EditGearSetWindow(this,
                                                                               gs, onSave as Action<GearSet>, onCancel,
                                                                               onDelete, param as Job?),
            HrtId.IdType.Group when data is RaidGroup rg => new EditGroupWindow(this,
                rg, onSave as Action<RaidGroup>, onCancel, onDelete),
            HrtId.IdType.None => null,
            _                 => null,
        };
        if (window == null) return;
        module.WindowSystem.AddWindow(window);
        window.Show();

    }

    private abstract class EditWindow<TData> : HrtWindowWithModalChild where TData : IHrtDataTypeWithId
    {
        private readonly Action? _onCancel;
        private readonly Action<TData>? _onSave;
        private readonly Action? _onDelete;
        protected bool CanDelete = false;
        protected readonly EditWindowFactory Factory;
        private TData _original;
        protected TData DataCopy;

        protected EditWindow(EditWindowFactory factory, TData original, Action<TData>? onSave, Action? onCancel,
                             Action? onDelete)
        {
            Factory = factory;
            _original = original;
            DataCopy = _original.Clone();
            _onCancel = onCancel;
            _onSave = onSave;
            _onDelete = onDelete;
            Title = string.Format(GeneralLoc.EditUi_Title, _original.DataTypeName, _original.Name)
                          .CapitalizedSentence();
        }
        public override sealed void Draw()
        {
            //Buttons
            if (ImGuiHelper.SaveButton())
            {
                Save(_original);
                _onSave?.Invoke(_original);
                Hide();
            }

            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
            {
                Cancel();
                _onCancel?.Invoke();
                Hide();
            }
            if (CanDelete)
            {
                ImGui.SameLine();
                if (ImGuiHelper.DeleteButton(_original))
                {
                    _onDelete?.Invoke();
                    Hide();
                }
            }
            ImGui.SameLine();
            ImGui.Text($"{GeneralLoc.EditUi_Txt_LocalId}: {(_original.LocalId.IsEmpty ? "-" : _original.LocalId)}");
            ImGui.Separator();
            ImGui.NewLine();
            DrawContent();
        }
        protected abstract void DrawContent();
        protected void ReplaceOriginal(TData newOrg)
        {
            _original = newOrg;
            DataCopy = _original.Clone();
        }

        protected abstract void Save(TData destination);
        protected abstract void Cancel();
    }

    private class EditGroupWindow : EditWindow<RaidGroup>
    {

        internal EditGroupWindow(EditWindowFactory factory, RaidGroup group, Action<RaidGroup>? onSave = null,
                                 Action? onCancel = null, Action? onDelete = null) :
            base(factory, group, onSave, onCancel, onDelete)
        {
            Size = new Vector2(500, 170 + (group.RolePriority != null ? 180 : 0));
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Save(RaidGroup destination)
        {
            destination.Name = DataCopy.Name;
            destination.Type = DataCopy.Type;
            destination.RolePriority = DataCopy.RolePriority;
            if (destination.LocalId.IsEmpty) Factory.DataManager.RaidGroupDb.TryAdd(destination);
            Factory.DataManager.Save();
        }
        protected override void Cancel() { }

        protected override void DrawContent()
        {
            //Name + Type
            ImGui.InputText(GeneralLoc.CommonTerms_Name, ref DataCopy.Name, 100);
            ImGui.BeginDisabled(DataCopy.TypeLocked);
            ImGuiHelper.Combo(GeneralLoc.EditGroupUi_in_type, ref DataCopy.Type);
            ImGui.EndDisabled();
            //Role priority
            bool overrideRolePriority = DataCopy.RolePriority != null;
            if (ImGui.Checkbox(GeneralLoc.EditGroupUi_cb_OverrideRolePriority, ref overrideRolePriority))
            {
                DataCopy.RolePriority = overrideRolePriority ? new RolePriority() : null;
                var curSize = Size!.Value;
                Resize(curSize with
                {
                    Y = curSize.Y + (overrideRolePriority ? 1 : -1) * 180f * ScaleFactor,
                });
            }

            if (overrideRolePriority)
            {
                ImGui.Text(LootmasterLoc.ConfigUi_hdg_RolePriority);
                ImGui.Text($"{LootmasterLoc.ConfigUi_txt_currentPrio}: {DataCopy.RolePriority}");
                DataCopy.RolePriority?.DrawEdit(ImGui.InputInt);
            }
        }
    }

    private class EditPlayerWindow : EditWindow<Player>
    {
        internal EditPlayerWindow(EditWindowFactory factory, Player p, Action<Player>? onSave = null,
                                  Action? onCancel = null,
                                  Action? onDelete = null)
            : base(factory, p, onSave, onCancel, onDelete)
        {
            Size = new Vector2(450, 250);
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            //Player Data
            ImGui.SetCursorPosX(
                (ImGui.GetWindowWidth() - ImGui.CalcTextSize(GeneralLoc.EditPlayerUi_hdg_playerData).X) / 2f);
            ImGui.Text(GeneralLoc.EditPlayerUi_hdg_playerData);
            ImGui.InputText(GeneralLoc.CommonTerms_Name, ref DataCopy.NickName, 50);
            //Character Data
            ImGui.SetCursorPosX(
                (ImGui.GetWindowWidth() - ImGui.CalcTextSize(GeneralLoc.EditPlayerUi_hdg_characterData).X) / 2f);
            ImGui.Text(GeneralLoc.EditPlayerUi_hdg_characterData);
            var mainChar = DataCopy.MainChar;
            foreach (var character in DataCopy.Characters)
            {
                ImGui.PushID(character.LocalId.ToString());
                if (ImGuiHelper.DeleteButton(character, "##delete"))
                    DataCopy.RemoveCharacter(character);
                ImGui.SameLine();
                if (ImGuiHelper.EditButton(character, "##edit"))
                    Factory.Create(character);

                ImGui.SameLine();
                bool isMain = mainChar.Equals(character);
                if (isMain) ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
                if (ImGuiHelper.Button(
                        isMain ? GeneralLoc.EditPlayerUi_btn_IsMain : GeneralLoc.EditPlayerUi_btn_MakeMain,
                        null))
                    DataCopy.MainChar = character;
                if (isMain) ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Text($"{character}");
                ImGui.PopID();
            }
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "addEmpty",
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, Character.DataTypeNameStatic)))
            {
                Factory.Create(new Character(), c =>
                {
                    if (Factory.DataManager.CharDb.TryAdd(c))
                        DataCopy.AddCharacter(c);
                });
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "addFromDb",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, Character.DataTypeNameStatic)))
            {
                AddChild(Factory.DataManager.CharDb.OpenSearchWindow(c => DataCopy.AddCharacter(c)));
            }
        }

        protected override void Save(Player destination)
        {
            if (destination.LocalId.IsEmpty)
            {
                Factory.DataManager.PlayerDb.TryAdd(destination);
            }
            //Player Data
            destination.NickName = DataCopy.NickName;
            //Characters
            var toRemove = destination.Characters.Where(c => !DataCopy.Characters.Contains(c));
            foreach (var c in toRemove)
            {
                destination.RemoveCharacter(c);
            }
            var toAdd = DataCopy.Characters.Where(c => !destination.Characters.Contains(c));
            foreach (var c in toAdd)
            {
                if (c.LocalId.IsEmpty)
                    if (!Factory.DataManager.CharDb.TryAdd(c))
                        continue;
                destination.AddCharacter(c);
            }
            destination.MainChar = DataCopy.MainChar;
            if (destination.MainChar.LocalId.IsEmpty) Factory.DataManager.CharDb.TryAdd(destination.MainChar);
        }
    }

    private class EditCharacterWindow : EditWindow<Character>
    {
        private const int CLASS_HEIGHT = 27 + 4;
        private Job _newJob = Job.ADV;
        internal EditCharacterWindow(EditWindowFactory factory, Character character, Action<Character>? onSave = null,
                                     Action? onCancel = null, Action? onDelete = null) : base(factory,
            character, onSave, onCancel, onDelete)
        {
            Size = new Vector2(500, 295 + CLASS_HEIGHT * DataCopy.Classes.Count());
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            ImGui.SetCursorPosX((ImGui.GetWindowWidth()
                               - ImGui.CalcTextSize(GeneralLoc.EditPlayerUi_hdg_characterData).X) / 2f);
            ImGui.Text(GeneralLoc.EditPlayerUi_hdg_characterData);
            if (ImGui.InputText(GeneralLoc.CommonTerms_Name, ref DataCopy.Name, 50)
             && Factory.Module.Services.CharacterInfoService.TryGetChar(out var pc, DataCopy.Name))
                DataCopy.HomeWorld ??= pc.HomeWorld.Value;
            if (ImGuiHelper.ExcelSheetCombo(GeneralLoc.EditCharUi_in_HomeWorld + "##" + Title, out World w,
                                            _ => DataCopy.HomeWorld?.Name.ExtractText() ?? "",
                                            x => x.Name.ExtractText(), x => x.IsPublic))
                DataCopy.HomeWorld = w;

            if (ImGuiHelper.ExcelSheetCombo(GeneralLoc.CommonTerms_Tribe + "##" + Title, out Tribe t,
                                            _ => GetGenderedTribeName(DataCopy.Tribe), GetGenderedTribeName))
                DataCopy.TribeId = t.RowId;
            //Class Data
            ImGui.SetCursorPosX((ImGui.GetWindowWidth()
                               - ImGui.CalcTextSize(GeneralLoc.EditCharUi_hdg_JobData).X) / 2f);
            ImGui.Text(GeneralLoc.EditCharUi_hdg_JobData);
            if (DataCopy.Classes.Any())
            {

                if (DataCopy.Classes.All(x => x.Job != DataCopy.MainJob))
                    DataCopy.MainJob = DataCopy.Classes.First().Job;
                if (ImGui.BeginCombo(GeneralLoc.EditCharUi_in_mainJob, DataCopy.MainJob.ToString()))
                    foreach (var curJob in DataCopy)
                    {
                        if (ImGui.Selectable(curJob.Job.ToString()))
                            DataCopy.MainJob = curJob.Job;
                    }
                ImGui.Separator();
                ImGui.TextColored(Colors.TextSoftRed, GeneralLoc.EditCharUi_text_BisNotice);
            }
            else
            {
                DataCopy.MainJob = null;
                ImGui.NewLine();
                ImGui.Text(GeneralLoc.EditCharUi_text_noJobs);
            }

            ImGui.Separator();
            Job? toDelete = null;
            foreach (var c in DataCopy.Classes)
            {
                ImGui.PushID(c.Job.ToString());

                if (ImGuiHelper.DeleteButton(c, "##delete"))
                    toDelete = c.Job;
                ImGui.SameLine();
                ImGui.Text($"{c.Job}  ");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                if (ImGui.InputInt(GeneralLoc.CommonTerms_Level, ref c.Level))
                    c.Level = Math.Clamp(c.Level, 1, Common.Services.ServiceManager.GameInfo.CurrentExpansion.MaxLevel);
                ImGui.SameLine();
                ImGui.Checkbox(GeneralLoc.EditCharUi_cb_hideJob, ref c.HideInUi);
                ImGui.Separator();
                ImGui.PopID();
            }
            if (toDelete is not null) DataCopy.RemoveClass(toDelete.Value);
            if (ImGuiHelper.SearchableCombo("##addJobCombo", out var job, _newJob.ToString(),
                                            Enum.GetValues<Job>(), j => j.ToString(),
                                            (j, s) => j.ToString()
                                                       .Contains(s, StringComparison.CurrentCultureIgnoreCase),
                                            j => DataCopy.Classes.All(y => y.Job != j)))
                _newJob = job;
            ImGui.SameLine();
            if (ImGuiHelper.AddButton(PlayableClass.DataTypeNameStatic, "##addJobBtn"))
            {
                var newClass = DataCopy[_newJob];
                if (newClass == null)
                {
                    newClass = DataCopy.AddClass(_newJob);
                    Factory.DataManager.GearDb.TryAdd(newClass.CurGear);
                    Factory.DataManager.GearDb.TryAdd(newClass.CurBis);
                }

                newClass.CurBis.ManagedBy = GearSetManager.Etro;
                newClass.CurBis.ExternalId = Factory.Module.Services.ConnectorPool.EtroConnector.GetDefaultBiS(_newJob);
            }
            return;

            //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
            string GetGenderedTribeName(Tribe tribe)
            {
                return DataCopy.Gender == Gender.Male ? tribe.Masculine.ExtractText() : tribe.Feminine.ExtractText();
            }
        }

        protected override void Save(Character destination)
        {
            //Copy safe data
            if (destination.Name != DataCopy.Name ||
                destination.HomeWorldId != DataCopy.HomeWorldId)
            {
                destination.Name = DataCopy.Name;
                destination.HomeWorldId = DataCopy.HomeWorldId;
            }

            destination.TribeId = DataCopy.TribeId;
            destination.MainJob = DataCopy.MainJob;
            //Remove classes that were removed in Ui
            for (int i = 0; i < destination.Classes.Count(); i++)
            {
                var c = destination.Classes.ElementAt(i);
                if (DataCopy.Classes.Any(x => x.Job == c.Job)) continue;
                destination.RemoveClass(c.Job);
                i--;
            }

            //Add missing classes and update Bis/Level for existing ones
            foreach (var c in DataCopy.Classes)
            {
                var target = destination[c.Job];
                var gearSetDb = Factory.DataManager.GearDb;

                if (target == null)
                {
                    target = destination.AddClass(c.Job);
                    gearSetDb.TryAdd(target.CurGear);
                    gearSetDb.TryAdd(target.CurBis);
                }

                target.Level = c.Level;
                target.HideInUi = c.HideInUi;
            }
            if (destination.LocalId.IsEmpty) Factory.DataManager.CharDb.TryAdd(destination);
            Factory.DataManager.Save();
        }
    }

    private class EditGearSetWindow : EditWindow<GearSet>
    {
        private readonly Job? _providedJob;
        private Job _job;
        private string _externalIdInput = "";
        private GearSetManager _curSetManager;
        private List<string> _xivGearSetList = [];
        private string _loadedExternalId = "";
        private bool _backgroundTaskBusy = false;
        private string _etroName = "";
        internal EditGearSetWindow(EditWindowFactory factory, GearSet original, Action<GearSet>? onSave = null,
                                   Action? onCancel = null, Action? onDelete = null, Job? job = null) :
            base(factory, original, onSave, onCancel, onDelete)
        {
            CanDelete = true;
            _providedJob = job;
            _job = job ?? original[GearSetSlot.MainHand].Jobs.FirstOrDefault(Job.ADV);
            _curSetManager = original.ManagedBy;
            MinSize = new Vector2(700, 570);
            Size = new Vector2(1100, 570);
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            ImGui.Columns(2, "##Naming", false);

            if (ImGui.BeginTable("##NameTable", 2))
            {
                ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn("##Input", ImGuiTableColumnFlags.WidthStretch, 5);
                ImGui.TableNextColumn();
                ImGui.BeginDisabled(DataCopy.IsManagedExternally);
                ImGui.Text(GeneralLoc.CommonTerms_Name);
                ImGui.TableNextColumn();
                ImGui.InputText("##name", ref DataCopy.Name, 100);
                ImGui.EndDisabled();
                ImGui.TableNextColumn();
                ImGui.Text("Local alias");
                ImGui.TableNextColumn();
                string alias = DataCopy.Alias ?? "";
                if (ImGui.InputText("##alias", ref alias, 100))
                    DataCopy.Alias = alias.IsNullOrWhitespace() ? null : alias;

                ImGui.TableNextColumn();
                ImGui.Text(GeneralLoc.EditGearSetUi_txt_LastChange);
                ImGui.TableNextColumn();
                ImGui.Text($"{DataCopy.TimeStamp}");
                ImGui.EndTable();
            }
            if (DataCopy is { IsSystemManaged: true, IsManagedExternally: false })
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Colors.TextRed);
                ImGui.TextWrapped(GeneralLoc.Gearset_Warning_SysManaged);
                ImGui.PopStyleColor();
            }


            if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_MakeLocal,
                                   GeneralLoc.EditGearSetUi_btn_tt_MakeLocal))
            {
                var newSet = DataCopy.Clone();
                newSet.LocalId = HrtId.Empty;
                newSet.RemoteIDs = [];
                newSet.ManagedBy = GearSetManager.Hrt;
                newSet.ExternalId = string.Empty;
                Factory.DataManager.GearDb.TryAdd(newSet);
                ReplaceOriginal(newSet);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_openEtro, null))
                Util.OpenLink(EtroConnector.GEARSET_WEB_BASE_URL + DataCopy.ExternalId);
            if (ImGui.BeginTable("##sourceTable", 2))
            {
                ImGui.TableSetupColumn("##Name", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("##Name2", ImGuiTableColumnFlags.WidthStretch, 4);

                ImGui.TableNextColumn();
                ImGui.Text(GeneralLoc.EditGearSetUi_txt_LastChange);
                ImGui.TableNextColumn();
                ImGui.Text($"{DataCopy.TimeStamp}");
                //Source information
                ImGui.TableNextColumn();
                ImGui.Text(GeneralLoc.EditGearSetUi_txt_Source);
                ImGui.TableNextColumn();
                ImGui.Text(DataCopy.ManagedBy.FriendlyName());
                if (DataCopy.IsManagedExternally)
                {

                    ImGui.TableNextColumn();
                    ImGui.Text(GeneralLoc.CommonTerms_ExternalID);
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(DataCopy.ExternalId);
                    ImGui.TableNextColumn();
                    ImGui.Text(GeneralLoc.EditGearSetUi_txt_lastUpdate);
                    ImGui.TableNextColumn();
                    ImGui.Text($"{DataCopy.LastExternalFetchDate}");
                }
                ImGui.TableNextColumn();
                ImGui.TextWrapped(GeneralLoc.EditGearSetUi_txt_job);
                ImGuiHelper.AddTooltip("Restricts the item to select");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(60 * ScaleFactor);
                ImGui.BeginDisabled(_providedJob is not null);
                ImGuiHelper.Combo("##JobSelection", ref _job);
                ImGui.EndDisabled();
                ImGui.TableNextColumn();
                ImGui.EndTable();
            }
            //


            DrawExternalSection();
            ImGui.NextColumn();
            ImGui.BeginDisabled(DataCopy.IsManagedExternally);
            //Gear slots
            if (ImGui.BeginTable("##GearEditTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("##GearL");
                ImGui.TableSetupColumn("##GearR");
                DrawSlot(GearSetSlot.MainHand);
                if (_job.CanHaveShield())
                    DrawSlot(GearSetSlot.OffHand);
                else
                    ImGui.TableNextColumn();
                DrawSlot(GearSetSlot.Head);
                DrawSlot(GearSetSlot.Ear);
                DrawSlot(GearSetSlot.Body);
                DrawSlot(GearSetSlot.Neck);
                DrawSlot(GearSetSlot.Hands);
                DrawSlot(GearSetSlot.Wrist);
                DrawSlot(GearSetSlot.Legs);
                DrawSlot(GearSetSlot.Ring1);
                DrawSlot(GearSetSlot.Feet);
                DrawSlot(GearSetSlot.Ring2);
                ImGui.EndTable();
            }
            ImGui.EndDisabled();
            ImGui.Columns();
            return;
            void DrawSlot(GearSetSlot slot)
            {
                ImGui.BeginDisabled(ChildIsOpen);
                ImGui.TableNextColumn();
                UiHelpers.DrawGearEdit(this, slot, DataCopy[slot], i =>
                {
                    foreach (var mat in DataCopy[slot].Materia)
                    {
                        i.AddMateria(mat);
                    }
                    DataCopy[slot] = i;
                }, _job);
                ImGui.EndDisabled();
            }
        }

        public void DrawExternalSection()
        {
            //Etro curated BiS
            var etroBisSets = Factory.Module.Services.ConnectorPool.EtroConnector.GetBiS(_job);
            if (etroBisSets.Any())
            {
                ImGui.Text(GeneralLoc.EditGearSetUi_text_getEtroBis);
                foreach ((string etroId, string name) in etroBisSets)
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button($"{name}##BIS#{etroId}", $"{etroId}"))
                    {
                        if (Factory.DataManager.GearDb.Search(gearSet => gearSet?.ExternalId == etroId,
                                                              out var newSet))
                        {
                            ReplaceOriginal(newSet);
                            return;
                        }
                        ReplaceOriginal(new GearSet(GearSetManager.Etro, name)
                        {
                            ExternalId = etroId,
                        });
                        Factory.Module.Services.ConnectorPool.EtroConnector.RequestGearSetUpdate(DataCopy);
                        return;
                    }
                }
            }
            ImGui.Text("Replace with: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100 * ScaleFactor);
            ImGuiHelper.Combo("##manager", ref _curSetManager, null, val => val != GearSetManager.Hrt);
            ImGui.SetNextItemWidth(200 * ScaleFactor);
            ImGui.SameLine();
            if (ImGui.InputText("ID/Url", ref _externalIdInput, 255))
            {
                if (_externalIdInput.StartsWith(EtroConnector.GEARSET_WEB_BASE_URL))
                {
                    _curSetManager = GearSetManager.Etro;
                    _externalIdInput = Factory.Module.Services.ConnectorPool.EtroConnector.GetId(_externalIdInput);
                }
                if (Factory.Module.Services.ConnectorPool.XivGearAppConnector.BelongsToThisService(_externalIdInput))
                {
                    _curSetManager = GearSetManager.XivGear;
                    _externalIdInput =
                        Factory.Module.Services.ConnectorPool.XivGearAppConnector.GetId(_externalIdInput);
                }
            }
            if (!_backgroundTaskBusy && _externalIdInput != _loadedExternalId)
            {
                switch (_curSetManager)
                {
                    case GearSetManager.XivGear:
                        _backgroundTaskBusy = true;
                        _xivGearSetList = [];
                        DataCopy.ExternalIdx = 0;
                        _loadedExternalId = _externalIdInput;
                        Factory.Module.Services.TaskManager.RegisterTask(new HrtTask<List<string>>(
                                                                             () => Factory.Module.Services.ConnectorPool
                                                                                 .XivGearAppConnector
                                                                                 .GetSetNames(_externalIdInput),
                                                                             list =>
                                                                             {
                                                                                 _xivGearSetList = list;
                                                                                 _backgroundTaskBusy = false;
                                                                             }, "GetSetNames"
                                                                         ));
                        break;
                    case GearSetManager.Etro:
                        _backgroundTaskBusy = true;
                        DataCopy.ExternalIdx = 0;
                        _etroName = string.Empty;
                        _loadedExternalId = _externalIdInput;
                        Factory.Module.Services.TaskManager.RegisterTask(new HrtTask<string>(
                                                                             () => Factory.Module.Services.ConnectorPool
                                                                                 .EtroConnector
                                                                                 .GetName(_externalIdInput),
                                                                             name =>
                                                                             {
                                                                                 _etroName = name;
                                                                                 _backgroundTaskBusy = false;
                                                                             }, "GetSetName"
                                                                         ));
                        break;
                    case GearSetManager.Hrt:
                    default:
                        break;
                }
            }
            if (_backgroundTaskBusy)
            {
                ImGui.Text("Loading... ");
                ImGui.SameLine();
            }
            else
                switch (_curSetManager)
                {
                    case GearSetManager.XivGear when _xivGearSetList.Count > 0:
                    {
                        ImGui.SetNextItemWidth(200 * ScaleFactor);
                        int idx = 0;
                        if (ImGui.BeginCombo("##idx", _xivGearSetList[DataCopy.ExternalIdx]))
                        {
                            foreach (string name in _xivGearSetList)
                            {
                                if (ImGui.Selectable(name))
                                    DataCopy.ExternalIdx = idx;
                                idx++;
                            }

                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        break;
                    }
                    case GearSetManager.Etro:
                        ImGui.Text(_etroName);
                        ImGui.SameLine();
                        break;
                    case GearSetManager.Hrt:
                    default:
                        ImGui.Text("No set");
                        ImGui.SameLine();
                        break;
                }
            if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_GetExternal,
                                   GeneralLoc.EditGearSetUi_btn_tt_GetExternal, !_backgroundTaskBusy))
            {
                if (Factory.DataManager.GearDb.Search(
                        gearSet => gearSet?.ManagedBy == _curSetManager && gearSet.ExternalId == _externalIdInput,
                        out var newSet))
                {
                    ReplaceOriginal(newSet);
                    return;
                }
                ReplaceOriginal(new GearSet(_curSetManager)
                {
                    ExternalId = _externalIdInput,
                    ExternalIdx = DataCopy.ExternalIdx,
                });
                if (DataCopy.ManagedBy == GearSetManager.Etro)
                {
                    Factory.Module.Services.ConnectorPool.EtroConnector.RequestGearSetUpdate(DataCopy);
                }
                else if (DataCopy.ManagedBy == GearSetManager.XivGear)
                {
                    Factory.Module.Services.ConnectorPool.XivGearAppConnector.RequestGearSetUpdate(DataCopy);
                }
            }
        }

        protected override void Save(GearSet destination)
        {
            if (destination.LocalId.IsEmpty) Factory.DataManager.GearDb.TryAdd(destination);
            DataCopy.TimeStamp = DateTime.Now;
            destination.CopyFrom(DataCopy);
            Factory.DataManager.Save();
        }
    }
}