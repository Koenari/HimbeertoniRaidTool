using ImGuiNET;
using System;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.UI
{
    public class UiSortableList<T>
    {
        private readonly List<T> Possibilities;
        private int[] FieldRefs;
        private int Lenght => FieldRefs.Length;
        public List<T> List => new List<int>(FieldRefs).ConvertAll((x) => Possibilities[x]);
        public UiSortableList(List<T> possibilities, List<T> list)
        {
            Possibilities = possibilities;
            FieldRefs = new int[Possibilities.Count];
            Array.Copy(list.ConvertAll( x => Possibilities.FindIndex(y => x!.Equals(y))).ToArray(), FieldRefs, FieldRefs.Length < list.Count ? FieldRefs.Length : list.Count);
            ReorganizeList(0);
        }
        

        public void Draw()
        {
            for(int i = 0; i < Lenght; i++)
            {
                if (ImGui.Combo(i.ToString(), ref FieldRefs[i], 
                    Possibilities.ConvertAll(x=> x!.ToString()).ToArray(), Possibilities.Count))
                {
                    ReorganizeList(i);
                }
            }
        }
        private void ReorganizeList(int changed)
        {
            SortedList<int,int> used = new();
            used.Add(FieldRefs[changed],FieldRefs[changed]);
            for(int i = 0; i < Lenght; i++)
            {
                if (i == changed)
                    continue;
                if (used.ContainsKey(FieldRefs[i]))
                {
                    for (int j = 0; j < Lenght; j++)
                    {
                        if (!used.ContainsKey(j))
                        {
                            FieldRefs[i] = j;
                            used.Add(j, j);
                            break;
                        }
                    }
                } else
                {
                    used.Add(FieldRefs[i], FieldRefs[i]);
                }
            }

        }
    }
}
