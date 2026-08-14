using System.Runtime.Versioning;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using ReactiveUI.Avalonia;

using CiBi;

internal sealed partial class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
            .UseReactiveUI(_ => { })
            .With(new FontManagerOptions
            {
                // Inter has no CJK glyphs; use the bundled Noto Sans SC subset as the app default so Chinese renders in the WASM canvas.
                DefaultFamilyName = "avares://CiBi/Assets/Fonts/NotoSansSC.ttf#Noto Sans SC"
            })
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}

