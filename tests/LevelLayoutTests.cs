using Fillsquir.Controls;

namespace tests;

[TestClass]
public class LevelLayoutTests
{
    [TestMethod]
    public void FragmentGrid_FragmentsBelowFour_UsesSingleRow()
    {
        var (rows, cols) = LevelLayout.FragmentGrid(3);

        Assert.AreEqual(1, rows);
        Assert.AreEqual(3, cols);
    }

    [TestMethod]
    public void FragmentGrid_FragmentsFour_UsesTwoByTwo()
    {
        var (rows, cols) = LevelLayout.FragmentGrid(4);

        Assert.AreEqual(2, rows);
        Assert.AreEqual(2, cols);
    }

    [TestMethod]
    public void FragmentGrid_FragmentsOdd_RoundsUpColumns()
    {
        var (rows, cols) = LevelLayout.FragmentGrid(5);

        Assert.AreEqual(2, rows);
        Assert.AreEqual(3, cols);
    }
}

