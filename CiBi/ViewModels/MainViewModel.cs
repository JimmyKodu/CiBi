using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CiBi.Models;
using ReactiveUI;

namespace CiBi.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private decimal _exchangeRate = 7.0284391534391534391534391534m; // CNY per 1 USD (1062.64 / 151.2)

    public decimal ExchangeRate
    {
        get => _exchangeRate;
        set { this.RaiseAndSetIfChanged(ref _exchangeRate, value); Recompute(); }
    }

    private string _exchangeRateText = "7.0284";
    public string ExchangeRateText
    {
        get => _exchangeRateText;
        set
        {
            this.RaiseAndSetIfChanged(ref _exchangeRateText, value);
            if (decimal.TryParse(value?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) && r > 0)
                ExchangeRate = r;
        }
    }

    private string _displayCurrency = "CNY";
    public string DisplayCurrency
    {
        get => _displayCurrency;
        set { this.RaiseAndSetIfChanged(ref _displayCurrency, value); this.RaisePropertyChanged(nameof(IsCny)); this.RaisePropertyChanged(nameof(IsUsd)); Recompute(); }
    }

    public bool IsCny
    {
        get => DisplayCurrency == "CNY";
        set { if (value) DisplayCurrency = "CNY"; }
    }

    public bool IsUsd
    {
        get => DisplayCurrency == "USD";
        set { if (value) DisplayCurrency = "USD"; }
    }

    // 用量构成滑块：缓存命中占输入的比例（0-100）
    private double _cacheHitRatio = 99d;
    public double CacheHitRatio
    {
        get => _cacheHitRatio;
        set
        {
            this.RaiseAndSetIfChanged(ref _cacheHitRatio, Math.Clamp(value, 0d, 100d));
            this.RaisePropertyChanged(nameof(CacheHitRatioText));
            this.RaisePropertyChanged(nameof(CacheMissRatioText));
            Recompute();
        }
    }

    public string CacheHitRatioText => $"{CacheHitRatio:0}%";
    public string CacheMissRatioText => $"{100d - CacheHitRatio:0}%";

    // 输出 token 占输入的比例（0-100）
    private double _outputRatio = 1d;
    public double OutputRatio
    {
        get => _outputRatio;
        set { this.RaiseAndSetIfChanged(ref _outputRatio, Math.Clamp(value, 0d, 100d)); this.RaisePropertyChanged(nameof(OutputRatioText)); Recompute(); }
    }

    public string OutputRatioText => $"{OutputRatio:0}%";

    public string MixSummary =>
        $"输入 100M = 缓存命中 {CacheHitRatio:0}M · 缓存未命中 {100 - CacheHitRatio:0}M，输出 {OutputRatio:0}M（综合 {100 + OutputRatio:0}M）";

    public ObservableCollection<PlanView> Plans { get; } = new();
    public ObservableCollection<PlanView> Ranked { get; } = new();

    public string ExchangeHint => DisplayCurrency == "CNY"
        ? "所有价格已按汇率折算为人民币（¥）"
        : "所有价格已按汇率折算为美元（$）";

    public MainViewModel()
    {
        foreach (var p in AiPlan.All)
            Plans.Add(new PlanView(p));
        Recompute();
    }

    private void Recompute()
    {
        var symbol = PlanView.CurrencySymbol(DisplayCurrency);
        var hit = CacheHitRatio;
        var miss = 100d - hit;
        var tout = OutputRatio;
        var total = 100d + tout; // 输入 100M + 输出 outRatio M

        foreach (var v in Plans)
        {
            if (v.IsSubscription)
            {
                var price = Convert(v.OriginalPrice, v.Currency, DisplayCurrency, ExchangeRate);
                v.PriceDisplay = $"{symbol}{price:#,##0.##}";
                var perM = v.MonthlyTokensMillions > 0 ? price / v.MonthlyTokensMillions : 0m;
                v.PerMillionValue = perM;
                v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
            }
            else
            {
                // 按量付费：以 100M 输入为基准，按构成比例算综合每 1M 价格（原始币种）
                var hitM = hit;
                var missM = miss;
                var outM = tout;
                var costLocal = (decimal)(hitM * (double)v.CacheHitPrice
                                        + missM * (double)v.CacheMissPrice
                                        + outM * (double)v.OutputPrice);
                var perMLocal = total > 0 ? costLocal / (decimal)total : 0m;
                var perM = Convert(perMLocal, v.Currency, DisplayCurrency, ExchangeRate);

                v.PerMillionValue = perM;
                v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
                v.PriceDisplay = v.PerMillionDisplay;

                // 三档单价折算显示
                v.CacheHitDisplay = $"{symbol}{Convert(v.CacheHitPrice, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.CacheMissDisplay = $"{symbol}{Convert(v.CacheMissPrice, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.OutputPriceDisplay = $"{symbol}{Convert(v.OutputPrice, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
            }
        }

        var ordered = Plans.OrderBy(x => x.PerMillionValue).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Rank = i + 1;
            ordered[i].IsBestValue = i == 0;
        }

        var best = ordered[0].PerMillionValue;
        var worst = ordered[^1].PerMillionValue;
        foreach (var v in Plans)
        {
            var score = worst == best ? 1.0 : (double)((worst - v.PerMillionValue) / (worst - best));
            v.ValueScore = Math.Clamp(score, 0, 1);
        }

        Ranked.Clear();
        foreach (var v in ordered)
            Ranked.Add(v);

        this.RaisePropertyChanged(nameof(ExchangeHint));
        this.RaisePropertyChanged(nameof(MixSummary));
    }

    private static decimal Convert(decimal amount, string from, string to, decimal rate)
    {
        if (from == to) return amount;
        if (from == "USD" && to == "CNY") return amount * rate;
        if (from == "CNY" && to == "USD") return amount / rate;
        return amount;
    }
}

