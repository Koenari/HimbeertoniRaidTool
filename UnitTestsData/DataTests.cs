using HimbeertoniRaidTool.Data;
using Xunit;

namespace UnitTestsData
{
    public class DataTests
    {
        [Fact]
        public void LootRuleEqualsTest()
        {
            Assert.Equal(new LootRule(LootRuleEnum.Random), new LootRule(LootRuleEnum.Random));
            Assert.NotEqual(new LootRule(LootRuleEnum.Random), (new LootRule(LootRuleEnum.LowestItemLevel)));
            Assert.False(new LootRule(LootRuleEnum.Random).Equals("Roll"));
        }
        [Fact]
        public void CharacterEqualsTest()
        {
            Assert.Equal(new Character("a"), new Character("a"));
            Assert.NotEqual(new Character("a"), new Character("b"));
            Assert.False(new Character("a").Equals("a"));
        }
        [Fact]
        public void LootSourceEqualsTest()
        {
            RaidTier T1 = new(6, 1, EncounterDifficulty.Savage, 605, 600, "Asphodelos", 9);
            RaidTier T2 = new(6, 1, EncounterDifficulty.Normal, 590, 580, "Asphodelos", 9);
            Assert.Equal(new LootSource((T1, 1)), new LootSource((T1, 1)));
            Assert.NotEqual(new LootSource((T1, 1)), new LootSource((T1, 2)));
            Assert.NotEqual(new LootSource((T2, 1)), new LootSource((T1, 1)));
            Assert.True(new LootSource(T1, 1) == new LootSource(T1, 1));
            Assert.True(new LootSource(T1, 1) != new LootSource(T1, 2));
            Assert.False(new LootSource((T1, 1)).Equals(T1));
        }
        [Fact]
        public void ItemIDRangeEqualsTest()
        {
            ItemIDRange Range1 = 15111;
            ItemIDRange Range1a = 15111;
            ItemIDRange Range2 = 14111;
            ItemIDRange Range3 = (15000, 16000);

            Assert.Equal(Range1, Range1);
            Assert.Equal(Range1, Range1a);
            Assert.NotEqual(Range1, Range2);
            Assert.NotEqual(Range3, Range2);
            Assert.NotEqual(Range3, Range1);
            Assert.NotEqual(Range1, Range3);
            Assert.True(Range3.Contains(15123));
            Assert.False(Range3.Contains(14123));
            Assert.True(!Range3.Contains(14111));
            Assert.False(!Range3.Contains(15123));
            Assert.True(Range3.Contains(15000));
            Assert.True(Range3.Contains(16000));
            Assert.False(Range3.Equals("a"));
        }
        [Fact]
        public void ItemIDRangeEnumeratorTest()
        {
            ItemIDRange Range1 = 25;
            uint i = 25;
            foreach (uint x in Range1.AsList)
                Assert.Equal(i, x);
            ItemIDRange Range2 = (15, 20);
            uint j = 15;
            foreach (uint y in Range2.AsList)
            {
                Assert.Equal(j, y);
                j++;
            }


        }

        [Fact]
        public void MateriaReorganize()
        {
            GearItem g1 = new(2000);
            g1.Materia.Add(new HrtMateria(MateriaCategory.DirectHit, 10));
            g1.Materia.Add(new HrtMateria(MateriaCategory.CriticalHit, 10));
            GearItem g2 = new(2000);
            g2.Materia.Add(new HrtMateria(MateriaCategory.CriticalHit, 10));
            g2.Materia.Add(new HrtMateria(MateriaCategory.DirectHit, 10));
            Assert.Equal(g1, g2);
        }
    }
}
