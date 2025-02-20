using Dalamud.Interface;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.UI;

public class UiSortableList<T> where T : IDrawable, IHrtDataType
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
            var currentItem = _currentList[i];
            ImGui.PushID(id);
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##up",
                                   string.Format(GeneralLoc.SortableList_btn_tt_moveUp, currentItem.DataTypeName),
                                   i > 0))
            {
                (_currentList[i - 1], _currentList[i]) = (_currentList[i], _currentList[i - 1]);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##down",
                                   string.Format(GeneralLoc.SortableList_btn_tt_moveDown, currentItem.DataTypeName),
                                   i < _currentList.Count - 1))
            {
                (_currentList[i], _currentList[i + 1]) = (_currentList[i + 1], _currentList[i]);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "##delete",
                                   string.Format(GeneralLoc.General_btn_tt_delete, currentItem.DataTypeName,
                                                 currentItem.Name)))
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
            if (ImGui.BeginCombo(
                    $"{string.Format(GeneralLoc.Ui_btn_tt_add, _possibilities.First().DataTypeName)}#add",
                    string.Format(GeneralLoc.Ui_btn_tt_add, _possibilities.First().DataTypeName)))
            {
                foreach (var unused in _possibilities.Where(item => !_currentList.Contains(item)))
                {
                    if (ImGui.Selectable(unused.ToString()))
                        _currentList.Add(unused);
                    changed = true;
                }

                ImGui.EndCombo();
            }

        return changed;
    }

    private void ConsolidateList()
    {
        HashSet<T> used = new();
        for (int i = 0; i < _currentList.Count; i++)
        {
            if (used.Add(_currentList[i]))
                continue;
            _currentList.RemoveAt(i);
            i--;

        }
    }
}

public interface IDrawable
{
    public void Draw();
}