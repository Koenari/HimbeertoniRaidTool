using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;
namespace HimbeertoniRaidTool.Plugin.UI;

public class UiSortableList<T>
{
    private readonly List<T> Possibilities;
    private readonly int[] OldVals;
    private readonly int[] FieldRefs;
    private int NumItems;
    private int Lenght => NumItems;
    private int NumPossibilities => Possibilities.Count;
    /// <summary>
    /// Get a copy of the current state of the list.
    /// </summary>
    public List<T> List => FieldRefs.Take(Lenght).ToList().ConvertAll((x) => Possibilities[x]);
    /// <summary>
    /// Class <c>UiSortableList</c> represents an Ui Element to edit a list and ensures all entries are unique.
    /// </summary>
    /// <param name="possibilities">A List of all possible entries</param>
    /// <param name="inList">An (incomplete) list of entries used as initial state</param>
    public UiSortableList(IEnumerable<T> possibilities, IEnumerable<T> inList)
    {
        Possibilities = possibilities.ToList();
        FieldRefs = new int[Possibilities.Count];
        OldVals = new int[FieldRefs.Length];
        NumItems = inList.Count() < Possibilities.Count ? inList.Count() : Possibilities.Count;
        List<T> list = inList.ToList();
        for (int i = 0; i < Lenght; i++)
            FieldRefs[i] = Possibilities.FindIndex(x => x?.Equals(list[i]) ?? false);
        ConsolidateList();
        FieldRefs.CopyTo(OldVals, 0);
    }

    /// <summary>
    /// Draws Ui for Editing this list using ImGui. Should be called inside of an ImGui Frame.
    /// </summary>
    public bool Draw()
    {
        bool changed = false;
        for (int i = 0; i < Lenght; i++)
        {

            if (ImGui.Combo("##" + i, ref FieldRefs[i],
                Possibilities.ConvertAll(x => x!.ToString()).ToArray(), Possibilities.Count))
            {
                changed = true;
                ReorganizeList(i);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button($"x##{i}", Localize("Remove option", "Remove option")))
            {
                changed = true;
                DeleteItem(i);
            }
        }
        if (Lenght < Possibilities.Count)
        {
            if (ImGuiHelper.Button("+", Localize("Add option", "Add option")))
            {
                changed = true;
                AddItem();
            }
        }
        return changed;
    }
    private void AddItem()
    {
        NumItems++;
        ConsolidateList();
        FieldRefs.CopyTo(OldVals, 0);
    }
    private void DeleteItem(int changed)
    {
        NumItems--;
        for (int i = changed; i < NumItems; i++)
            FieldRefs[i] = FieldRefs[i + 1];
        ConsolidateList();
        FieldRefs.CopyTo(OldVals, 0);
    }
    private void ReorganizeList(int changed)
    {
        int otherPos = Array.FindIndex(OldVals, x => x == FieldRefs[changed]);
        FieldRefs[otherPos] = OldVals[changed];
        ConsolidateList();
        FieldRefs.CopyTo(OldVals, 0);
    }
    private void ConsolidateList()
    {
        List<int> used = new();
        for (int i = 0; i < NumItems; i++)
        {
            if (used.Contains(FieldRefs[i]))
            {
                for (int j = 0; j < NumPossibilities; j++)
                {
                    if (!used.Contains(j))
                    {
                        FieldRefs[i] = j;
                        used.Add(j);
                        break;
                    }
                }
            }
            else
            {
                used.Add(FieldRefs[i]);
            }
        }

    }
}
