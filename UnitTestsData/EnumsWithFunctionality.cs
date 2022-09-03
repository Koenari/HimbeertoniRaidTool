using System;
using HimbeertoniRaidTool.Data;
using Xunit;
namespace UnitTestsData
{
    public class EnumsWithFunctionality
    {
        //Does not work because of Localize
        public void AllCasesHandledForLootRuleEnum()
        {
            foreach (LootRuleEnum lre in Enum.GetValues(typeof(LootRuleEnum)))
            {
                LootRule lr = new(lre);
                Assert.NotEqual("Not defined", lr.ToString());
                Assert.NotEqual("", lr.ToString());
            }
        }
        [Fact]
        public void AllCasesHandledForMainstat()
        {
            //var mock = new Moq.Mock<Dalamud>
            foreach (Job c in Enum.GetValues(typeof(Job)))
            {
                try
                {
                    Assert.NotEqual(StatType.Vitality, c.MainStat());
                }
                catch
                {
                    Assert.True(false);
                }
            }
        }
        [Fact]
        public void AllCasesHandledForRole()
        {
            foreach (Job c in Enum.GetValues(typeof(Job)))
            {
                if (c != Job.ADV)
                    Assert.NotEqual(Role.None, c.GetRole());
            }
        }
    }
}
