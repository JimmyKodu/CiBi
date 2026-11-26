using System;
using System.Collections.Generic;

namespace CiBi.Models;

public enum PlanType { Subscription, PayAsYouGo }

public sealed class AiPlan
{
    public required string Id { get; init; }
    public required string ShortName { get; init; }   // e.g. "Max 国际 V3"
    public required string FullName { get; init; }    // e.g. "GLM Coding Plan Max 国内版 V3"
    public required string Brand { get; init; }       // GLM / DeepSeek / Qwen / Kimi / MiniMax
    public required string Tier { get; init; }        // Lite / Pro / Max / Flash
    public required string Version { get; init; }     // V3 / V4 / V4.1 / 5.3 …
    public required string Region { get; init; }      // 国际版 / 国内版 / 按量付费
    public required PlanType Type { get; init; }      // 订阅制 / 按量付费
    public required string Currency { get; init; }    // USD / CNY
    public string BillingCycle { get; init; } = "";   // 年付 / 季付 / 月付（国内版 V3）

    // 订阅制字段
    public decimal PriceMonthly { get; init; }
    public decimal WeeklyTokensMillions { get; init; }
    public int TokenMultiplier { get; init; }          // ×1 / ×4 / ×5 / ×6 / ×14 / ×15 / ×16

    // 订阅制但官方直接给定月配额（M）的套餐（如 MiniMax），>0 时优先于周配额折算
    public decimal MonthlyQuotaMillions { get; init; }

    // 订阅制无官方配额时（如 ChatGPT Plus）：每 1M 价格 = 锚定按量付费套餐综合单价 × 比例（0.05 = 1/20），随用量构成滑块联动；空 = 正常按配额折算
    public string PaygAnchorId { get; init; } = "";
    public decimal PaygAnchorFraction { get; init; } = 1m;

    // 按量付费字段：每 1M token 单价（套餐币种，空闲时段）
    public decimal CacheHitPrice { get; init; }        // 缓存命中
    public decimal CacheMissPrice { get; init; }       // 缓存未命中
    public decimal OutputPrice { get; init; }          // 输出

    // 高峰时段单价（每 1M token；与空闲同价表示不分时段）
    public decimal CacheHitPricePeak { get; init; }
    public decimal CacheMissPricePeak { get; init; }
    public decimal OutputPricePeak { get; init; }

    // GLM Coding Plan 高峰时段（北京时间每周一至周五 14:00-18:00）token 消耗计入配额的倍率：V3 ×2；1 = 不分时段
    public int PeakMultiplier => Brand == "GLM" && Type == PlanType.Subscription && Version == "V3" ? 2 : 1;

    // 订阅制按模型计的额度消耗速率（当前各套餐均 1x，额度按模型分列直接给定；<1 表示每 token 少计入配额，高峰同乘 PeakMultiplier）
    public decimal QuotaBurnRate { get; init; } = 1m;

    // 限时优惠：官方按折扣结算（消耗按原抵扣系数的 50% 计入 → 可用额度为常态的 2 倍），仅在起止日期内生效；
    // 起止按北京时间、含首尾，过期自动回落常态额度，页面无需改数据即可显示正确价格
    public decimal QuotaPromoFactor { get; init; } = 1m;   // 可用额度倍率（1 = 无优惠）
    public DateOnly? QuotaPromoStart { get; init; }
    public DateOnly? QuotaPromoEnd { get; init; }

    // 上下文窗口（tokens；0 = 未公布）
    public long ContextWindowTokens { get; init; }

    // 北京时间当天（官网优惠期按北京时间起止）
    public static DateOnly BeijingToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

    public bool IsPromoActive(DateOnly today) =>
        QuotaPromoFactor != 1m
        && (QuotaPromoStart is null || today >= QuotaPromoStart.Value)
        && (QuotaPromoEnd is null || today <= QuotaPromoEnd.Value);

    // 生效中的周配额（已含优惠加成；优惠过期即回落常态）
    public decimal EffectiveWeeklyTokensMillions => EffectiveWeeklyTokensMillionsOn(BeijingToday());

    // 指定日期下的有效周配额（便于边界验证）
    public decimal EffectiveWeeklyTokensMillionsOn(DateOnly today) =>
        WeeklyTokensMillions * (IsPromoActive(today) ? QuotaPromoFactor : 1m);

    // 52 weeks / 12 months；直接给定月配额的套餐（MonthlyQuotaMillions > 0）不折算
    public decimal MonthlyTokensMillions => Type == PlanType.Subscription
        ? (MonthlyQuotaMillions > 0 ? MonthlyQuotaMillions : EffectiveWeeklyTokensMillions * (52m / 12m))
        : 0m;

    public static readonly IReadOnlyList<AiPlan> All =
    [
        // V3 国内版 — GLM-5.4 约 1.45 亿 / 周；GLM-5.3-Flash 常态 2.92 亿 / 周，限时优惠期（11/24–12/24）按折扣结算使额度翻倍至 5.84 亿 / 周（过期自动回落）；年付=七折 季付=八折 月付=原价；高峰（工作日 14-18）消耗 ×2 计入配额
        new() { Id = "v3-lite-cn-year", ShortName = "Lite 国内 V3 年付 · 5.4", FullName = "GLM Coding Plan Lite 国内版 V3 年付 · GLM-5.4",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 82.6m, Currency = "CNY", WeeklyTokensMillions = 145m, TokenMultiplier = 1 },
        new() { Id = "v3-lite-cn-year-flash", ShortName = "Lite 国内 V3 年付 · Flash", FullName = "GLM Coding Plan Lite 国内版 V3 年付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 82.6m, Currency = "CNY", WeeklyTokensMillions = 292m, TokenMultiplier = 1,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-pro-cn-year", ShortName = "Pro 国内 V3 年付 · 5.4", FullName = "GLM Coding Plan Pro 国内版 V3 年付 · GLM-5.4",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 376.6m, Currency = "CNY", WeeklyTokensMillions = 145m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-pro-cn-year-flash", ShortName = "Pro 国内 V3 年付 · Flash", FullName = "GLM Coding Plan Pro 国内版 V3 年付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 376.6m, Currency = "CNY", WeeklyTokensMillions = 292m * 6, TokenMultiplier = 6,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-max-cn-year", ShortName = "Max 国内 V3 年付 · 5.4", FullName = "GLM Coding Plan Max 国内版 V3 年付 · GLM-5.4",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 754.6m, Currency = "CNY", WeeklyTokensMillions = 145m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-max-cn-year-flash", ShortName = "Max 国内 V3 年付 · Flash", FullName = "GLM Coding Plan Max 国内版 V3 年付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 754.6m, Currency = "CNY", WeeklyTokensMillions = 292m * 14, TokenMultiplier = 14,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-lite-cn-quarter", ShortName = "Lite 国内 V3 季付 · 5.4", FullName = "GLM Coding Plan Lite 国内版 V3 季付 · GLM-5.4",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 94.4m, Currency = "CNY", WeeklyTokensMillions = 145m, TokenMultiplier = 1 },
        new() { Id = "v3-lite-cn-quarter-flash", ShortName = "Lite 国内 V3 季付 · Flash", FullName = "GLM Coding Plan Lite 国内版 V3 季付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 94.4m, Currency = "CNY", WeeklyTokensMillions = 292m, TokenMultiplier = 1,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-pro-cn-quarter", ShortName = "Pro 国内 V3 季付 · 5.4", FullName = "GLM Coding Plan Pro 国内版 V3 季付 · GLM-5.4",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 430.4m, Currency = "CNY", WeeklyTokensMillions = 145m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-pro-cn-quarter-flash", ShortName = "Pro 国内 V3 季付 · Flash", FullName = "GLM Coding Plan Pro 国内版 V3 季付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 430.4m, Currency = "CNY", WeeklyTokensMillions = 292m * 6, TokenMultiplier = 6,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-max-cn-quarter", ShortName = "Max 国内 V3 季付 · 5.4", FullName = "GLM Coding Plan Max 国内版 V3 季付 · GLM-5.4",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 862.4m, Currency = "CNY", WeeklyTokensMillions = 145m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-max-cn-quarter-flash", ShortName = "Max 国内 V3 季付 · Flash", FullName = "GLM Coding Plan Max 国内版 V3 季付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 862.4m, Currency = "CNY", WeeklyTokensMillions = 292m * 14, TokenMultiplier = 14,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-lite-cn-month", ShortName = "Lite 国内 V3 月付 · 5.4", FullName = "GLM Coding Plan Lite 国内版 V3 月付 · GLM-5.4",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 118m, Currency = "CNY", WeeklyTokensMillions = 145m, TokenMultiplier = 1 },
        new() { Id = "v3-lite-cn-month-flash", ShortName = "Lite 国内 V3 月付 · Flash", FullName = "GLM Coding Plan Lite 国内版 V3 月付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 118m, Currency = "CNY", WeeklyTokensMillions = 292m, TokenMultiplier = 1,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-pro-cn-month", ShortName = "Pro 国内 V3 月付 · 5.4", FullName = "GLM Coding Plan Pro 国内版 V3 月付 · GLM-5.4",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 538m, Currency = "CNY", WeeklyTokensMillions = 145m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-pro-cn-month-flash", ShortName = "Pro 国内 V3 月付 · Flash", FullName = "GLM Coding Plan Pro 国内版 V3 月付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 538m, Currency = "CNY", WeeklyTokensMillions = 292m * 6, TokenMultiplier = 6,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },
        new() { Id = "v3-max-cn-month", ShortName = "Max 国内 V3 月付 · 5.4", FullName = "GLM Coding Plan Max 国内版 V3 月付 · GLM-5.4",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 1078m, Currency = "CNY", WeeklyTokensMillions = 145m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-max-cn-month-flash", ShortName = "Max 国内 V3 月付 · Flash", FullName = "GLM Coding Plan Max 国内版 V3 月付 · GLM-5.3-Flash",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 1078m, Currency = "CNY", WeeklyTokensMillions = 292m * 14, TokenMultiplier = 14,
                QuotaPromoFactor = 2m, QuotaPromoStart = new DateOnly(2026, 11, 24), QuotaPromoEnd = new DateOnly(2026, 12, 24) },

        // DeepSeek 按量付费（国内，CNY，每 1M token 单价；高峰=北京时间工作日 9:00-12:00、14:00-18:00，周末不涨价，单价=空闲×2；上下文均 1M）
        new() { Id = "dsv41-flash", ShortName = "DeepSeek V4.1 Flash", FullName = "DeepSeek-V4.1-Flash 按量付费",
                Brand = "DeepSeek", Tier = "Flash", Version = "V4.1", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.02m, CacheHitPricePeak = 0.04m,
                CacheMissPrice = 1m, CacheMissPricePeak = 2m,
                OutputPrice = 4m, OutputPricePeak = 8m, ContextWindowTokens = 1_048_576 },
        new() { Id = "dsv4-pro", ShortName = "DeepSeek V4 Pro", FullName = "DeepSeek-V4-Pro-0813 按量付费",
                Brand = "DeepSeek", Tier = "Pro", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.15m, CacheHitPricePeak = 0.30m,
                CacheMissPrice = 4.5m, CacheMissPricePeak = 9.0m,
                OutputPrice = 13.5m, OutputPricePeak = 27.0m, ContextWindowTokens = 1_048_576 },

        // 以下按量付费模型不分时段：高峰单价与空闲同价
        new() { Id = "glm-5.3-payg", ShortName = "GLM-5.3", FullName = "GLM-5.3 按量付费",
                Brand = "GLM", Tier = "GLM", Version = "5.3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 2m, CacheHitPricePeak = 2m,
                CacheMissPrice = 8m, CacheMissPricePeak = 8m,
                OutputPrice = 28m, OutputPricePeak = 28m },
        new() { Id = "glm-5.3-flash-payg", ShortName = "GLM-5.3-Flash", FullName = "GLM-5.3-Flash 按量付费",
                Brand = "GLM", Tier = "GLM", Version = "5.3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.23m, CacheHitPricePeak = 0.23m,
                CacheMissPrice = 0.8m, CacheMissPricePeak = 0.8m,
                OutputPrice = 2.8m, OutputPricePeak = 2.8m },
        new() { Id = "qwen3.8-max-payg", ShortName = "Qwen3.8-Max", FullName = "Qwen3.8-Max 按量付费",
                Brand = "Qwen", Tier = "Qwen", Version = "3.8", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 1.5m, CacheHitPricePeak = 1.5m,
                CacheMissPrice = 12m, CacheMissPricePeak = 12m,
                OutputPrice = 36m, OutputPricePeak = 36m },
        new() { Id = "kimi-k3-payg", ShortName = "Kimi K3", FullName = "Kimi K3 按量付费",
                Brand = "Kimi", Tier = "Kimi", Version = "K3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 2m, CacheHitPricePeak = 2m,
                CacheMissPrice = 20m, CacheMissPricePeak = 20m,
                OutputPrice = 100m, OutputPricePeak = 100m, ContextWindowTokens = 1_048_576 },
        // MiniMax-M3.1-Flash 按量付费 — 模型升级后的新单价（缓存命中 0.10 / 缓存未命中 1 / 输出 4，每 1M token，元），不分时段；取代原 MiniMax-M3 上下文档
        new() { Id = "mm-m31-flash-payg", ShortName = "MiniMax-M3.1-Flash", FullName = "MiniMax-M3.1-Flash 按量付费",
                Brand = "MiniMax", Tier = "M3.1-Flash", Version = "M3.1", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.10m, CacheHitPricePeak = 0.10m,
                CacheMissPrice = 1m, CacheMissPricePeak = 1m,
                OutputPrice = 4m, OutputPricePeak = 4m, ContextWindowTokens = 1_048_576 },
        // GPT-6 Astra 按量付费（USD，不分时段）— 按上下文分两档：≤272K 标准档；>272K 长上下文档（输入 ×2、输出 ×1.5）；官方另收缓存写入费（=输入 ×1.25，$12.5 / $25），本表不建模
        new() { Id = "gpt6-astra-payg-272k", ShortName = "GPT-6 Astra ≤272K", FullName = "GPT-6 Astra 按量付费（上下文 ≤ 272K）",
                Brand = "OpenAI", Tier = "Astra ≤272K", Version = "6", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "USD",
                CacheHitPrice = 1m, CacheHitPricePeak = 1m,
                CacheMissPrice = 10m, CacheMissPricePeak = 10m,
                OutputPrice = 50m, OutputPricePeak = 50m },
        new() { Id = "gpt6-astra-payg-lc", ShortName = "GPT-6 Astra >272K", FullName = "GPT-6 Astra 按量付费（上下文 > 272K，长上下文）",
                Brand = "OpenAI", Tier = "Astra >272K", Version = "6", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "USD",
                CacheHitPrice = 2m, CacheHitPricePeak = 2m,
                CacheMissPrice = 20m, CacheMissPricePeak = 20m,
                OutputPrice = 75m, OutputPricePeak = 75m },

        // ChatGPT Plus — 月付 $20；无官方配额，按 Astra（≤272K）API 综合单价 1/20 暴力折算（无国内版，不参与地区/周期筛选）
        new() { Id = "chatgpt-plus", ShortName = "ChatGPT Plus", FullName = "ChatGPT Plus 订阅",
                Brand = "OpenAI", Tier = "Plus", Version = "—", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 20m, Currency = "USD", PaygAnchorId = "gpt6-astra-payg-272k", PaygAnchorFraction = 0.05m },

        // Qwen Coding Plan 订阅制 — 国内 CNY，每周额度 Lite 7M、Standard 4 倍、Pro 16 倍；季付/年付总价已折算为月费
        new() { Id = "qwen-lite-month", ShortName = "Lite 月付", FullName = "Qwen Coding Plan Lite 国内版 月付",
                Brand = "Qwen", Tier = "Lite", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 39m, Currency = "CNY", WeeklyTokensMillions = 7m, TokenMultiplier = 1 },
        new() { Id = "qwen-standard-month", ShortName = "Standard 月付", FullName = "Qwen Coding Plan Standard 国内版 月付",
                Brand = "Qwen", Tier = "Standard", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 139m, Currency = "CNY", WeeklyTokensMillions = 7m * 4, TokenMultiplier = 4 },
        new() { Id = "qwen-pro-month", ShortName = "Pro 月付", FullName = "Qwen Coding Plan Pro 国内版 月付",
                Brand = "Qwen", Tier = "Pro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 499m, Currency = "CNY", WeeklyTokensMillions = 7m * 16, TokenMultiplier = 16 },
        new() { Id = "qwen-lite-quarter", ShortName = "Lite 季付", FullName = "Qwen Coding Plan Lite 国内版 季付",
                Brand = "Qwen", Tier = "Lite", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 110m / 3m, Currency = "CNY", WeeklyTokensMillions = 7m, TokenMultiplier = 1 },
        new() { Id = "qwen-standard-quarter", ShortName = "Standard 季付", FullName = "Qwen Coding Plan Standard 国内版 季付",
                Brand = "Qwen", Tier = "Standard", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 396m / 3m, Currency = "CNY", WeeklyTokensMillions = 7m * 4, TokenMultiplier = 4 },
        new() { Id = "qwen-pro-quarter", ShortName = "Pro 季付", FullName = "Qwen Coding Plan Pro 国内版 季付",
                Brand = "Qwen", Tier = "Pro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 1420m / 3m, Currency = "CNY", WeeklyTokensMillions = 7m * 16, TokenMultiplier = 16 },
        new() { Id = "qwen-lite-year", ShortName = "Lite 年付", FullName = "Qwen Coding Plan Lite 国内版 年付",
                Brand = "Qwen", Tier = "Lite", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 420m / 12m, Currency = "CNY", WeeklyTokensMillions = 7m, TokenMultiplier = 1 },
        new() { Id = "qwen-standard-year", ShortName = "Standard 年付", FullName = "Qwen Coding Plan Standard 国内版 年付",
                Brand = "Qwen", Tier = "Standard", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 1510m / 12m, Currency = "CNY", WeeklyTokensMillions = 7m * 4, TokenMultiplier = 4 },
        new() { Id = "qwen-pro-year", ShortName = "Pro 年付", FullName = "Qwen Coding Plan Pro 国内版 年付",
                Brand = "Qwen", Tier = "Pro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 5600m / 12m, Currency = "CNY", WeeklyTokensMillions = 7m * 16, TokenMultiplier = 16 },

        // MiniMax Token Plan — 国内 CNY，月付；官方标称月配额 Plus 6亿 / Max 18亿 / Ultra 71亿，实测约 3 倍，按实测计算
        new() { Id = "mm-plus", ShortName = "MiniMax Plus", FullName = "MiniMax Token Plan Plus",
                Brand = "MiniMax", Tier = "Plus", Version = "—", Region = "国内版", Type = PlanType.Subscription,
                PriceMonthly = 49m, Currency = "CNY", MonthlyQuotaMillions = 1800m },
        new() { Id = "mm-max", ShortName = "MiniMax Max", FullName = "MiniMax Token Plan Max",
                Brand = "MiniMax", Tier = "Max", Version = "—", Region = "国内版", Type = PlanType.Subscription,
                PriceMonthly = 119m, Currency = "CNY", MonthlyQuotaMillions = 5400m },
        new() { Id = "mm-ultra", ShortName = "MiniMax Ultra", FullName = "MiniMax Token Plan Ultra",
                Brand = "MiniMax", Tier = "Ultra", Version = "—", Region = "国内版", Type = PlanType.Subscription,
                PriceMonthly = 469m, Currency = "CNY", MonthlyQuotaMillions = 21300m },

        // Kimi For Coding 月付订阅 — 国内 CNY，每周额度官方给定（Moderato 48M/周；Allegretto / Allegro 为其 5 / 15 倍）
        new() { Id = "kimi-moderato-cn", ShortName = "Moderato 国内", FullName = "Kimi For Coding Moderato 国内版 月付",
                Brand = "Kimi", Tier = "Moderato", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 99m, Currency = "CNY", WeeklyTokensMillions = 48m, TokenMultiplier = 1 },
        new() { Id = "kimi-allegretto-cn", ShortName = "Allegretto 国内", FullName = "Kimi For Coding Allegretto 国内版 月付",
                Brand = "Kimi", Tier = "Allegretto", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 199m, Currency = "CNY", WeeklyTokensMillions = 48m * 5, TokenMultiplier = 5 },
        new() { Id = "kimi-allegro-cn", ShortName = "Allegro 国内", FullName = "Kimi For Coding Allegro 国内版 月付",
                Brand = "Kimi", Tier = "Allegro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 699m, Currency = "CNY", WeeklyTokensMillions = 48m * 15, TokenMultiplier = 15 },

        // Kimi 年付 — 价格已折算为月费（国内 948/1908/6708 元每年 ÷ 12 = 79/159/559，约八折）
        new() { Id = "kimi-moderato-cn-year", ShortName = "Moderato 国内 年付", FullName = "Kimi For Coding Moderato 国内版 年付",
                Brand = "Kimi", Tier = "Moderato", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 79m, Currency = "CNY", WeeklyTokensMillions = 48m, TokenMultiplier = 1 },
        new() { Id = "kimi-allegretto-cn-year", ShortName = "Allegretto 国内 年付", FullName = "Kimi For Coding Allegretto 国内版 年付",
                Brand = "Kimi", Tier = "Allegretto", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 159m, Currency = "CNY", WeeklyTokensMillions = 48m * 5, TokenMultiplier = 5 },
        new() { Id = "kimi-allegro-cn-year", ShortName = "Allegro 国内 年付", FullName = "Kimi For Coding Allegro 国内版 年付",
                Brand = "Kimi", Tier = "Allegro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 559m, Currency = "CNY", WeeklyTokensMillions = 48m * 15, TokenMultiplier = 15 },
    ];
}
