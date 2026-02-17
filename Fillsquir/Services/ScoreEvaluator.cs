using Fillsquir.Domain;
using Fillsquir.Controls;

namespace Fillsquir.Services;

internal sealed class ScoreEvaluator : IScoreEvaluator
{
    public decimal ComputeCoveragePercent(double coveredArea, double maxArea)
    {
        return ScoreMath.ComputeCoveragePercent(coveredArea, maxArea);
    }

    public decimal? GetReferenceRecord(decimal? worldRecordCoveragePercent, decimal? localBestCoveragePercent)
    {
        return ScoreMath.ReferenceRecord(worldRecordCoveragePercent, localBestCoveragePercent);
    }

    public int ComputeStars(decimal coveragePercent, decimal? worldRecordCoveragePercent, decimal? localBestCoveragePercent)
    {
        return ScoreMath.ComputeStars(
            coveragePercent,
            worldRecordCoveragePercent,
            localBestCoveragePercent,
            GameRules.OneStarFraction,
            GameRules.TwoStarFraction,
            GameRules.ThreeStarFraction);
    }
}
