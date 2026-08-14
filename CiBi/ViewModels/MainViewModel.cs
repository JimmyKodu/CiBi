using System;
using System.Collections.Generic;
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
        set { this.RaiseAndSetIfChanged(ref _exchangeRate, value); Recompute(false); }
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
        set { this.RaiseAndSetIfChanged(ref _displayCurrency, value); this.RaisePropertyChanged(nameof(IsCny)); this.RaisePropertyChanged(nameof(IsUsd)); Recompute(false); }
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
            Recompute(false);
        }
    }

    public string CacheHitRatioText => $"{CacheHitRatio:0}%";
    public string CacheMissRatioText => $"{100d - CacheHitRatio:0}%";

    // 输出 token 占输入的比例（0-100）
    private double _outputRatio = 1d;
    public double OutputRatio
    {
        get => _outputRatio;
        set { this.RaiseAndSetIfChanged(ref _outputRatio, Math.Clamp(value, 0d, 100d)); this.RaisePropertyChanged(nameof(OutputRatioText)); Recompute(false); }
    }

    public string OutputRatioText => $"{OutputRatio:0}%";

    public string MixSummary =>
        $"输入 100M = 缓存命中 {CacheHitRatio:0}M · 缓存未命中 {100 - CacheHitRatio:0}M，输出 {OutputRatio:0}M（综合 {100 + OutputRatio:0}M）";

    public ObservableCollection<PlanView> Plans { get; } = new();
    public ObservableCollection<PlanView> Ranked { get; } = new();
    public ObservableCollection<PlanView> VisiblePlans { get; } = new();

    // 大类筛选（默认全选）
    private readonly HashSet<string> _selRegions = new() { "国际版", "国内版", "按量付费" };
    private readonly HashSet<string> _selVersions = new() { "V2", "V3" };
    private readonly HashSet<string> _selTiers = new() { "Lite", "Pro", "Max" };
    private readonly HashSet<string> _selCycles = new() { "年付", "季付", "月付" };

    private static bool Has(HashSet<string> s, string k) => s.Contains(k);
    private bool Toggle(HashSet<string> s, string k, bool v, string propName)
    {
        if (v == s.Contains(k)) return false;
        if (v) s.Add(k); else s.Remove(k);
        this.RaisePropertyChanged(propName);
        Recompute();
        return true;
    }

    public bool FilterIntl { get => Has(_selRegions, "国际版"); set => Toggle(_selRegions, "国际版", value, nameof(FilterIntl)); }
    public bool FilterCn { get => Has(_selRegions, "国内版"); set => Toggle(_selRegions, "国内版", value, nameof(FilterCn)); }
    public bool FilterPayG { get => Has(_selRegions, "按量付费"); set => Toggle(_selRegions, "按量付费", value, nameof(FilterPayG)); }
    public bool FilterV2 { get => Has(_selVersions, "V2"); set => Toggle(_selVersions, "V2", value, nameof(FilterV2)); }
    public bool FilterV3 { get => Has(_selVersions, "V3"); set => Toggle(_selVersions, "V3", value, nameof(FilterV3)); }
    public bool FilterLite { get => Has(_selTiers, "Lite"); set => Toggle(_selTiers, "Lite", value, nameof(FilterLite)); }
    public bool FilterPro { get => Has(_selTiers, "Pro"); set => Toggle(_selTiers, "Pro", value, nameof(FilterPro)); }
    public bool FilterMax { get => Has(_selTiers, "Max"); set => Toggle(_selTiers, "Max", value, nameof(FilterMax)); }
    public bool FilterYear { get => Has(_selCycles, "年付"); set => Toggle(_selCycles, "年付", value, nameof(FilterYear)); }
    public bool FilterQuarter { get => Has(_selCycles, "季付"); set => Toggle(_selCycles, "季付", value, nameof(FilterQuarter)); }
    public bool FilterMonth { get => Has(_selCycles, "月付"); set => Toggle(_selCycles, "月付", value, nameof(FilterMonth)); }

    private bool IsVisible(AiPlan p) =>
        _selRegions.Contains(p.Region) &&
        // 版本筛选只针对订阅制；DeepSeek 的 V4 是模型版本，与 GLM 计费规则版本(V2/V3)含义不同
        (p.Type == PlanType.PayAsYouGo || _selVersions.Contains(p.Version)) &&
        // 档次筛选只针对订阅制；DeepSeek 的 Flash/Pro 与 GLM 套餐档次无关，不参与档次筛选
        (p.Type == PlanType.PayAsYouGo || _selTiers.Contains(p.Tier)) &&
        (string.IsNullOrEmpty(p.BillingCycle) || _selCycles.Contains(p.BillingCycle));

    public string ExchangeHint => DisplayCurrency == "CNY"
        ? "所有价格已按汇率折算为人民币（¥）"
        : "所有价格已按汇率折算为美元（$）";

    public MainViewModel()
    {
        foreach (var p in AiPlan.All)
            Plans.Add(new PlanView(p));
        Recompute();
    }

    private void Recompute() => Recompute(true);

    // rebuildLists=true: 可见项变化(筛选)时重建排行/详情集合；false: 滑块/汇率/币种变化时仅 Move 重排，避免容器重建卡顿
    private void Recompute(bool rebuildLists)
    {
        UpdateMetrics();
        if (rebuildLists)
        {
            var visible = Plans.Where(p => IsVisible(p.Plan)).ToList();
            RebuildCollection(Ranked, visible.OrderBy(x => x.PerMillionValue));
            RebuildCollection(VisiblePlans, visible);
        }
        else
        {
            ReorderRanked();
        }
        ApplyRankProperties();
        this.RaisePropertyChanged(nameof(ExchangeHint));
        this.RaisePropertyChanged(nameof(MixSummary));
    }

    private void UpdateMetrics()
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
                var costLocal = (decimal)(hit * (double)v.CacheHitPrice + miss * (double)v.CacheMissPrice + tout * (double)v.OutputPrice);
                var perMLocal = total > 0 ? costLocal / (decimal)total : 0m;
                var perM = Convert(perMLocal, v.Currency, DisplayCurrency, ExchangeRate);
                v.PerMillionValue = perM;
                v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
                v.PriceDisplay = v.PerMillionDisplay;
                v.CacheHitDisplay = $"{symbol}{Convert(v.CacheHitPrice, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.CacheMissDisplay = $"{symbol}{Convert(v.CacheMissPrice, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.OutputPriceDisplay = $"{symbol}{Convert(v.OutputPrice, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
            }
        }
    }

    // 用 Move 把各项移到排序后的位置，复用现有容器(不触发 DataTemplate 重建)
    private void ReorderRanked()
    {
        var ordered = Ranked.OrderBy(x => x.PerMillionValue).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var cur = Ranked.IndexOf(ordered[i]);
            if (cur != i) Ranked.Move(cur, i);
        }
    }

    private void ApplyRankProperties()
    {
        var ordered = Ranked.ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Rank = i + 1;
            ordered[i].IsBestValue = i == 0;
        }
        if (ordered.Count > 0)
        {
            var best = ordered[0].PerMillionValue;
            var worst = ordered[^1].PerMillionValue;
            foreach (var v in ordered)
            {
                var score = worst == best ? 1.0 : (double)((worst - v.PerMillionValue) / (worst - best));
                v.ValueScore = Math.Clamp(score, 0, 1);
            }
        }
    }

    private static void RebuildCollection(ObservableCollection<PlanView> col, IEnumerable<PlanView> items)
    {
        col.Clear();
        foreach (var v in items) col.Add(v);
    }

    private static decimal Convert(decimal amount, string from, string to, decimal rate)
    {
        if (from == to) return amount;
        if (from == "USD" && to == "CNY") return amount * rate;
        if (from == "CNY" && to == "USD") return amount / rate;
        return amount;
    }
}

