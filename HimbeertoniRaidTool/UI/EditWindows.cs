﻿using System.Collections.Concurrent;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace HimbeertoniRaidTool.Plugin.UI;

public class EditWindowFactory(IGlobalServiceContainer services)
{
    private HrtDataManager DataManager => services.HrtDataManager;
    private ConnectorPool ConnectorPool => services.ConnectorPool;
    private CharacterInfoService CharacterInfoService => services.CharacterInfoService;
    private TaskManager TaskManager => services.TaskManager;
    private IUiSystem UiSystem => services.UiSystem;
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
        services.UiSystem.AddWindow(window);
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
                             Action? onDelete) : base(factory.UiSystem)
        {
            Factory = factory;
            _original = original;
            DataCopy = _original.Clone();
            _onCancel = onCancel;
            _onSave = onSave;
            _onDelete = onDelete;
            Title = string.Format(GeneralLoc.EditUi_Title, _original.DataTypeName, _original.Name)
                          .Capitalized();
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
                AddChild(Factory.DataManager.CharDb.GetSearchWindow(UiSystem, c => DataCopy.AddCharacter(c)));
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
             && Factory.CharacterInfoService.TryGetChar(out var pc, DataCopy.Name))
                DataCopy.HomeWorld ??= pc.HomeWorld.Value;
            if (UiSystem.Helpers.ExcelSheetCombo(GeneralLoc.EditCharUi_in_HomeWorld + "##" + Title, out World w,
                                                 _ => DataCopy.HomeWorld?.Name.ExtractText() ?? "",
                                                 x => x.Name.ExtractText(), x => x.IsPublic))
                DataCopy.HomeWorld = w;

            if (UiSystem.Helpers.ExcelSheetCombo(GeneralLoc.CommonTerms_Tribe + "##" + Title, out Tribe t,
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
                    c.Level = Math.Clamp(c.Level, 1, GameInfo.CurrentExpansion.MaxLevel);
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
                var newBis = Factory.ConnectorPool.GetDefaultBiS(_newJob);
                newClass.CurBis.ManagedBy = newBis.Service;
                newClass.CurBis.ExternalId = newBis.Id;
                newClass.CurBis.ExternalIdx = newBis.Idx;
                newClass.CurBis.Name = newBis.Name;
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
        private string _curSetId = "";
        private GearSetManager? _selectedService;
        private GearSetManager? _detectedService;
        private GearSetManager? CurService => _selectedService ?? _detectedService;
        private readonly ConcurrentDictionary<(GearSetManager? service, string id), IList<ExternalBiSDefinition>>
            _externalGearSetCache = [];
        private readonly ConcurrentDictionary<(GearSetManager? service, string id), bool> _currentlyLoading = [];
        internal EditGearSetWindow(EditWindowFactory factory, GearSet original, Action<GearSet>? onSave = null,
                                   Action? onCancel = null, Action? onDelete = null, Job? job = null) :
            base(factory, original, onSave, onCancel, onDelete)
        {
            CanDelete = true;
            _providedJob = job;
            _job = job ?? original[GearSetSlot.MainHand].Jobs.FirstOrDefault(Job.ADV);
            MinSize = new Vector2(700, 600);
            Size = new Vector2(1100, 600);
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Cancel() { }

        private void LoadSet()
        {
            if (_curSetId.Length == 0) return;
            if (_externalGearSetCache.ContainsKey((CurService, _curSetId))) return;
            if (_currentlyLoading.ContainsKey((CurService, _curSetId))) return;
            foreach (var serviceType in Enum.GetValues<GearSetManager>())
            {
                if (!Factory.ConnectorPool.TryGetConnector(serviceType, out var connector)) continue;
                if (!connector.BelongsToThisService(_curSetId) && CurService == null) continue;
                if (CurService.HasValue && CurService.Value != serviceType) continue;
                _detectedService = serviceType;
                _selectedService = null;
                _curSetId = connector.GetId(_curSetId);
                if (_externalGearSetCache.ContainsKey((CurService, _curSetId))) return;
                if (_currentlyLoading.ContainsKey((CurService, _curSetId))) return;
                if (_currentlyLoading.TryAdd((CurService, _curSetId), true))
                    Factory.TaskManager.RegisterTask(new HrtTask<IList<ExternalBiSDefinition>>(
                                                         () => connector.GetPossibilities(_curSetId),
                                                         list =>
                                                         {
                                                             _externalGearSetCache.TryAdd(
                                                                 (CurService, _curSetId), list);
                                                             _currentlyLoading.Remove(
                                                                 (CurService, _curSetId), out _);
                                                         }, "GetSetNames"
                                                     ));
                return;
            }
        }

        protected override void DrawContent()
        {
            ImGui.Columns(2, "##Naming", false);
            var leftTable = ImRaii.Table("##leftTable", 1, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg);
            if (leftTable)
            {
                ImGui.TableNextColumn();
                DrawGeneralSection();
                if (DataCopy.IsManagedExternally)
                {
                    ImGui.TableNextColumn();
                    DrawExternalSection();
                }
                ImGui.TableNextColumn();
                DrawReplaceSection();
            }
            leftTable.Dispose();
            ImGui.NextColumn();
            if (DataCopy.IsManagedExternally || DataCopy.IsSystemManaged)
            {
                string text = DataCopy.IsManagedExternally ? GeneralLoc.Gearset_Warning_ExternalManaged
                    : GeneralLoc.Gearset_Warning_SysManaged;
                using (ImRaii.PushColor(ImGuiCol.Text, Colors.TextRed))
                {
                    ImGui.TextWrapped(text);
                }
            }
            ImGui.BeginDisabled(DataCopy.IsManagedExternally);
            DrawGearEditSection();
            ImGui.EndDisabled();
            ImGui.Columns();
        }

        private void DrawGeneralSection()
        {
            using var table = ImRaii.Table("##GeneralTable", 2);
            if (!table) return;
            ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthStretch, 2);
            ImGui.TableSetupColumn("##Input", ImGuiTableColumnFlags.WidthStretch, 6);
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
            ImGui.TableNextColumn();
            ImGui.Text(GeneralLoc.EditGearSetUi_txt_job);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(70 * ScaleFactor);
            ImGui.BeginDisabled(_providedJob is not null);
            ImGuiHelper.Combo("##JobSelection", ref _job);
            ImGui.EndDisabled();
            ImGui.TableNextColumn();
            ImGui.Text(GeneralLoc.EditGearSetUi_txt_Source);
            ImGui.TableNextColumn();
            ImGui.Text(DataCopy.ManagedBy.FriendlyName());
        }

        private void DrawExternalSection()
        {
            if (!DataCopy.IsManagedExternally) return;
            ImGui.Text($"{DataCopy.ManagedBy.FriendlyName()} Section");
            using (var table = ImRaii.Table("##NameTable", 2))
            {
                if (table)
                {
                    ImGui.TableSetupColumn("##Name", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn("##Name2", ImGuiTableColumnFlags.WidthStretch, 4);
                    ImGui.TableNextColumn();
                    ImGui.Text("Service");
                    ImGui.TableNextColumn();
                    ImGui.Text(DataCopy.ManagedBy.FriendlyName());
                    ImGui.TableNextColumn();
                    ImGui.Text(GeneralLoc.CommonTerms_ExternalID);
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(DataCopy.ExternalId);
                    ImGui.TableNextColumn();
                    ImGui.Text(GeneralLoc.EditGearSetUi_txt_lastUpdate);
                    ImGui.TableNextColumn();
                    ImGui.Text($"{DataCopy.LastExternalFetchDate}");
                }
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
            if (Factory.ConnectorPool.TryGetConnector(DataCopy.ManagedBy, out var connector))
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button(
                        string.Format(GeneralLoc.EditGearSetUi_btn_OpenWeb, DataCopy.ManagedBy.FriendlyName()),
                        GeneralLoc.EditGearSetUi_btn_tt_OpenWeb))
                    Util.OpenLink(connector.GetWebUrl(DataCopy.ExternalId));
            }
        }

        private void DrawGearEditSection()
        {
            //Food
            ImGui.Text(GeneralLoc.CommonTerms_Food);
            ImGui.SameLine();
            UiSystem.Helpers.DrawFoodEdit(this, DataCopy.Food, i => DataCopy.Food = i);
            //Gear table
            using var table = ImRaii.Table("##GearEditTable", 2, ImGuiTableFlags.Borders);
            if (!table) return;
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
            return;
            void DrawSlot(GearSetSlot slot)
            {

                ImGui.BeginDisabled(ChildIsOpen);
                ImGui.TableNextColumn();
                UiSystem.Helpers.DrawGearEdit(this, slot, DataCopy[slot], i =>
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

        private void DrawReplaceSection()
        {
            foreach (var service in Enum.GetValues<GearSetManager>())
            {
                if (!Factory.ConnectorPool.TryGetConnector(service, out var connector)) continue;
                //curated BiS
                var bisSets = connector.GetBiSList(_job);
                if (!bisSets.Any()) continue;
                ImGui.Text(string.Format(GeneralLoc.EditGearSetUi_text_getCuratedBis, service.FriendlyName()));

                foreach (var biSDefinition in bisSets)
                {
                    ImGui.SameLine();
                    DrawReplaceButton(biSDefinition);
                }
            }

            ImGui.Text(GeneralLoc.EditGearSetUi_hdg_ExtCustom);
            ImGui.SetNextItemWidth(100 * ScaleFactor);
            ImGuiHelper.Combo("##manager", ref _selectedService,
                              val => val.HasValue ? val.Value.FriendlyName() : GeneralLoc.EditGearSetUi_txt_AutoDetect,
                              val => Factory.ConnectorPool.HasConnector(val));
            ImGui.SetNextItemWidth(200 * ScaleFactor);
            ImGui.SameLine();
            ImGui.InputText(GeneralLoc.EditGearSetUi_input_ExtId, ref _curSetId, 255);
            LoadSet();
            if (_currentlyLoading.ContainsKey((CurService, _curSetId)))
            {
                ImGui.Text(GeneralLoc.EditGearSetUi_txt_Loading);
            }
            else
            {
                if (_externalGearSetCache.TryGetValue((CurService, _curSetId), out var bisList) && bisList.Any())
                {
                    ImGui.Text(GeneralLoc.EditGearSetUi_text_Replace);
                    foreach (var biSDefinition in bisList)
                    {
                        ImGui.SameLine();
                        DrawReplaceButton(biSDefinition);
                    }
                }
                else
                {
                    ImGui.Text(GeneralLoc.EditGearSetUi_text_InvalidId);
                }

            }
            return;
            void DrawReplaceButton(ExternalBiSDefinition biSDefinition)
            {
                if (!ImGuiHelper.Button($"{biSDefinition.Name}##BIS#{biSDefinition.Id}#{biSDefinition.Idx}",
                                        $"{biSDefinition.Id} ({biSDefinition.Idx}) <{biSDefinition.Service.FriendlyName()}>"))
                    return;
                if (Factory.DataManager.GearDb.Search(biSDefinition.Equals, out var newSet))
                {
                    ReplaceOriginal(newSet);
                    return;
                }
                ReplaceOriginal(new GearSet(biSDefinition.Service)
                {
                    ExternalId = biSDefinition.Id,
                    ExternalIdx = biSDefinition.Idx,
                });
                if (Factory.ConnectorPool.TryGetConnector(biSDefinition.Service, out var connector))
                    connector.RequestGearSetUpdate(DataCopy);
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