using Fillsquir.Controls;

namespace tests;

[TestClass]
public class ProgressionTests
{
    [TestMethod]
    public void FragmentAreaDivisors_AreMonotonicAcrossLevels()
    {
        float previousMinDivisor = 0f;
        float previousMaxDivisor = 0f;

        for (int level = 1; level <= 100; level++)
        {
            var (minDivisor, maxDivisor) = SquirGenerator.FragmentAreaDivisorsForLevel(level);
            Assert.IsTrue(minDivisor > maxDivisor, $"Level {level} has invalid divisors.");

            if (level > 1)
            {
                Assert.IsTrue(minDivisor >= previousMinDivisor, $"Min divisor regressed at level {level}.");
                Assert.IsTrue(maxDivisor >= previousMaxDivisor, $"Max divisor regressed at level {level}.");
            }

            previousMinDivisor = minDivisor;
            previousMaxDivisor = maxDivisor;
        }
    }

    [TestMethod]
    public void WallAngleSet_NeverDecreasesWithLevel()
    {
        int previous = 0;

        for (int level = 1; level <= 100; level++)
        {
            int current = WallAngleSet.WallAnglesForLevel(level);
            Assert.IsTrue(current >= previous, $"Wall angle count decreased at level {level}.");
            previous = current;
        }
    }
}
