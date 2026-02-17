using FSquir.Api.Contracts;

namespace FSquir.Api.Validation;

internal static class RequestValidation
{
    internal static bool IsValidPuzzleKey(int level, int seed, string rulesVersion)
    {
        if (level < 1 || level > 1000)
        {
            return false;
        }

        if (seed < 0 || seed > 1000000)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(rulesVersion)
            && rulesVersion.Length <= 32;
    }

    internal static bool IsValidInstallId(string installId)
    {
        return !string.IsNullOrWhiteSpace(installId)
            && installId.Length <= 128;
    }

    internal static bool IsValidCoverage(decimal coveragePercent)
    {
        return coveragePercent >= 0m && coveragePercent <= 100m;
    }

    internal static bool IsValidSubmission(SubmitScoreRequest request)
    {
        return IsValidPuzzleKey(request.Level, request.Seed, request.RulesVersion)
            && IsValidInstallId(request.InstallId)
            && IsValidCoverage(request.CoveragePercent)
            && request.ClientAttemptId != Guid.Empty;
    }
}
