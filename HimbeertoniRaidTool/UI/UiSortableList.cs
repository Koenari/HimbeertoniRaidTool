using Dalamud.Interface;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public class UiSortableList<T> where T : IDrawable
{
    private readonly List<T> _possibilities;
    private readonly List<T> _currentList;
    private int Length => _currentList.Count;

    /// <summary>
    /// Get a copy of the current state of the list.
    /// </summary>
    public IReadOnlyList<T> List => _currentList;

    /// <summary>
    /// Class <c>UiSortableList</c> represents an Ui Element to edit a list and ensures all entries are unique.
    /// </summary>
    /// <param name="possibilities">A List of all possible entries</param>
    /// <param name="inList">An (incomplete) list of entries used as initial state</param>
    public UiSortableList(IEnumerable<T> possibilities, List<T> inList)
    {
        _possibilities = possibilities.ToList();
        _currentList = inList;
        ConsolidateList();
    }

    /// <summary>
    /// Draws Ui for Editing this list using ImGui. Should be called inside of an ImGui Frame.
    /// </summary>
    public bool Draw()
    {
        bool changed = false;
        int id = 0;
        for (int i = 0; i < _currentList.Count; i++)
        {
            ImGui.PushID(id);
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "up", Localize("ui:sortable_list:move_up", "Move up"),
                    i > 0))
            {
                (_currentList[i - 1], _currentList[i]) = (_currentList[i], _currentList[i - 1]);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "down",
                    Localize("ui:sortable_list:move_down", "Move down"),
                    i < _currentList.Count - 1))
            {
                (_currentList[i], _currentList[i + 1]) = (_currentList[i + 1], _currentList[i]);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "delete",
                    Localize("ui:sortable_list:remove", "Remove entry")))
            {
                _currentList.RemoveAt(i);
                changed = true;
            }

            ImGui.SameLine();
            _currentList[i].Draw();


            ImGui.PopID();
            id++;
        }

        if (_currentList.Count < _possibilities.Count)
            if (ImGui.BeginCombo($"{Localize("ui:sortable_list::add", "Add entry")}#add",
                    Localize("ui:sortable_list::add", "Add entry")))
            {
                foreach (T unused in _possibilities)
                {
                    if (_currentList.Contains(unused))
                        continue;
                    if (ImGui.Selectable(unused.ToString())) _currentList.Add(unused);
                }

                ImGui.EndCombo();
            }

        return changed;
    }

    private void ConsolidateList()
    {
        List<T> used = new();
        for (int i = 0; i < _currentList.Count; i++)
        {
            if (used.Contains(_currentList[i]))
            {
                _currentList.RemoveAt(i);
                i--;
                continue;
            }

            used.Add(_currentList[i]);
        }
    }
}

public interface IDrawable
{
    public void Draw();
}