using HimbeertoniRaidTool.Data;
using System;
using Xunit;

namespace UnitTestsData
{
    public class EnumsWithFunctionality
    {
        [Fact]
        public void AllCasesHandledForLootRuleEnum()
        {
            foreach(LootRuleEnum lre in Enum.GetValues(typeof(LootRuleEnum)))
            {
                LootRule lr = new(lre);
                Assert.NotEqual("Not defined", lr.ToString());
                Assert.NotEqual("", lr.ToString());
            }
        }
    }
}
