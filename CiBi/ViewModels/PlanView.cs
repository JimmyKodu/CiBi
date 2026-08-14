using System.Globalization;
using Avalonia.Media;
using CiBi.Models;
using ReactiveUI;

namespace CiBi.ViewModels;

public sealed class PlanView : ReactiveObject
{
    public AiPlan Plan { get; }

    public string ShortName => Plan.ShortName;
    public string FullName => Plan.FullName;
    public string Tier => Plan.Tier;
    public string Version => Plan.Version;
    public string Region => Plan.Region;
    public string Currency => Plan.Currency;
    public PlanType Type => Plan.Type;
    public bool IsPayAsYouGo => Plan.Type == PlanType.PayAsYouGo;
    public bool IsSubscription => Plan.Type == PlanType.Subscription;
    public int TokenMultiplier => Plan.TokenMultiplier;
    public decimal WeeklyTokensMillions => Plan.WeeklyTokensMillions;
    public decimal MonthlyTokensMillions => Plan.MonthlyTokensMillions;
    public decimal OriginalPrice => Plan.PriceMonthly;

    // 按量付费单价（原始币种）
    public decimal CacheHitPrice => Plan.CacheHitPrice;
    public decimal CacheMissPrice => Plan.CacheMissPrice;
    public decimal OutputPrice => Plan.OutputPrice;

    public string OriginalPriceText => IsPayAsYouGo ? "—" : $"{CurrencySymbol(Plan.Currency)}{Plan.PriceMonthly:0.##}";
    public string WeeklyTokensText => IsPayAsYouGo ? "不限" : $"{Plan.WeeklyTokensMillions:#,##0.0} M";
    public string MonthlyTokensText => IsPayAsYouGo ? "不限" : $"{Plan.MonthlyTokensMillions:#,##0.0} M";
    public string Subtitle => IsPayAsYouGo
        ? $"{Plan.Version} · 按量付费 · 缓存命中/未命中/输出 单价"
        : $"{Plan.Version} · {Plan.Region} · {WeeklyTokensText}/周 ×{TokenMultiplier}{(string.IsNullOrEmpty(Plan.BillingCycle) ? "" : " · " + Plan.BillingCycle)}";

    // 缓存为静态 Brush，避免每次绑定求值都 new SolidColorBrush + Color.Parse（GC 压力）
    private static readonly IBrush TierBrushPayg = new SolidColorBrush(Color.Parse("#10B981"));
    private static readonly IBrush TierBrushLite = new SolidColorBrush(Color.Parse("#6B7280"));
    private static readonly IBrush TierBrushPro = new SolidColorBrush(Color.Parse("#0EA5E9"));
    private static readonly IBrush TierBrushMax = new SolidColorBrush(Color.Parse("#1428A0"));
    // 按量付费(DeepSeek)统一绿色系，与 GLM 订阅制档次视觉区分
    public IBrush TierBrush => IsPayAsYouGo
        ? TierBrushPayg
        : Tier switch { "Lite" => TierBrushLite, "Pro" => TierBrushPro, _ => TierBrushMax };

    private string _priceDisplay = "";
    public string PriceDisplay { get => _priceDisplay; set => this.RaiseAndSetIfChanged(ref _priceDisplay, value); }

    private string _perMillionDisplay = "";
    public string PerMillionDisplay { get => _perMillionDisplay; set => this.RaiseAndSetIfChanged(ref _perMillionDisplay, value); }

    private decimal _perMillionValue;
    public decimal PerMillionValue { get => _perMillionValue; set => this.RaiseAndSetIfChanged(ref _perMillionValue, value); }

    // 按量付费：折算到显示币种后的三档单价文本
    private string _cacheHitDisplay = "";
    public string CacheHitDisplay { get => _cacheHitDisplay; set => this.RaiseAndSetIfChanged(ref _cacheHitDisplay, value); }
    private string _cacheMissDisplay = "";
    public string CacheMissDisplay { get => _cacheMissDisplay; set => this.RaiseAndSetIfChanged(ref _cacheMissDisplay, value); }
    private string _outputPriceDisplay = "";
    public string OutputPriceDisplay { get => _outputPriceDisplay; set => this.RaiseAndSetIfChanged(ref _outputPriceDisplay, value); }

    private int _rank;
    public int Rank
    {
        get => _rank;
        set { this.RaiseAndSetIfChanged(ref _rank, value); this.RaisePropertyChanged(nameof(RankText)); }
    }

    public string RankText => $"#{Rank}";

    private bool _isBestValue;
    public bool IsBestValue
    {
        get => _isBestValue;
        set { this.RaiseAndSetIfChanged(ref _isBestValue, value); this.RaisePropertyChanged(nameof(ValueLabel)); }
    }

    private double _valueScore;
    public double ValueScore
    {
        get => _valueScore;
        set { this.RaiseAndSetIfChanged(ref _valueScore, value); this.RaisePropertyChanged(nameof(ValueLabel)); }
    }

    public string ValueLabel => IsBestValue ? "最优性价比" : $"性价比 {ValueScore * 100:0}%";

    public PlanView(AiPlan plan) => Plan = plan;

    public static string CurrencySymbol(string c) => c switch { "CNY" => "¥", "USD" => "$", _ => "" };
}
