using Fillsquir.Domain;
using SkiaSharp;

namespace Fillsquir.Visuals;

public interface IPuzzleMaterialService
{
    void DrawBoardFill(SKCanvas canvas, SKPath path, PuzzleKey puzzleKey, VisualSettings settings, SKRect textureRect, SKRect sourceRect, SKRect surfaceRect);
    SKShader GetBoardShader(PuzzleKey puzzleKey, VisualSettings settings, SKRect textureRect, SKRect sourceRect, SKRect effectRect, SKRect textureSurfaceRect);
    SKPaint GetPieceFillPaint(PuzzleKey puzzleKey, VisualSettings settings, SKRect textureRect, SKRect sourceRect, SKRect effectRect, SKRect textureSurfaceRect, bool forcePieceLocal);
    SKPaint GetPieceShadowPaint(VisualSettings settings, bool isDragging, float elevationMultiplier);
    SKPaint GetPieceBevelPaint(VisualSettings settings, SKRect pieceRect, bool darkPass);
    SKPaint GetStripBackgroundPaint(PuzzleKey puzzleKey, VisualSettings settings, SKRect stripRect);
    SKPaint GetStripDividerPaint(VisualSettings settings);
    SKPaint GetOutlinePaint(VisualSettings settings);
    MaterialEffectFlags GetQualityEffects(GraphicsQualityTier qualityTier);
    void InvalidateCacheForSkinOrSeed(PuzzleKey puzzleKey, string skinId);
}
