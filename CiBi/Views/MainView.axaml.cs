using Avalonia.Controls;
using CiBi.ViewModels;

namespace CiBi.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        // 比例条轨道宽度变化时回传 VM，换算三段像素宽
        MixTrack.SizeChanged += (_, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.MixTrackWidth = e.NewSize.Width;
        };
    }
}
