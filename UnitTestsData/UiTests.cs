using System.Collections.Generic;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using Xunit;

namespace UnitTestsData
{
    public class UiTests
    {
        [Fact]
        public void TestUiSortableList()
        {
            List<string> P = new() { "a", "b", "c", "d", "e" };
            List<string> L = new() { "c", "d", "a" };
            UiSortableList<string> sortableList = new(P, L);
            Assert.Equal(L, sortableList.List);
            List<LootRule> RuleSet = new()
            {
                new(LootRuleEnum.BISOverUpgrade),
                new(LootRuleEnum.RolePrio),
                new(LootRuleEnum.HighesItemLevelGain),
                new(LootRuleEnum.LowestItemLevel),
                new(LootRuleEnum.Random)

            };
            LootRuling LootRuling = new()
            {
                RuleSet = RuleSet
            };
            Assert.Equal(RuleSet, LootRuling.RuleSet);
            List<LootRule> P2 = LootRuling.PossibleRules;
            List<LootRule> L2 = LootRuling.RuleSet;

            UiSortableList<LootRule> sortableList2 = new(P2, L2);
            Assert.Equal(5, sortableList2.List.Count);
            Assert.Equal(5, L2.Count);
            for (int i = 0; i < L2.Count; i++)
                Assert.Equal(L2[i], sortableList2.List[i]);
        }
    }
}
