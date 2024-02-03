using Dalamud.Interface;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.UI;

public class UiSortableList<T> where T : IDrawable
{
    private readonly List<T> _currentList;
    private readonly List<T> _possibilities;

    /// <summary>
    ///     Class <c>UiSortableList</c> represents an Ui Element to edit a list and ensures all entries are
    ///     unique.
    /// </summary>
    /// <param name="possibilities">A List of all possible entries</param>
    /// <param name="inList">An (incomplete) list of entries used as initial state</param>
    public UiSortableList(IEnumerable<T> possibilities, List<T> inList)
    {
        _possibilities = possibilities.ToList();
        _currentList = inList;
        ConsolidateList();
    }
    private int Length => _currentList.Count;

    /// <summary>
    ///     Get a copy of the current state of the list.
    /// </summary>
    public IReadOnlyList<T> List => _currentList;

    /// <summary>
    ///     Draws Ui for Editing this list using ImGui. Should be called inside of an ImGui Frame.
    /// </summary>
    public bool Draw()
    {
        bool changed = false;
        int id = 0;
        for (int i = 0; i < _currentList.Count; i++)
        {
            ImGui.PushID(id);
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "up", GeneralLoc.SortableList_moveUp,
                                   i > 0))
            {
                (_currentList[i - 1], _currentList[i]) = (_currentList[i], _currentList[i - 1]);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "down", GeneralLoc.SortableList_moveDown,
                                   i < _currentList.Count - 1))
            {
                (_currentList[i], _currentList[i + 1]) = (_currentList[i + 1], _currentList[i]);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "delete", GeneralLoc.SortableList_remove))
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
            if (ImGui.BeginCombo($"{GeneralLoc.SortableList_Add}#add", GeneralLoc.SortableList_Add))
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