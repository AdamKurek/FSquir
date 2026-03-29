using Fillsquir.Campaign;
using Fillsquir.Domain;
using Fillsquir.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Diagnostics;

namespace Fillsquir;

public partial class GameSelectionPage : ContentPage
{
    private const int PageSize = 12;
    private const int InitialSectionCount = 2;
    private const int InitialSectionCap = 3;
    private const int SectionBatchSize = 2;
    private const double ScrollPrefetchThreshold = 320d;

    private readonly IProgressStore progressStore;
    private readonly CampaignProgressionService progressionService;
    private readonly List<CampaignSectionModel> loadedSections = new();

    private CampaignCatalogState? catalogState;
    private bool hasBootstrappedLayout;
    private bool hasResolvedProgress;
    private bool isProgressHydrating;
    private bool isRefreshing;
    private bool isLoadingSections;
    private bool isRebuildingSections;
    private bool hasAnimatedChrome;
    private int nextSectionIndex;
    private int currentColumnCount = 3;
    private int sectionGeneration;

    public GameSelectionPage()
    {
        IServiceProvider? services = App.Services;
        progressStore = services?.GetService(typeof(IProgressStore)) as IProgressStore ?? new JsonFileProgressStore();
        progressionService = services?.GetService(typeof(CampaignProgressionService)) as CampaignProgressionService
            ?? new CampaignProgressionService(
                services?.GetService(typeof(IScoreEvaluator)) as IScoreEvaluator
                ?? new ScoreEvaluator());

        Shell.SetNavBarIsVisible(this, false);
        InitializeComponent();
        SizeChanged += GameSelectionPage_SizeChanged;

        PrimeForEntry(topBarPanel, 8, 0.995);
        PrimeForEntry(heroHeaderPanel, 12, 0.995);
        PrimeForEntry(loadPanel, 10, 0.995);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        BootstrapLayout();

        if (hasAnimatedChrome)
        {
            _ = SafeRefreshCampaignAsync();
            return;
        }

        hasAnimatedChrome = true;
        _ = AnimateChromeAsync();
        _ = SafeRefreshCampaignAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = NavigateBackAsync();
        return true;
    }

    private async Task RefreshCampaignAsync()
    {
        if (isRefreshing)
        {
            return;
        }

        isRefreshing = true;
        isProgressHydrating = true;
        UpdateSummary();
        SetLoadingState(isActive: true, "Syncing progress");

        try
        {
            IReadOnlyList<LevelProgress> savedProgress = await progressStore.LoadAllLevelProgressAsync();
            List<CampaignProgressEntry> progressEntries = savedProgress
                .Where(static progress => progress.PuzzleKey.Level > 0)
                .Where(static progress => progress.PuzzleKey.Seed == 0)
                .Where(static progress => string.Equals(progress.PuzzleKey.RulesVersion, GameRules.RulesVersion, StringComparison.OrdinalIgnoreCase))
                .Select(static progress => new CampaignProgressEntry(
                    Level: progress.PuzzleKey.Level,
                    BestCoveragePercent: progress.BestCoveragePercent,
                    WorldRecordCoveragePercent: progress.WorldRecordCoveragePercent,
                    HasSavedSnapshot: progress.BestSnapshot is not null))
                .ToList();

            catalogState = progressionService.BuildCatalog(progressEntries);
            hasResolvedProgress = true;
            currentColumnCount = DetermineColumnCount(Width);

            int currentSectionIndex = (catalogState.CurrentLevel - 1) / PageSize;
            int hydratedTargetSectionCount = Math.Min(InitialSectionCap, Math.Max(InitialSectionCount, currentSectionIndex + 1));

            isRebuildingSections = true;
            try
            {
                ResetLoadedSections();
                isProgressHydrating = false;
                UpdateSummary();
                EnsureSectionsLoadedImmediate(hydratedTargetSectionCount);
            }
            finally
            {
                isRebuildingSections = false;
            }
        }
        finally
        {
            isRefreshing = false;
            isProgressHydrating = false;
            UpdateSummary();
            if (!isLoadingSections)
            {
                SetLoadingState(isActive: false, "Ready");
            }
        }
    }

    private async Task SafeRefreshCampaignAsync()
    {
        try
        {
            await RefreshCampaignAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GameSelectionPage] Refresh failed: {ex}");
            isProgressHydrating = false;
            isRebuildingSections = false;
            SetLoadingState(isActive: false, "Load failed");
        }
    }

    private async Task EnsureSectionsLoadedAsync(int targetSectionCount, bool animate)
    {
        if (catalogState is null || isLoadingSections || isRebuildingSections || isProgressHydrating)
        {
            return;
        }

        isLoadingSections = true;
        SetLoadingState(isActive: true, "Loading");
        int activeGeneration = sectionGeneration;

        try
        {
            while (loadedSections.Count < targetSectionCount)
            {
                if (activeGeneration != sectionGeneration || catalogState is null)
                {
                    break;
                }

                CampaignSectionModel section = progressionService.BuildSection(nextSectionIndex, PageSize, catalogState);
                loadedSections.Add(section);

                View sectionView = CreateSectionView(section, animate);
                if (activeGeneration != sectionGeneration)
                {
                    break;
                }

                sectionsHost.Children.Add(sectionView);
                nextSectionIndex++;

                if (animate)
                {
                    _ = AnimateSectionEntryAsync(sectionView, loadedSections.Count - 1);
                }

                await Task.Yield();
            }
        }
        finally
        {
            isLoadingSections = false;
            SetLoadingState(isActive: false, "Ready");
        }
    }

    private void UpdateSummary()
    {
        if (isProgressHydrating && !hasResolvedProgress)
        {
            statusPillLabel.Text = "SYNC";
            campaignSummaryLabel.Text = "Campaign";
            campaignSubSummaryLabel.Text = "Loading";
            clearedCounterLabel.Text = "--";
            sectorCounterLabel.Text = $"{PageSize}";
            return;
        }

        if (catalogState is null)
        {
            statusPillLabel.Text = "CURRENT";
            campaignSummaryLabel.Text = "Level 01";
            campaignSubSummaryLabel.Text = "Sector 01";
            clearedCounterLabel.Text = "0";
            sectorCounterLabel.Text = $"{PageSize}";
            return;
        }

        int activeSector = (catalogState.CurrentLevel - 1) / PageSize + 1;
        statusPillLabel.Text = "CURRENT";
        campaignSummaryLabel.Text = $"Level {catalogState.CurrentLevel:00}";
        campaignSubSummaryLabel.Text = $"Sector {activeSector:00}";
        clearedCounterLabel.Text = $"{catalogState.CompletedLevelCount}";
        sectorCounterLabel.Text = $"{PageSize}";
    }

    private void SetLoadingState(bool isActive, string message)
    {
        headerLoadIndicator.IsVisible = isActive;
        headerLoadIndicator.IsRunning = isActive;
        loadMoreIndicator.IsVisible = isActive;
        loadMoreIndicator.IsRunning = isActive;
        loadStatusLabel.Text = message;
    }

    private void GameSelectionPage_SizeChanged(object? sender, EventArgs e)
    {
        int nextColumnCount = DetermineColumnCount(Width);
        if (nextColumnCount == currentColumnCount || loadedSections.Count == 0)
        {
            return;
        }

        currentColumnCount = nextColumnCount;
        RebuildLoadedSections();
    }

    private void RebuildLoadedSections()
    {
        sectionsHost.Children.Clear();
        foreach (CampaignSectionModel section in loadedSections)
        {
            sectionsHost.Children.Add(CreateSectionView(section, animateOnEntry: false));
        }
    }

    private void BootstrapLayout()
    {
        if (hasBootstrappedLayout)
        {
            return;
        }

        hasBootstrappedLayout = true;
        isProgressHydrating = true;
        catalogState = progressionService.BuildCatalog(Array.Empty<CampaignProgressEntry>());
        currentColumnCount = DetermineColumnCount(Width);

        ResetLoadedSections();
        UpdateSummary();
        EnsureSectionsLoadedImmediate(InitialSectionCount);
        SetLoadingState(isActive: true, "Syncing progress");
    }

    private void ResetLoadedSections()
    {
        sectionGeneration++;
        loadedSections.Clear();
        nextSectionIndex = 0;
        sectionsHost.Children.Clear();
    }

    private void EnsureSectionsLoadedImmediate(int targetSectionCount)
    {
        if (catalogState is null)
        {
            return;
        }

        while (loadedSections.Count < targetSectionCount)
        {
            CampaignSectionModel section = progressionService.BuildSection(nextSectionIndex, PageSize, catalogState);
            loadedSections.Add(section);
            sectionsHost.Children.Add(CreateSectionView(section, animateOnEntry: false));
            nextSectionIndex++;
        }
    }

    private static int DetermineColumnCount(double width)
    {
        if (width >= 1180d)
        {
            return 5;
        }

        if (width >= 820d)
        {
            return 4;
        }

        return 3;
    }

    private static Style GetStyle(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out object? resource) == true
            && resource is Style style)
        {
            return style;
        }

        return new Style(typeof(Label));
    }

    private View CreateSectionView(CampaignSectionModel section, bool animateOnEntry)
    {
        Color accent = Color.FromArgb(section.Theme.AccentHex);
        Color glow = Color.FromArgb(section.Theme.GlowHex);
        Color fog = Color.FromArgb("#F5F7FF");

        Border container = new()
        {
            Padding = new Thickness(16),
            Background = CreateSectionBackground(section.Theme),
            Stroke = new SolidColorBrush(ApplyAlpha(Color.FromArgb(section.Theme.StrokeHex), 0.9f)),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24) },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(ApplyAlpha(glow, (float)Math.Min(0.45d, 0.14d + section.Theme.Drama * 0.16d))),
                Opacity = 0.8f,
                Radius = 24f + (float)(section.Theme.Drama * 10d),
                Offset = new Point(0, 14)
            }
        };

        if (animateOnEntry)
        {
            PrimeForEntry(container, 18, 0.99);
        }

        Grid shell = new()
        {
            RowSpacing = 12
        };
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        VerticalStackLayout titleStack = new()
        {
            Spacing = 2
        };
        titleStack.Children.Add(new Label
        {
            Text = section.Title,
            Style = GetStyle("ArcadeSectionTitle"),
            FontSize = 20
        });
        titleStack.Children.Add(new Label
        {
            Text = $"{section.StartLevel:00}-{section.EndLevel:00}  {section.Subtitle}",
            Style = GetStyle("ArcadeCaptionText"),
            TextColor = ApplyAlpha(fog, 0.82f),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        Border accentBar = new()
        {
            HeightRequest = 4,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(accent, 0f),
                    new GradientStop(ApplyAlpha(glow, 0.85f), 0.55f),
                    new GradientStop(ApplyAlpha(Color.FromArgb(section.Theme.AccentMutedHex), 0.6f), 1f)
                },
                new Point(0, 0),
                new Point(1, 0))
        };

        Grid levelsGrid = CreateLevelGrid(section);

        shell.Children.Add(titleStack);
        Grid.SetRow(titleStack, 0);
        shell.Children.Add(accentBar);
        Grid.SetRow(accentBar, 1);
        shell.Children.Add(levelsGrid);
        Grid.SetRow(levelsGrid, 2);

        container.Content = shell;
        return container;
    }

    private Grid CreateLevelGrid(CampaignSectionModel section)
    {
        Grid grid = new()
        {
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        for (int columnIndex = 0; columnIndex < currentColumnCount; columnIndex++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        int totalRows = (int)Math.Ceiling(section.Levels.Count / (double)currentColumnCount);
        for (int rowIndex = 0; rowIndex < totalRows; rowIndex++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (int index = 0; index < section.Levels.Count; index++)
        {
            CampaignLevelCard card = section.Levels[index];
            View cardView = CreateLevelCardView(card, section.Theme);
            int rowIndex = index / currentColumnCount;
            int columnIndex = index % currentColumnCount;

            grid.Children.Add(cardView);
            Grid.SetRow(cardView, rowIndex);
            Grid.SetColumn(cardView, columnIndex);
        }

        return grid;
    }

    private View CreateLevelCardView(CampaignLevelCard card, CampaignSectionTheme theme)
    {
        Color accent = Color.FromArgb(theme.AccentHex);
        Color glow = Color.FromArgb(theme.GlowHex);
        Color text = Color.FromArgb("#F5F7FF");
        Color muted = Color.FromArgb("#97AAC9");
        Color panel = Color.FromArgb("#10192B");
        bool isLegacyOpen = card.State == CampaignLevelState.Locked && card.IsPlayable;
        bool showLoadingPlaceholder = isProgressHydrating && !hasResolvedProgress;
        bool isFreshPlayable = !showLoadingPlaceholder
            && card.State == CampaignLevelState.Current
            && card.BestCoveragePercent <= 0m;

        Border cardBorder = new()
        {
            Padding = new Thickness(14, 12),
            MinimumHeightRequest = 172,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(22) }
        };

        if (showLoadingPlaceholder)
        {
            cardBorder.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#0D1727"), 0f),
                    new GradientStop(Color.FromArgb("#122036"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1));
            cardBorder.Stroke = new SolidColorBrush(Color.FromArgb("#35506F"));
            cardBorder.StrokeThickness = 1;
            cardBorder.Opacity = 0.78;
        }
        else if (isFreshPlayable)
        {
            cardBorder.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#152338"), 0f),
                    new GradientStop(ApplyAlpha(accent, 0.16f), 0.5f),
                    new GradientStop(Color.FromArgb("#1A2A42"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1));
            cardBorder.Stroke = new SolidColorBrush(ApplyAlpha(accent, 0.58f));
            cardBorder.StrokeThickness = 1.5;
            cardBorder.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(ApplyAlpha(glow, 0.16f)),
                Opacity = 0.7f,
                Radius = 14f,
                Offset = new Point(0, 8)
            };
        }
        else if (card.State == CampaignLevelState.Current)
        {
            cardBorder.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(ApplyAlpha(accent, 0.42f), 0f),
                    new GradientStop(ApplyAlpha(glow, 0.22f), 0.55f),
                    new GradientStop(Color.FromArgb(theme.SurfaceEndHex), 1f)
                },
                new Point(0, 0),
                new Point(1, 1));
            cardBorder.Stroke = new SolidColorBrush(ApplyAlpha(accent, 0.95f));
            cardBorder.StrokeThickness = 2;
            cardBorder.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(ApplyAlpha(glow, 0.42f)),
                Opacity = 0.92f,
                Radius = 20f,
                Offset = new Point(0, 12)
            };
        }
        else if (card.State == CampaignLevelState.Completed || isLegacyOpen)
        {
            cardBorder.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(theme.SurfaceStartHex), 0f),
                    new GradientStop(ApplyAlpha(accent, isLegacyOpen ? 0.18f : 0.12f), 0.45f),
                    new GradientStop(Color.FromArgb(theme.SurfaceEndHex), 1f)
                },
                new Point(0, 0),
                new Point(1, 1));
            cardBorder.Stroke = new SolidColorBrush(ApplyAlpha(accent, isLegacyOpen ? 0.72f : 0.52f));
            cardBorder.StrokeThickness = 1;
            cardBorder.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(ApplyAlpha(glow, 0.16f)),
                Opacity = 0.72f,
                Radius = 14f,
                Offset = new Point(0, 8)
            };
        }
        else
        {
            cardBorder.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#0C1422"), 0f),
                    new GradientStop(Color.FromArgb("#111C2E"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1));
            cardBorder.Stroke = new SolidColorBrush(Color.FromArgb("#273953"));
            cardBorder.StrokeThickness = 1;
            cardBorder.Opacity = 0.92;
        }

        Grid content = new()
        {
            RowSpacing = 10
        };
        for (int row = 0; row < 6; row++)
        {
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        Grid topRow = new()
        {
            ColumnSpacing = 10
        };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        string pillText = showLoadingPlaceholder
            ? "SYNC"
            : card.State switch
        {
            CampaignLevelState.Current => "CURRENT",
            CampaignLevelState.Completed => "CLEAR",
            _ => card.IsPlayable ? "OPEN" : "LOCK"
        };

        Border pill = new()
        {
            Padding = new Thickness(10, 6),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Background = new SolidColorBrush(showLoadingPlaceholder
                ? ApplyAlpha(Color.FromArgb("#6D8AA8"), 0.34f)
                : GetPillColor(card.State, accent, card.IsPlayable)),
            Content = new Label
            {
                Text = pillText,
                Style = GetStyle("ArcadeCaptionText"),
                TextColor = showLoadingPlaceholder
                    ? Color.FromArgb("#E8F1FF")
                    : card.State == CampaignLevelState.Current ? Color.FromArgb("#08111F") : text
            }
        };

        Label levelToken = new()
        {
            Text = $"L{card.Level:00}",
            Style = GetStyle("ArcadeCaptionText"),
            HorizontalTextAlignment = TextAlignment.End,
            TextColor = muted,
            VerticalTextAlignment = TextAlignment.Center
        };

        topRow.Children.Add(pill);
        Grid.SetColumn(pill, 0);
        topRow.Children.Add(levelToken);
        Grid.SetColumn(levelToken, 1);

        Label title = new()
        {
            Text = $"Level {card.Level:00}",
            FontFamily = "OpenSansSemibold",
            FontSize = 22,
            TextColor = text
        };

        Grid statGrid = new()
        {
            ColumnSpacing = 8
        };
        statGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        statGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        Border bestChip = CreateStatCard(
            "BEST",
            showLoadingPlaceholder ? "--" : card.BestCoveragePercent > 0m ? $"{card.BestCoveragePercent:F1}%" : "--",
            accent,
            panel);
        Border worldChip = CreateStatCard(
            "WORLD",
            showLoadingPlaceholder ? "--" : card.WorldRecordCoveragePercent is decimal world ? $"{world:F1}%" : "--",
            glow,
            panel);

        statGrid.Children.Add(bestChip);
        Grid.SetColumn(bestChip, 0);
        statGrid.Children.Add(worldChip);
        Grid.SetColumn(worldChip, 1);

        ProgressBar progressBar = new()
        {
            Progress = showLoadingPlaceholder ? 0d : Math.Clamp((double)(card.BestCoveragePercent / 100m), 0d, 1d),
            ProgressColor = showLoadingPlaceholder ? Color.FromArgb("#587493") : card.IsPlayable ? accent : Color.FromArgb("#41556F"),
            BackgroundColor = Color.FromArgb("#1C2A42"),
            HeightRequest = 6
        };

        View stars = CreateStarStrip(card, accent, muted, showLoadingPlaceholder);

        Label status = new()
        {
            Text = showLoadingPlaceholder
                ? "Syncing"
                : card.State switch
            {
                CampaignLevelState.Current => "Next",
                CampaignLevelState.Completed => "Replay",
                _ => card.IsPlayable ? "Saved" : "Locked"
            },
            Style = GetStyle("ArcadeCaptionText"),
            TextColor = showLoadingPlaceholder
                ? Color.FromArgb("#AFC7E5")
                : isFreshPlayable
                    ? Color.FromArgb("#C7DAF2")
                : card.State == CampaignLevelState.Current ? Color.FromArgb("#FFD89E") : muted
        };

        content.Children.Add(topRow);
        Grid.SetRow(topRow, 0);
        content.Children.Add(title);
        Grid.SetRow(title, 1);
        content.Children.Add(statGrid);
        Grid.SetRow(statGrid, 2);
        content.Children.Add(progressBar);
        Grid.SetRow(progressBar, 3);
        content.Children.Add(stars);
        Grid.SetRow(stars, 4);
        content.Children.Add(status);
        Grid.SetRow(status, 5);

        cardBorder.Content = content;

        if (card.IsPlayable && !showLoadingPlaceholder)
        {
            TapGestureRecognizer tapGesture = new();
            tapGesture.Tapped += async (_, _) => await NavigateToLevelAsync(card.Level);
            cardBorder.GestureRecognizers.Add(tapGesture);
        }

        return cardBorder;
    }

    private Border CreateStatCard(string title, string value, Color accent, Color panel)
    {
        return new Border
        {
            Padding = new Thickness(10, 8),
            Background = new SolidColorBrush(ApplyAlpha(panel, 0.86f)),
            Stroke = new SolidColorBrush(ApplyAlpha(accent, 0.35f)),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        Style = GetStyle("ArcadeCaptionText"),
                        TextColor = ApplyAlpha(accent, 0.88f)
                    },
                    new Label
                    {
                        Text = value,
                        Style = GetStyle("ArcadeActionTitle"),
                        FontSize = 15
                    }
                }
            }
        };
    }

    private View CreateStarStrip(CampaignLevelCard card, Color accent, Color muted, bool showLoadingPlaceholder)
    {
        HorizontalStackLayout stars = new()
        {
            Spacing = 6
        };

        for (int index = 0; index < 3; index++)
        {
            bool isLit = index < card.Stars;
            stars.Children.Add(new Border
            {
                WidthRequest = 12,
                HeightRequest = 12,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(6) },
                Background = new SolidColorBrush(isLit ? accent : ApplyAlpha(Color.FromArgb("#25364F"), 0.95f))
            });
        }

        stars.Children.Add(new Label
        {
            Text = showLoadingPlaceholder ? "--" : $"{card.Stars}/3",
            Style = GetStyle("ArcadeCaptionText"),
            TextColor = muted,
            VerticalTextAlignment = TextAlignment.Center
        });

        return stars;
    }

    private static Brush CreateSectionBackground(CampaignSectionTheme theme)
    {
        Color start = Color.FromArgb(theme.SurfaceStartHex);
        Color end = Color.FromArgb(theme.SurfaceEndHex);
        Color accent = ApplyAlpha(Color.FromArgb(theme.AccentHex), (float)Math.Min(0.2d, 0.05d + theme.OverlayIntensity * 0.18d));

        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(start, 0f),
                new GradientStop(accent, 0.46f),
                new GradientStop(end, 1f)
            },
            new Point(0, 0),
            new Point(1, 1));
    }

    private static Color GetPillColor(CampaignLevelState state, Color accent, bool isPlayable)
    {
        if (state == CampaignLevelState.Current)
        {
            return accent;
        }

        if (state == CampaignLevelState.Completed)
        {
            return ApplyAlpha(accent, 0.28f);
        }

        return isPlayable ? ApplyAlpha(accent, 0.24f) : Color.FromArgb("#1A2740");
    }

    private static Color ApplyAlpha(Color color, float alpha)
    {
        return new Color(color.Red, color.Green, color.Blue, alpha);
    }

    private static void PrimeForEntry(VisualElement element, double translateY, double scale)
    {
        element.Opacity = 0;
        element.TranslationY = translateY;
        element.Scale = scale;
    }

    private async Task AnimateChromeAsync()
    {
        await Task.WhenAll(
            topBarPanel.FadeToAsync(1, 220, Easing.CubicOut),
            topBarPanel.TranslateToAsync(0, 0, 260, Easing.CubicOut),
            topBarPanel.ScaleToAsync(1, 260, Easing.CubicOut));

        await Task.WhenAll(
            heroHeaderPanel.FadeToAsync(1, 240, Easing.CubicOut),
            heroHeaderPanel.TranslateToAsync(0, 0, 280, Easing.CubicOut),
            heroHeaderPanel.ScaleToAsync(1, 280, Easing.CubicOut));

        await Task.WhenAll(
            loadPanel.FadeToAsync(1, 220, Easing.CubicOut),
            loadPanel.TranslateToAsync(0, 0, 260, Easing.CubicOut),
            loadPanel.ScaleToAsync(1, 260, Easing.CubicOut));
    }

    private static async Task AnimateSectionEntryAsync(View sectionView, int index)
    {
        if (sectionView is not VisualElement visual)
        {
            return;
        }

        int delayMs = Math.Min(160, index * 45);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        await Task.WhenAll(
            visual.FadeToAsync(1, 220, Easing.CubicOut),
            visual.TranslateToAsync(0, 0, 280, Easing.CubicOut),
            visual.ScaleToAsync(1, 280, Easing.CubicOut));
    }

    private async Task NavigateToLevelAsync(int levelNumber)
    {
        Dictionary<string, object> navigationParameter = new()
        {
            { "Level", levelNumber }
        };

        await Shell.Current.GoToAsync(nameof(GamePage), true, navigationParameter);
    }

    private async void CampaignScroll_Scrolled(object sender, ScrolledEventArgs e)
    {
        try
        {
            if (isRefreshing || isLoadingSections || isRebuildingSections || isProgressHydrating)
            {
                return;
            }

            if (sectionsHost.Height <= 0d || campaignScroll.Height <= 0d)
            {
                return;
            }

            if (e.ScrollY + campaignScroll.Height >= sectionsHost.Height - ScrollPrefetchThreshold)
            {
                await EnsureSectionsLoadedAsync(loadedSections.Count + SectionBatchSize, animate: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GameSelectionPage] Scroll load failed: {ex}");
            SetLoadingState(isActive: false, "Load failed");
        }
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        await NavigateBackAsync();
    }

    private async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void SettingsButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
