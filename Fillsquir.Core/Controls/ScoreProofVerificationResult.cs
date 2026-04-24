namespace Fillsquir.Controls;

public sealed class ScoreProofVerificationResult
{
    private ScoreProofVerificationResult(bool isValid, decimal verifiedCoveragePercent, string? failureReason)
    {
        IsValid = isValid;
        VerifiedCoveragePercent = verifiedCoveragePercent;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }
    public decimal VerifiedCoveragePercent { get; }
    public string? FailureReason { get; }

    internal static ScoreProofVerificationResult Valid(decimal verifiedCoveragePercent)
    {
        return new ScoreProofVerificationResult(true, verifiedCoveragePercent, null);
    }

    internal static ScoreProofVerificationResult Invalid(string failureReason, decimal verifiedCoveragePercent = 0m)
    {
        return new ScoreProofVerificationResult(false, verifiedCoveragePercent, failureReason);
    }
}
