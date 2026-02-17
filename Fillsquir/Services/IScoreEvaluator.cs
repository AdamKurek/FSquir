namespace Fillsquir.Services;

public interface IScoreEvaluator
{
    decimal ComputeCoveragePercent(double coveredArea, double maxArea);
    decimal? GetReferenceRecord(decimal? worldRecordCoveragePercent, decimal? localBestCoveragePercent);
    int ComputeStars(decimal coveragePercent, decimal? worldRecordCoveragePercent, decimal? localBestCoveragePercent);
}
