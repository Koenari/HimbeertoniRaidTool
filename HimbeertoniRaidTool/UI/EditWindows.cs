using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class EditWindowFactory
{
    private static HrtDataManager DataManager => ServiceManager.HrtDataManager;
    public static HrtWindow? Create<TData>(HrtId id, Action<TData>? onSave = null,
                                           Action? onCancel = null,
                                           object? param = null)
        where TData : IHasHrtId, new()
    {
        HrtId.IdType type = new TData().IdType;
        if (id.Type != type) return null;
        switch (type)
        {
            case HrtId.IdType.Group:
                if (DataManager.RaidGroupDb.TryGet(id, out RaidGroup? group))
                    return Create(group, onSave as Action<RaidGroup>, onCancel, param);
                break;
            case HrtId.IdType.Player:
                if (DataManager.PlayerDb.TryGet(id, out Player? player))
                    return Create(player, onSave as Action<Player>, onCancel, param);
                break;
            case HrtId.IdType.Character:
                if (DataManager.CharDb.TryGet(id, out Character? character))
                    return Create(character, onSave as Action<Character>, onCancel, param);
                break;
            case HrtId.IdType.Gear:
                if (DataManager.GearDb.TryGet(id, out GearSet? gearSet))
                    return Create(gearSet, onSave as Action<GearSet>, onCancel, param);
                break;
            case HrtId.IdType.None:
            default:
                return null;
        }
        return null;
    }
    public static HrtWindow? Create<TData>(TData data, Action<TData>? onSave = null,
                                           Action? onCancel = null,
                                           object? param = null)
        where TData : IHasHrtId => data.IdType switch
    {
        HrtId.IdType.Player when data is Player p => new EditPlayerWindow(p, onSave as Action<Player>, onCancel),
        HrtId.IdType.Character when data is Character c => new EditCharacterWindow(
            c, onSave as Action<Character>, onCancel),
        HrtId.IdType.Gear when data is GearSet gs => new EditGearSetWindow(
            gs, onSave as Action<GearSet>, onCancel, param as Job?),
        HrtId.IdType.Group when data is RaidGroup rg => new EditGroupWindow(rg, onSave as Action<RaidGroup>, onCancel),
        HrtId.IdType.None                            => null,
        _                                            => null,
    };

    private abstract class EditWindow<TData> : HrtWindowWithModalChild where TData : IHrtDataTypeWithId
    {
        private readonly Action? _onCancel;

        private readonly Action<TData>? _onSave;

        private TData _original;
        protected TData DataCopy;

        protected EditWindow(TData original, Action<TData>? onSave, Action? onCancel)
        {
            _original = original;
            DataCopy = _original.Clone();
            _onCancel = onCancel;
            _onSave = onSave;
            Title = string.Format(GeneralLoc.EditUi_Title, _original.DataTypeName, _original.Name)
                          .CapitalizedSentence();
        }
        public override sealed void Draw()
        {
            ImGui.Text($"{GeneralLoc.EditUi_Txt_LocalId}: {(_original.LocalId.IsEmpty ? "-" : _original.LocalId)}");
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
            DrawContent();
        }
        protected abstract void DrawContent();
        protected void Save() => Save(_original);

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

        internal EditGroupWindow(RaidGroup group, Action<RaidGroup>? onSave = null,
                                 Action? onCancel = null) :
            base(group, onSave, onCancel)
        {
            Size = new Vector2(500, 170 + (group.RolePriority != null ? 180 : 0));
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void Save(RaidGroup destination)
        {
            destination.Name = DataCopy.Name;
            destination.Type = DataCopy.Type;
            destination.RolePriority = DataCopy.RolePriority;
            if (destination.LocalId.IsEmpty) ServiceManager.HrtDataManager.RaidGroupDb.TryAdd(destination);
            ServiceManager.HrtDataManager.Save();
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
                Vector2 curSize = Size!.Value;
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
        internal EditPlayerWindow(Player p, Action<Player>? onSave = null, Action? onCancel = null)
            : base(p, onSave, onCancel)
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
            Character mainChar = DataCopy.MainChar;
            foreach (Character character in DataCopy.Characters)
            {
                ImGui.PushID(character.LocalId.ToString());
                if (ImGuiHelper.DeleteButton(character, "##delete"))
                    DataCopy.RemoveCharacter(character);
                ImGui.SameLine();
                if (ImGuiHelper.EditButton(character, "##edit"))
                    AddChild(Create<Character>(character.LocalId));

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
                AddChild(Create(new Character(), c =>
                {
                    if (ServiceManager.HrtDataManager.CharDb.TryAdd(c))
                        DataCopy.AddCharacter(c);
                }));
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "addFromDb",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, Character.DataTypeNameStatic)))
            {
                AddChild(ServiceManager.HrtDataManager.CharDb.OpenSearchWindow(c => DataCopy.AddCharacter(c)));
            }
        }

        protected override void Save(Player destination)
        {
            if (destination.LocalId.IsEmpty)
            {
                ServiceManager.HrtDataManager.PlayerDb.TryAdd(destination);
            }
            //Player Data
            destination.NickName = DataCopy.NickName;
            //Characters
            var toRemove = destination.Characters.Where(c => !DataCopy.Characters.Contains(c));
            foreach (Character c in toRemove)
            {
                destination.RemoveCharacter(c);
            }
            var toAdd = DataCopy.Characters.Where(c => !destination.Characters.Contains(c));
            foreach (Character c in toAdd)
            {
                if (c.LocalId.IsEmpty)
                    if (!ServiceManager.HrtDataManager.CharDb.TryAdd(c))
                        continue;
                destination.AddCharacter(c);
            }
            destination.MainChar = DataCopy.MainChar;
            if (destination.MainChar.LocalId.IsEmpty) ServiceManager.HrtDataManager.CharDb.TryAdd(destination.MainChar);
        }
    }

    private class EditCharacterWindow : EditWindow<Character>
    {
        private const int CLASS_HEIGHT = 27 + 4;
        private Job _newJob = Job.ADV;
        internal EditCharacterWindow(Character character, Action<Character>? onSave = null,
                                     Action? onCancel = null) : base(character, onSave, onCancel)
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
             && ServiceManager.CharacterInfoService.TryGetChar(out IPlayerCharacter? pc, DataCopy.Name))
                DataCopy.HomeWorld ??= pc.HomeWorld.GameData;
            if (ImGuiHelper.ExcelSheetCombo(GeneralLoc.EditCharUi_in_HomeWorld + "##" + Title, out World? w,
                                            _ => DataCopy.HomeWorld?.Name.RawString ?? "",
                                            x => x.Name.RawString, x => x.IsPublic))
                DataCopy.HomeWorld = w;

            //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
            string GetGenderedTribeName(Tribe? tribe)
            {
                return (DataCopy.Gender == Gender.Male ? tribe?.Masculine.RawString : tribe?.Feminine.RawString) ??
                       string.Empty;
            }

            if (ImGuiHelper.ExcelSheetCombo(GeneralLoc.CommonTerms_Tribe + "##" + Title, out Tribe? t,
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
                    foreach (PlayableClass curJob in DataCopy)
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
            foreach (PlayableClass c in DataCopy.Classes)
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
            if (ImGuiHelper.SearchableCombo("##addJobCombo", out Job job, _newJob.ToString(),
                                            Enum.GetValues<Job>(), j => j.ToString(),
                                            (j, s) => j.ToString()
                                                       .Contains(s, StringComparison.CurrentCultureIgnoreCase),
                                            j => DataCopy.Classes.All(y => y.Job != j)))
                _newJob = job;
            ImGui.SameLine();
            if (ImGuiHelper.AddButton(PlayableClass.DataTypeNameStatic, "##addJobBtn"))
            {
                PlayableClass? newClass = DataCopy[_newJob];
                if (newClass == null)
                {
                    newClass = DataCopy.AddClass(_newJob);
                    ServiceManager.HrtDataManager.GearDb.TryAdd(newClass.CurGear);
                    ServiceManager.HrtDataManager.GearDb.TryAdd(newClass.CurBis);
                }

                newClass.CurBis.ManagedBy = GearSetManager.Etro;
                newClass.CurBis.EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(_newJob);
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
                PlayableClass c = destination.Classes.ElementAt(i);
                if (DataCopy.Classes.Any(x => x.Job == c.Job)) continue;
                destination.RemoveClass(c.Job);
                i--;
            }

            //Add missing classes and update Bis/Level for existing ones
            foreach (PlayableClass c in DataCopy.Classes)
            {
                PlayableClass? target = destination[c.Job];
                var gearSetDb = ServiceManager.HrtDataManager.GearDb;

                if (target == null)
                {
                    target = destination.AddClass(c.Job);
                    gearSetDb.TryAdd(target.CurGear);
                    gearSetDb.TryAdd(target.CurBis);
                }

                target.Level = c.Level;
                target.HideInUi = c.HideInUi;
            }
            if (destination.LocalId.IsEmpty) ServiceManager.HrtDataManager.CharDb.TryAdd(destination);
            ServiceManager.HrtDataManager.Save();
        }
    }

    private class EditGearSetWindow : EditWindow<GearSet>
    {
        private readonly Job? _providedJob;
        private Job _job;
        private string _etroIdInput = "";
        internal EditGearSetWindow(GearSet original, Action<GearSet>? onSave = null,
                                   Action? onCancel = null, Job? job = null) :
            base(original, onSave, onCancel)
        {
            _providedJob = job;
            _job = job ?? original[GearSetSlot.MainHand].Jobs.FirstOrDefault(Job.ADV);
            MinSize = new Vector2(550, 300);
        }

        protected override void Cancel() { }

        protected override void DrawContent()
        {
            bool isFromEtro = DataCopy.ManagedBy == GearSetManager.Etro;
            ImGui.SameLine();
            if (ImGuiHelper.DeleteButton(DataCopy, "##delete", true, new Vector2(50, 25)))
            {
                GearSet newSet = new();
                if (ServiceManager.HrtDataManager.GearDb.TryAdd(newSet))
                {
                    DataCopy = newSet;
                    Save();
                    Hide();
                }
            }
            //Replace with etro section
            ImGui.Text(GeneralLoc.EditGearSetUi_text_getEtro);
            foreach ((string etroId, string name) in ServiceManager.ConnectorPool.EtroConnector.GetBiS(_job))
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button($"{name}##BIS#{etroId}", $"{etroId}"))
                {
                    if (ServiceManager.HrtDataManager.GearDb.Search(gearSet => gearSet?.EtroId == etroId,
                                                                    out GearSet? newSet))
                    {
                        ReplaceOriginal(newSet);
                        return;
                    }
                    ReplaceOriginal(new GearSet(GearSetManager.Etro, name)
                    {
                        EtroId = etroId,
                    });
                    ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(DataCopy);
                    return;
                }
            }
            const string customEtroPopupId = "customEtroPopupID";
            ImGui.SameLine();
            if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_CustomEtro,
                                   GeneralLoc.EditGearSetUi_btn_tt_CustomEtro))
            {
                _etroIdInput = "";
                ImGui.OpenPopup(customEtroPopupId);
            }

            if (ImGui.BeginPopupContextItem(customEtroPopupId))
            {
                ImGui.InputText("CustomEtroID", ref _etroIdInput, 200);
                if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_tt_cutsomEtro,
                                       GeneralLoc.EditGearSetUi_btn_tt_cutsomEtro_Get))
                {
                    if (_etroIdInput.StartsWith(EtroConnector.GEARSET_WEB_BASE_URL))
                        _etroIdInput = _etroIdInput[EtroConnector.GEARSET_WEB_BASE_URL.Length..];
                    if (ServiceManager.HrtDataManager.GearDb.Search(gearSet => gearSet?.EtroId == _etroIdInput,
                                                                    out GearSet? newSet))
                    {
                        ReplaceOriginal(newSet);
                        return;
                    }
                    ReplaceOriginal(new GearSet(GearSetManager.Etro)
                    {
                        EtroId = _etroIdInput,
                    });
                    ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(DataCopy);
                    ImGui.CloseCurrentPopup();
                    return;
                }
                ImGui.EndPopup();
            }
            //Source information
            if (isFromEtro)
            {
                ImGui.Text($"{GeneralLoc.EditGearSetUi_Source}: {GearSetManager.Etro.FriendlyName()}");
                ImGui.SameLine();
                if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_MakeLocal,
                                       GeneralLoc.EditGearSetUi_btn_tt_MakeLocal))
                {
                    GearSet newSet = DataCopy.Clone();
                    newSet.LocalId = HrtId.Empty;
                    newSet.RemoteIDs = new List<HrtId>();
                    newSet.ManagedBy = GearSetManager.Hrt;
                    newSet.EtroId = string.Empty;
                    ServiceManager.HrtDataManager.GearDb.TryAdd(newSet);
                    ReplaceOriginal(newSet);
                }

                ImGui.Text($"{GeneralLoc.CommonTerms_EtroID}: {DataCopy.EtroId}");
                ImGui.SameLine();
                if (ImGuiHelper.Button(GeneralLoc.EditGearSetUi_btn_openEtro, null))
                    Util.OpenLink(EtroConnector.GEARSET_WEB_BASE_URL + DataCopy.EtroId);

                ImGui.Text(string.Format(GeneralLoc.EditGearSetUi_txt_lastUpdate, DataCopy.EtroFetchDate));
            }
            else
            {
                ImGui.Text($"{GeneralLoc.EditGearSetUi_txt_Source}: {GearSetManager.Hrt.FriendlyName()}");
                if (DataCopy.IsSystemManaged)
                    ImGui.TextColored(Colors.TextRed, GeneralLoc.Gearset_Warning_SysManaged);
            }
            //
            ImGui.Text(GeneralLoc.EditGearSetUi_txt_job);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60 * ScaleFactor);
            ImGui.BeginDisabled(_providedJob is not null);
            ImGuiHelper.Combo("##JobSelection", ref _job);
            ImGui.EndDisabled();
            ImGui.Text(string.Format(GeneralLoc.EditGearSetUi_txt_LastChange, DataCopy.TimeStamp));
            ImGui.BeginDisabled(isFromEtro);
            ImGui.Text($"{GeneralLoc.CommonTerms_Name}: ");
            ImGui.SameLine();
            ImGui.InputText("", ref DataCopy.Name, 100);
            ImGui.NewLine();
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
            return;
            void DrawSlot(GearSetSlot slot)
            {
                ImGui.BeginDisabled(ChildIsOpen);
                ImGui.TableNextColumn();
                UiHelpers.DrawGearEdit(this, slot, DataCopy[slot], i =>
                {
                    foreach (HrtMateria mat in DataCopy[slot].Materia)
                    {
                        i.AddMateria(mat);
                    }
                    DataCopy[slot] = i;
                }, _job);
                ImGui.EndDisabled();
            }
        }

        protected override void Save(GearSet destination)
        {
            if (destination.LocalId.IsEmpty) ServiceManager.HrtDataManager.GearDb.TryAdd(destination);
            DataCopy.TimeStamp = DateTime.Now;
            destination.CopyFrom(DataCopy);
            ServiceManager.HrtDataManager.Save();
        }
    }
}