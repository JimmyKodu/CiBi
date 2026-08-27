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
    private double _cacheHitRatio = 98d;
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

    // 使用时段（小时粒度，北京时间）：按各厂商高峰窗口自动折算高峰占比，替代手拖峰谷比例
    // DeepSeek 高峰 = 工作日 9:00-12:00、14:00-18:00（周末不涨价）；GLM Coding Plan 高峰 = 每周一至周五 14:00-18:00（消耗 V2×3 / V3×2）
    public IReadOnlyList<string> HourOptions { get; } = Enumerable.Range(0, 25).Select(h => $"{h:00}:00").ToList();

    private string _useStart = "09:00";
    public string UseStart
    {
        get => _useStart;
        set { if (string.IsNullOrEmpty(value) || value == _useStart) return; this.RaiseAndSetIfChanged(ref _useStart, value); Recompute(false); }
    }

    private string _useEnd = "18:00";
    public string UseEnd
    {
        get => _useEnd;
        set { if (string.IsNullOrEmpty(value) || value == _useEnd) return; this.RaiseAndSetIfChanged(ref _useEnd, value); Recompute(false); }
    }

    // 午休：勾选后从使用时段中扣除 [LunchStart, LunchEnd)，再与各厂商高峰窗口求重叠
    private bool _hasLunch = true;
    public bool HasLunch
    {
        get => _hasLunch;
        set { this.RaiseAndSetIfChanged(ref _hasLunch, value); Recompute(false); }
    }

    private string _lunchStart = "12:00";
    public string LunchStart
    {
        get => _lunchStart;
        set { if (string.IsNullOrEmpty(value) || value == _lunchStart) return; this.RaiseAndSetIfChanged(ref _lunchStart, value); Recompute(false); }
    }

    private string _lunchEnd = "14:00";
    public string LunchEnd
    {
        get => _lunchEnd;
        set { if (string.IsNullOrEmpty(value) || value == _lunchEnd) return; this.RaiseAndSetIfChanged(ref _lunchEnd, value); Recompute(false); }
    }

    public string UsageSummary => HasLunch
        ? $"使用时段 {UseStart}~{UseEnd}（扣除午休 {LunchStart}~{LunchEnd}）"
        : $"使用时段 {UseStart}~{UseEnd}";

    // 所选时段落在各厂商高峰窗口内的占比（0-1）；高峰仅周一至周五，按每周作息的天数权重占比折算
    public double DeepSeekPeakShare { get; private set; }
    public double GlmPeakShare { get; private set; }
    public string DeepSeekPeakText => $"{DeepSeekPeakShare * 100:0}%";
    public string GlmPeakText => $"{GlmPeakShare * 100:0}%";

    // 周一..周日每日使用权重；大小周模式周六 = 0.5（隔周使用），未勾选 = 0；默认大小周
    private readonly double[] _dayWeights = [1, 1, 1, 1, 1, 0.5, 0];
    private string _workPattern = "大小周";

    public double DaysPerWeek => _dayWeights.Sum();
    public string DaysPerWeekText => $"每周用 {DaysPerWeek:0.#} 天";

    public bool PatDouble { get => _workPattern == "双休"; set { if (value) ApplyPattern("双休"); } }
    public bool PatSingle { get => _workPattern == "单休"; set { if (value) ApplyPattern("单休"); } }
    public bool PatAlt { get => _workPattern == "大小周"; set { if (value) ApplyPattern("大小周"); } }

    public bool UseMon { get => _dayWeights[0] > 0; set => SetDay(0, value); }
    public bool UseTue { get => _dayWeights[1] > 0; set => SetDay(1, value); }
    public bool UseWed { get => _dayWeights[2] > 0; set => SetDay(2, value); }
    public bool UseThu { get => _dayWeights[3] > 0; set => SetDay(3, value); }
    public bool UseFri { get => _dayWeights[4] > 0; set => SetDay(4, value); }
    public bool UseSat { get => _dayWeights[5] > 0; set => SetDay(5, value); }
    public bool UseSun { get => _dayWeights[6] > 0; set => SetDay(6, value); }

    // 预设一键切换；手动勾选/取消任一天则转为"自定义"（三个预设单选全部取消选中）
    private void ApplyPattern(string p)
    {
        if (p == _workPattern) return;
        _workPattern = p;
        switch (p)
        {
            case "双休": SetWeights(1, 1, 1, 1, 1, 0, 0); break;
            case "单休": SetWeights(1, 1, 1, 1, 1, 1, 0); break;
            case "大小周": SetWeights(1, 1, 1, 1, 1, 0.5, 0); break;
        }
        RaiseDayProps();
        Recompute(false);
    }

    private void SetDay(int i, bool on)
    {
        if ((_dayWeights[i] > 0) == on) return;
        _dayWeights[i] = on ? 1d : 0d;
        _workPattern = "自定义";
        RaiseDayProps();
        Recompute(false);
    }

    private void SetWeights(params double[] w)
    {
        for (var i = 0; i < w.Length && i < _dayWeights.Length; i++) _dayWeights[i] = w[i];
    }

    private void RaiseDayProps()
    {
        foreach (var n in new[]
        {
            nameof(UseMon), nameof(UseTue), nameof(UseWed), nameof(UseThu), nameof(UseFri), nameof(UseSat), nameof(UseSun),
            nameof(PatDouble), nameof(PatSingle), nameof(PatAlt), nameof(DaysPerWeekText)
        })
            this.RaisePropertyChanged(n);
    }

    private static int ParseHour(string t) => int.TryParse(t?[..2], out var h) ? h : 0;

    // 按小时粒度标记每天的实际使用时段（支持跨零点与扣除午休），再统计与各高峰窗口的重叠占比
    private void UpdatePeakShares()
    {
        var s = ParseHour(UseStart);
        var e = ParseHour(UseEnd);
        var ls = ParseHour(LunchStart);
        var le = ParseHour(LunchEnd);
        double len = 0, ds = 0, glm = 0;
        for (var h = 0; h < 24; h++)
        {
            var inWin = s <= e ? h >= s && h < e : h >= s || h < e;
            if (!inWin) continue;
            if (HasLunch && (ls <= le ? h >= ls && h < le : h >= ls || h < le)) continue;
            len++;
            if ((h >= 9 && h < 12) || (h >= 14 && h < 18)) ds++;
            if (h >= 14 && h < 18) glm++;
        }
        var total = _dayWeights.Sum();
        if (len == 0 || total == 0)
        {
            DeepSeekPeakShare = GlmPeakShare = 0;
            return;
        }
        // DeepSeek / GLM 高峰均仅周一至周五：按使用天数权重中工作日占比折算（大小周周六 0.5 天不计入高峰）
        var weekday = _dayWeights.Take(5).Sum();
        DeepSeekPeakShare = ds / len * (weekday / total);
        GlmPeakShare = glm / len * (weekday / total);
    }

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
        $"输入 100M = 缓存命中 {CacheHitRatio:0}M · 缓存未命中 {100 - CacheHitRatio:0}M，输出 {OutputRatio:0}M（综合 {100 + OutputRatio:0}M）；{DaysPerWeekText} · {UsageSummary} → DeepSeek 高峰 {DeepSeekPeakText}（工作日 9-12、14-18，周末不涨价）· GLM 高峰 {GlmPeakText}（工作日 14-18，消耗 V2×3 / V3×2）";

    public ObservableCollection<PlanView> Plans { get; } = new();
    public ObservableCollection<PlanView> Ranked { get; } = new();
    public ObservableCollection<PlanView> VisiblePlans { get; } = new();

    // 品牌筛选（默认全选；版本仅 V3，档次各品牌仅保留单一推荐档，订阅制默认国内版 + 年付）
    private readonly HashSet<string> _selBrands = new() { "GLM", "DeepSeek", "Qwen", "Kimi", "MiniMax", "OpenAI" };
    private readonly HashSet<string> _selVersions = new() { "V3" };
    // 档次按品牌分组（键 = 品牌:档次）：GLM=Lite/Pro/Max，Qwen=Lite/Standard/Pro，MiniMax=Plus/Max/Ultra，Kimi=Moderato/Allegretto/Allegro；
    // 按量付费按上下文分档的品牌（MiniMax-M3 / GPT-5.6 Sol）默认仅勾一档，避免占两个排名位
    private readonly HashSet<string> _selTiers = new() { "GLM:Max", "Qwen:Pro", "Kimi:Allegretto", "MiniMax:Max", "MiniMax:M3 ≤512K", "OpenAI:Sol ≤272K", "OpenAI:Sol >272K", "OpenAI:Plus" };
    private readonly HashSet<string> _selCycles = new() { "年付" };
    // 按量付费且按上下文分档定价的品牌：各档参与档次筛选（其余品牌按量付费不受档次/地区/周期筛选）
    private static readonly HashSet<string> TieredPaygBrands = new() { "MiniMax", "OpenAI" };
    // 地区筛选（只作用于订阅制；按量付费不参与，GPT-5.6 为 USD 计价）
    private readonly HashSet<string> _selRegions = new() { "国内版" };

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
    public bool FilterOpenAI { get => Has(_selBrands, "OpenAI"); set => Toggle(_selBrands, "OpenAI", value, nameof(FilterOpenAI)); }
    public bool FilterV2 { get => Has(_selVersions, "V2"); set => Toggle(_selVersions, "V2", value, nameof(FilterV2)); }
    public bool FilterV3 { get => Has(_selVersions, "V3"); set => Toggle(_selVersions, "V3", value, nameof(FilterV3)); }
    public bool FilterTierGlmLite { get => Has(_selTiers, "GLM:Lite"); set => Toggle(_selTiers, "GLM:Lite", value, nameof(FilterTierGlmLite)); }
    public bool FilterTierGlmPro { get => Has(_selTiers, "GLM:Pro"); set => Toggle(_selTiers, "GLM:Pro", value, nameof(FilterTierGlmPro)); }
    public bool FilterTierGlmMax { get => Has(_selTiers, "GLM:Max"); set => Toggle(_selTiers, "GLM:Max", value, nameof(FilterTierGlmMax)); }
    public bool FilterTierQwenLite { get => Has(_selTiers, "Qwen:Lite"); set => Toggle(_selTiers, "Qwen:Lite", value, nameof(FilterTierQwenLite)); }
    public bool FilterTierQwenStandard { get => Has(_selTiers, "Qwen:Standard"); set => Toggle(_selTiers, "Qwen:Standard", value, nameof(FilterTierQwenStandard)); }
    public bool FilterTierQwenPro { get => Has(_selTiers, "Qwen:Pro"); set => Toggle(_selTiers, "Qwen:Pro", value, nameof(FilterTierQwenPro)); }
    public bool FilterTierMmPlus { get => Has(_selTiers, "MiniMax:Plus"); set => Toggle(_selTiers, "MiniMax:Plus", value, nameof(FilterTierMmPlus)); }
    public bool FilterTierMmMax { get => Has(_selTiers, "MiniMax:Max"); set => Toggle(_selTiers, "MiniMax:Max", value, nameof(FilterTierMmMax)); }
    public bool FilterTierMmUltra { get => Has(_selTiers, "MiniMax:Ultra"); set => Toggle(_selTiers, "MiniMax:Ultra", value, nameof(FilterTierMmUltra)); }
    public bool FilterTierMmM3Low { get => Has(_selTiers, "MiniMax:M3 ≤512K"); set => Toggle(_selTiers, "MiniMax:M3 ≤512K", value, nameof(FilterTierMmM3Low)); }
    public bool FilterTierMmM3High { get => Has(_selTiers, "MiniMax:M3 1M"); set => Toggle(_selTiers, "MiniMax:M3 1M", value, nameof(FilterTierMmM3High)); }
    public bool FilterTierGptSolLow { get => Has(_selTiers, "OpenAI:Sol ≤272K"); set => Toggle(_selTiers, "OpenAI:Sol ≤272K", value, nameof(FilterTierGptSolLow)); }
    public bool FilterTierGptSolHigh { get => Has(_selTiers, "OpenAI:Sol >272K"); set => Toggle(_selTiers, "OpenAI:Sol >272K", value, nameof(FilterTierGptSolHigh)); }
    public bool FilterTierGptPlus { get => Has(_selTiers, "OpenAI:Plus"); set => Toggle(_selTiers, "OpenAI:Plus", value, nameof(FilterTierGptPlus)); }
    public bool FilterTierKimiModerato { get => Has(_selTiers, "Kimi:Moderato"); set => Toggle(_selTiers, "Kimi:Moderato", value, nameof(FilterTierKimiModerato)); }
    public bool FilterTierKimiAllegretto { get => Has(_selTiers, "Kimi:Allegretto"); set => Toggle(_selTiers, "Kimi:Allegretto", value, nameof(FilterTierKimiAllegretto)); }
    public bool FilterTierKimiAllegro { get => Has(_selTiers, "Kimi:Allegro"); set => Toggle(_selTiers, "Kimi:Allegro", value, nameof(FilterTierKimiAllegro)); }
    public bool FilterYear { get => Has(_selCycles, "年付"); set => Toggle(_selCycles, "年付", value, nameof(FilterYear)); }
    public bool FilterQuarter { get => Has(_selCycles, "季付"); set => Toggle(_selCycles, "季付", value, nameof(FilterQuarter)); }
    public bool FilterMonth { get => Has(_selCycles, "月付"); set => Toggle(_selCycles, "月付", value, nameof(FilterMonth)); }
    public bool FilterIntl { get => Has(_selRegions, "国际版"); set => Toggle(_selRegions, "国际版", value, nameof(FilterIntl)); }
    public bool FilterCn { get => Has(_selRegions, "国内版"); set => Toggle(_selRegions, "国内版", value, nameof(FilterCn)); }

    private bool IsVisible(AiPlan p) =>
        _selBrands.Contains(p.Brand) &&
        // 按量付费无版本/档次概念（上下文分档品牌除外：只放行勾选档）；订阅制按"品牌:档次"筛选，版本筛选(V2/V3)只针对 GLM
        (p.Type == PlanType.PayAsYouGo
            ? (!TieredPaygBrands.Contains(p.Brand) || _selTiers.Contains(p.Brand + ":" + p.Tier))
            : (_selTiers.Contains(p.Brand + ":" + p.Tier) && (p.Brand != "GLM" || _selVersions.Contains(p.Version)))) &&
        // OpenAI 订阅无国内版，不参与地区筛选
        (p.Type == PlanType.PayAsYouGo || p.Brand == "OpenAI" || _selRegions.Contains(p.Region)) &&
        (string.IsNullOrEmpty(p.BillingCycle) || _selCycles.Contains(p.BillingCycle));

    public string ExchangeHint => DisplayCurrency == "CNY"
        ? "所有价格已按汇率折算为人民币（¥）"
        : "所有价格已按汇率折算为美元（$）";

    // 按量付费参与计价的模型数（随筛选变化）
    public string PaygCountText => $"{VisiblePlans.Count(v => v.IsPayAsYouGo)} 款模型";

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
        UpdatePeakShares();
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
        this.RaisePropertyChanged(nameof(PaygCountText));
        this.RaisePropertyChanged(nameof(MixSummary));
    }

    private void UpdateMetrics()
    {
        var symbol = PlanView.CurrencySymbol(DisplayCurrency);
        foreach (var v in Plans)
        {
            if (v.IsSubscription)
            {
                var price = Convert(v.OriginalPrice, v.Currency, DisplayCurrency, ExchangeRate);
                v.PriceDisplay = $"{symbol}{price:#,##0.##}";
                if (v.IsAnchored)
                {
                    // 无官方配额（ChatGPT Plus）：每 1M 价格 = 锚定按量套餐综合单价 × 比例，随构成滑块联动；
                    // 锚定对象取同品牌同版本按量套餐中当前勾选的档位，多档同勾时取各档综合单价的平均（平衡而非极端），均未勾选回退默认档
                    var anchorDefault = AiPlan.All.First(p => p.Id == v.Plan.PaygAnchorId);
                    var visible = AiPlan.All.Where(p => p.Type == PlanType.PayAsYouGo
                        && p.Brand == anchorDefault.Brand && p.Version == anchorDefault.Version && IsVisible(p)).ToList();
                    if (visible.Count == 0) visible.Add(anchorDefault);
                    var perMLocal = visible.Average(PaygPerMLocal);
                    var perM = Convert(perMLocal * v.Plan.PaygAnchorFraction, anchorDefault.Currency, DisplayCurrency, ExchangeRate);
                    v.PerMillionValue = perM;
                    v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
                    v.PeakWeightText = "";
                }
                else
                {
                    // GLM Coding Plan：工作日 14:00-18:00 高峰消耗按 V2×3 / V3×2 计入配额，按使用时段高峰占比折算有效单价；
                    // 按模型消耗速率（GLM-5.3 = 1x / GLM-5.3-Flash 闲时 0.4x）同乘
                    var weight = 1m + (v.Plan.PeakMultiplier - 1m) * (decimal)GlmPeakShare;
                    var eff = v.Plan.QuotaBurnRate * weight;
                    var perM = v.MonthlyTokensMillions > 0 ? price / v.MonthlyTokensMillions * eff : 0m;
                    v.PerMillionValue = perM;
                    v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
                    v.PeakWeightText = Math.Abs(eff - 1m) > 0.0001m ? $"×{eff:0.00}" : "";
                }
            }
            else
            {
                // 按量付费：以 100M 输入为基准，按构成比例算综合每 1M 价格（原始币种）；
                // DeepSeek 单价在空闲/高峰（9-12、14-18，高峰=空闲×2）两档间按时段重叠占比线性加权；其余模型不分时段
                var pk = (decimal)(v.Plan.Brand == "DeepSeek" ? DeepSeekPeakShare : 0d);
                decimal Blend(decimal offPeak, decimal peak) => offPeak + (peak - offPeak) * pk;
                var hitP = Blend(v.CacheHitPrice, v.CacheHitPricePeak);
                var missP = Blend(v.CacheMissPrice, v.CacheMissPricePeak);
                var outP = Blend(v.OutputPrice, v.OutputPricePeak);
                var perM = Convert(PaygPerMLocal(v.Plan), v.Currency, DisplayCurrency, ExchangeRate);
                v.PerMillionValue = perM;
                v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
                v.PriceDisplay = v.PerMillionDisplay;
                v.CacheHitDisplay = $"{symbol}{Convert(hitP, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.CacheMissDisplay = $"{symbol}{Convert(missP, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
                v.OutputPriceDisplay = $"{symbol}{Convert(outP, v.Currency, DisplayCurrency, ExchangeRate):0.####}";
            }
        }
    }

    // 按量付费综合每 1M 单价（原始币种）：以 100M 输入为基准按构成（缓存命中 / 未命中 / 输出）加权
    private decimal PaygPerMLocal(AiPlan p)
    {
        var pk = (decimal)(p.Brand == "DeepSeek" ? DeepSeekPeakShare : 0d);
        decimal Blend(decimal offPeak, decimal peak) => offPeak + (peak - offPeak) * pk;
        var cost = (double)Blend(p.CacheHitPrice, p.CacheHitPricePeak) * CacheHitRatio
                 + (double)Blend(p.CacheMissPrice, p.CacheMissPricePeak) * (100d - CacheHitRatio)
                 + (double)Blend(p.OutputPrice, p.OutputPricePeak) * OutputRatio;
        var total = 100d + OutputRatio;
        return total > 0 ? (decimal)(cost / total) : 0m;
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

