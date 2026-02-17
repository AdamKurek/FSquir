namespace Fillsquir.Controls;

internal static class ScoreMath
{
    internal const decimal ComparisonTolerance = 0.0001m;

    internal static decimal ComputeCoveragePercent(double coveredArea, double maxArea)
    {
        if (maxArea <= 0d || double.IsNaN(maxArea) || double.IsInfinity(maxArea))
        {
            return 0m;
        }

        double ratio = coveredArea / maxArea;
        if (double.IsNaN(ratio) || double.IsInfinity(ratio))
        {
            return 0m;
        }

        ratio = Math.Clamp(ratio, 0d, 1d);
        return Math.Round((decimal)(ratio * 100d), 4, MidpointRounding.AwayFromZero);
    }

    internal static decimal? ReferenceRecord(decimal? worldRecordCoveragePercent, decimal? localBestCoveragePercent)
    {
        if (worldRecordCoveragePercent.HasValue && worldRecordCoveragePercent.Value > 0m)
        {
            return worldRecordCoveragePercent.Value;
        }

        if (localBestCoveragePercent.HasValue && localBestCoveragePercent.Value > 0m)
        {
            return localBestCoveragePercent.Value;
        }

        return null;
    }

    internal static int ComputeStars(
        decimal coveragePercent,
        decimal? worldRecordCoveragePercent,
        decimal? localBestCoveragePercent,
        decimal oneStarFraction,
        decimal twoStarFraction,
        decimal threeStarFraction)
    {
        decimal? reference = ReferenceRecord(worldRecordCoveragePercent, localBestCoveragePercent);
        if (!reference.HasValue || reference.Value <= 0m)
        {
            return 0;
        }

        decimal oneStarThreshold = reference.Value * oneStarFraction;
        decimal twoStarThreshold = reference.Value * twoStarFraction;
        decimal threeStarThreshold = reference.Value * threeStarFraction;

        if (coveragePercent + ComparisonTolerance >= threeStarThreshold)
        {
            return 3;
        }

        if (coveragePercent + ComparisonTolerance >= twoStarThreshold)
        {
            return 2;
        }

        if (coveragePercent + ComparisonTolerance >= oneStarThreshold)
        {
            return 1;
        }

        return 0;
    }
}
