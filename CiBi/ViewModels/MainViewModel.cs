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
            UpdateMixBar();
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
        set { this.RaiseAndSetIfChanged(ref _outputRatio, Math.Clamp(value, 0d, 100d)); this.RaisePropertyChanged(nameof(OutputRatioText)); UpdateMixBar(); Recompute(false); }
    }

    public string OutputRatioText => $"{OutputRatio:0}%";

    // 高峰时段占比（0-100，默认 100=全高峰）；DeepSeek V4 分空闲/高峰两档单价，按此比例线性加权
    private double _peakRatio = 100d;
    public double PeakRatio
    {
        get => _peakRatio;
        set { this.RaiseAndSetIfChanged(ref _peakRatio, Math.Clamp(value, 0d, 100d)); this.RaisePropertyChanged(nameof(PeakRatioText)); Recompute(false); }
    }

    public string PeakRatioText => $"{PeakRatio:0}%";

    // 比例条三段像素宽：视图回填轨道宽度后按占比换算（段间各留 4px 间隙）
    private double _mixTrackWidth = 320d;
    public double MixTrackWidth
    {
        get => _mixTrackWidth;
        set
        {
            if (Math.Abs(_mixTrackWidth - value) < 0.5) return;
            _mixTrackWidth = value;
            UpdateMixBar();
        }
    }

    private double _hitPx;
    public double HitPx { get => _hitPx; private set => this.RaiseAndSetIfChanged(ref _hitPx, value); }

    private double _missPx;
    public double MissPx { get => _missPx; private set => this.RaiseAndSetIfChanged(ref _missPx, value); }

    private double _outPx;
    public double OutPx { get => _outPx; private set => this.RaiseAndSetIfChanged(ref _outPx, value); }

    private void UpdateMixBar()
    {
        var total = 100d + OutputRatio; // 命中+未命中=100，输出额外
        var usable = Math.Max(0d, _mixTrackWidth - 8d);
        HitPx = usable * CacheHitRatio / total;
        MissPx = usable * (100d - CacheHitRatio) / total;
        OutPx = usable * OutputRatio / total;
    }

    public string MixSummary =>
        $"输入 100M = 缓存命中 {CacheHitRatio:0}M · 缓存未命中 {100 - CacheHitRatio:0}M，输出 {OutputRatio:0}M（综合 {100 + OutputRatio:0}M）；高峰时段占比 {PeakRatio:0}% · 空闲 {100 - PeakRatio:0}%";

    public ObservableCollection<PlanView> Plans { get; } = new();
    public ObservableCollection<PlanView> Ranked { get; } = new();
    public ObservableCollection<PlanView> VisiblePlans { get; } = new();

    // 品牌筛选（默认全选；版本 V3、档次全选、周期 月付）
    private readonly HashSet<string> _selBrands = new() { "GLM", "DeepSeek", "Qwen", "Kimi", "MiniMax" };
    private readonly HashSet<string> _selVersions = new() { "V3" };
    // 档次按品牌分组（键 = 品牌:档次）：GLM=Lite/Pro/Max，MiniMax=Plus/Max/Ultra，Kimi=Moderato
    private readonly HashSet<string> _selTiers = new() { "GLM:Lite", "GLM:Pro", "GLM:Max", "MiniMax:Plus", "MiniMax:Max", "MiniMax:Ultra", "Kimi:Moderato" };
    private readonly HashSet<string> _selCycles = new() { "月付" };

    private static bool Has(HashSet<string> s, string k) => s.Contains(k);
    private bool Toggle(HashSet<string> s, string k, bool v, string propName)
    {
        if (v == s.Contains(k)) return false;
        if (v) s.Add(k); else s.Remove(k);
        this.RaisePropertyChanged(propName);
        Recompute();
        return true;
    }

    public bool FilterGlm { get => Has(_selBrands, "GLM"); set => Toggle(_selBrands, "GLM", value, nameof(FilterGlm)); }
    public bool FilterDeepSeek { get => Has(_selBrands, "DeepSeek"); set => Toggle(_selBrands, "DeepSeek", value, nameof(FilterDeepSeek)); }
    public bool FilterQwen { get => Has(_selBrands, "Qwen"); set => Toggle(_selBrands, "Qwen", value, nameof(FilterQwen)); }
    public bool FilterKimi { get => Has(_selBrands, "Kimi"); set => Toggle(_selBrands, "Kimi", value, nameof(FilterKimi)); }
    public bool FilterMiniMax { get => Has(_selBrands, "MiniMax"); set => Toggle(_selBrands, "MiniMax", value, nameof(FilterMiniMax)); }
    public bool FilterV2 { get => Has(_selVersions, "V2"); set => Toggle(_selVersions, "V2", value, nameof(FilterV2)); }
    public bool FilterV3 { get => Has(_selVersions, "V3"); set => Toggle(_selVersions, "V3", value, nameof(FilterV3)); }
    public bool FilterTierGlmLite { get => Has(_selTiers, "GLM:Lite"); set => Toggle(_selTiers, "GLM:Lite", value, nameof(FilterTierGlmLite)); }
    public bool FilterTierGlmPro { get => Has(_selTiers, "GLM:Pro"); set => Toggle(_selTiers, "GLM:Pro", value, nameof(FilterTierGlmPro)); }
    public bool FilterTierGlmMax { get => Has(_selTiers, "GLM:Max"); set => Toggle(_selTiers, "GLM:Max", value, nameof(FilterTierGlmMax)); }
    public bool FilterTierMmPlus { get => Has(_selTiers, "MiniMax:Plus"); set => Toggle(_selTiers, "MiniMax:Plus", value, nameof(FilterTierMmPlus)); }
    public bool FilterTierMmMax { get => Has(_selTiers, "MiniMax:Max"); set => Toggle(_selTiers, "MiniMax:Max", value, nameof(FilterTierMmMax)); }
    public bool FilterTierMmUltra { get => Has(_selTiers, "MiniMax:Ultra"); set => Toggle(_selTiers, "MiniMax:Ultra", value, nameof(FilterTierMmUltra)); }
    public bool FilterTierKimiModerato { get => Has(_selTiers, "Kimi:Moderato"); set => Toggle(_selTiers, "Kimi:Moderato", value, nameof(FilterTierKimiModerato)); }
    public bool FilterYear { get => Has(_selCycles, "年付"); set => Toggle(_selCycles, "年付", value, nameof(FilterYear)); }
    public bool FilterQuarter { get => Has(_selCycles, "季付"); set => Toggle(_selCycles, "季付", value, nameof(FilterQuarter)); }
    public bool FilterMonth { get => Has(_selCycles, "月付"); set => Toggle(_selCycles, "月付", value, nameof(FilterMonth)); }

    private bool IsVisible(AiPlan p) =>
        _selBrands.Contains(p.Brand) &&
        // 按量付费无版本/档次概念；订阅制按"品牌:档次"筛选（GLM 与 MiniMax 的 Max 各自独立），版本筛选(V2/V3)只针对 GLM
        (p.Type == PlanType.PayAsYouGo ||
            (_selTiers.Contains(p.Brand + ":" + p.Tier) && (p.Brand != "GLM" || _selVersions.Contains(p.Version)))) &&
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
            // 差异同步：只增删变化的项，复用共同的容器(避免全部重建卡顿)
            SyncCollection(Ranked, visible.OrderBy(x => x.PerMillionValue).ToList());
            SyncCollection(VisiblePlans, visible);
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
                // 按量付费：以 100M 输入为基准，按构成比例算综合每 1M 价格（原始币种）；
                // 单价按高峰时段占比在空闲/高峰两档间线性加权
                var pk = (decimal)(PeakRatio / 100d);
                decimal Blend(decimal offPeak, decimal peak) => offPeak + (peak - offPeak) * pk;
                var hitP = Blend(v.CacheHitPrice, v.CacheHitPricePeak);
                var missP = Blend(v.CacheMissPrice, v.CacheMissPricePeak);
                var outP = Blend(v.OutputPrice, v.OutputPricePeak);
                var costLocal = (decimal)(hit * (double)hitP + miss * (double)missP + tout * (double)outP);
                var perMLocal = total > 0 ? costLocal / (decimal)total : 0m;
                var perM = Convert(perMLocal, v.Currency, DisplayCurrency, ExchangeRate);
                v.PerMillionValue = perM;
                v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
                v.PriceDisplay = v.PerMillionDisplay;
                v.CacheHitDisplay = $"{symbol}{Convert(hitP, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.CacheMissDisplay = $"{symbol}{Convert(missP, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.OutputPriceDisplay = $"{symbol}{Convert(outP, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
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

    // 差异同步：只增删变化的项并调整顺序，复用未变项的容器，避免 Clear+Add 全部重建
    private static void SyncCollection(ObservableCollection<PlanView> col, IReadOnlyList<PlanView> target)
    {
        for (var i = col.Count - 1; i >= 0; i--)
            if (!target.Contains(col[i])) col.RemoveAt(i);
        for (var i = 0; i < target.Count; i++)
        {
            var cur = col.IndexOf(target[i]);
            if (cur == -1) col.Insert(i, target[i]);
            else if (cur != i) col.Move(cur, i);
        }
    }

    private static decimal Convert(decimal amount, string from, string to, decimal rate)
    {
        if (from == to) return amount;
        if (from == "USD" && to == "CNY") return amount * rate;
        if (from == "CNY" && to == "USD") return amount / rate;
        return amount;
    }
}

