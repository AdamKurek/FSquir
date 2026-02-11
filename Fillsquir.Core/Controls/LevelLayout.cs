namespace Fillsquir.Controls;

internal static class LevelLayout
{
    internal static (int Rows, int Cols) FragmentGrid(int fragments)
    {
        if (fragments <= 0)
        {
            return (Rows: 0, Cols: 0);
        }

        if (fragments < 4)
        {
            return (Rows: 1, Cols: fragments);
        }

        const int rows = 2;
        int cols = (fragments + rows - 1) / rows;
        return (Rows: rows, Cols: cols);
    }
}

