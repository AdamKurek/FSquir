using Fillsquir.Domain;
using SkiaSharp;

namespace Fillsquir.Visuals;

public interface IPuzzleMaterialService
{
    SKShader GetBoardShader(PuzzleKey puzzleKey, VisualSettings settings, SKRect boardRect);
    SKPaint GetPieceFillPaint(PuzzleKey puzzleKey, VisualSettings settings, SKRect boardRect, SKRect pieceRect, bool forcePieceLocal);
    SKPaint GetPieceShadowPaint(VisualSettings settings, bool isDragging, float elevationMultiplier);
    SKPaint GetPieceBevelPaint(VisualSettings settings, SKRect pieceRect, bool darkPass);
    SKPaint GetStripBackgroundPaint(PuzzleKey puzzleKey, VisualSettings settings, SKRect stripRect);
    SKPaint GetStripDividerPaint(VisualSettings settings);
    SKPaint GetOutlinePaint(VisualSettings settings);
    MaterialEffectFlags GetQualityEffects(GraphicsQualityTier qualityTier);
    void InvalidateCacheForSkinOrSeed(PuzzleKey puzzleKey, string skinId);
}
