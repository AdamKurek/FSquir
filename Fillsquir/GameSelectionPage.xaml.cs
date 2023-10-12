using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;


namespace Fillsquir;

public partial class GameSelectionPage : ContentPage
{
#if WINDOWS
    double scrolled=0;
#endif
    int levels = 10;
    bool moreData = true;
    public GameSelectionPage()
	{
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();
        ObservableCollection<int> ints = new ObservableCollection<int>()
        {
            1,2,3,4,5,6,7,8,9//,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
        };

        levelsView.ItemsSource = ints;
        //levelsView.ItemTemplate = new DataTemplate(() =>
        //    {

        //        Button levelButton = new Button();
        //        levelButton.SetBinding(Button.TextProperty, new Binding("."));
        //        levelButton.Clicked += LevelButton_Clicked;
        //        if (Application.Current.Resources.TryGetValue("LevelSelect", out var style))
        //        {
        //            levelButton.Style = style as Style;
        //        }
        //        return levelButton;
        //    });
        //levelsView.SizeChanged += AdjustZizese;

        //levelsView.SizeChanged += AdjustZizese;
        levelsView.RemainingItemsThreshold = 100;
        levelsView.RemainingItemsThresholdReached  += async (s, e)  => 
        {
            if (!moreData) return;
            var newItems = await FetchItemsAsync();  // Fetch a small set of items asynchronously
            if (newItems.Count == 0 || newItems is null)
            {
                moreData = false;
                return;
            }
            foreach (var item in newItems)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    ints.Add(item);
                });
            }
        };
#if WINDOWS
        levelsView.Scrolled += (s, e) =>
        {
            var qua = e.LastVisibleItemIndex;
            if (e.VerticalOffset > scrolled)
            {
                scrolled = e.VerticalOffset;
                ints.Add(levels++);
            }
        };
#endif
    }

    private async Task<List<int>> FetchItemsAsync()
    {
        List<int> items = new List<int>();
        for(int i = 0; i < 100;i++ )
        {
            items.Add(levels++);
        }
        return await Task.FromResult(items);
    }


    private void AdjustZizese(object sender, EventArgs e)
    {
        ListGrid.VerticalItemSpacing = levelsView.Width / 5;
        ListGrid.HorizontalItemSpacing = levelsView.Width / 5;
        levelsView.Margin = new Thickness(levelsView.Width / 5, 0);
    }

    private async void LevelButton_Clicked(object sender, EventArgs e)
    {
        Button button = (Button)sender;
        int levelNumber = Int32.Parse(button.Text);
        // Navigate to the selected level
        Routing.RegisterRoute($"//GamePage{levelNumber}", typeof(GamePage));
        var navigationParameter = new Dictionary<string, object>
        {
            { "Level", levelNumber }
        };
        await Shell.Current.GoToAsync($"//GamePage{levelNumber}", true, navigationParameter);
    }
}