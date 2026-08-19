using System.Collections.Generic;

namespace CiBi.Models;

public enum PlanType { Subscription, PayAsYouGo }

public sealed class AiPlan
{
    public required string Id { get; init; }
    public required string ShortName { get; init; }   // e.g. "Max 国际 V3"
    public required string FullName { get; init; }    // e.g. "GLM Coding Plan Max 国际版 V3"
    public required string Tier { get; init; }        // Lite / Pro / Max / Flash
    public required string Version { get; init; }     // V2 / V3 / V4
    public required string Region { get; init; }      // 国际版 / 国内版 / 按量付费
    public required PlanType Type { get; init; }      // 订阅制 / 按量付费
    public required string Currency { get; init; }    // USD / CNY
    public string BillingCycle { get; init; } = "";   // 年付 / 季付 / 月付（国内版 V3）

    // 订阅制字段
    public decimal PriceMonthly { get; init; }
    public decimal WeeklyTokensMillions { get; init; }
    public int TokenMultiplier { get; init; }          // ×1 / ×5 / ×6 / ×14 / ×20

    // 订阅制但官方直接给定月配额（M）的套餐（如 MiniMax），>0 时优先于周配额折算
    public decimal MonthlyQuotaMillions { get; init; }

    // 按量付费字段：每 1M token 单价（套餐币种，空闲时段）
    public decimal CacheHitPrice { get; init; }        // 缓存命中
    public decimal CacheMissPrice { get; init; }       // 缓存未命中
    public decimal OutputPrice { get; init; }          // 输出

    // 高峰时段单价（每 1M token；0 表示不分时段）
    public decimal CacheHitPricePeak { get; init; }
    public decimal CacheMissPricePeak { get; init; }
    public decimal OutputPricePeak { get; init; }

    // 上下文窗口（tokens；0 = 未公布）
    public long ContextWindowTokens { get; init; }

    // 52 weeks / 12 months；直接给定月配额的套餐（MonthlyQuotaMillions > 0）不折算
    public decimal MonthlyTokensMillions => Type == PlanType.Subscription
        ? (MonthlyQuotaMillions > 0 ? MonthlyQuotaMillions : WeeklyTokensMillions * (52m / 12m))
        : 0m;

    public static readonly IReadOnlyList<AiPlan> All =
    [
        // V2 国际版 — base 79.67 M / week
        new() { Id = "v2-lite-int", ShortName = "Lite 国际 V2", FullName = "GLM Coding Plan Lite 国际版 V2",
                Tier = "Lite", Version = "V2", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 12.6m, Currency = "USD", WeeklyTokensMillions = 79.67m, TokenMultiplier = 1 },
        new() { Id = "v2-pro-int", ShortName = "Pro 国际 V2", FullName = "GLM Coding Plan Pro 国际版 V2",
                Tier = "Pro", Version = "V2", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 50.4m, Currency = "USD", WeeklyTokensMillions = 79.67m * 5, TokenMultiplier = 5 },
        new() { Id = "v2-max-int", ShortName = "Max 国际 V2", FullName = "GLM Coding Plan Max 国际版 V2",
                Tier = "Max", Version = "V2", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 112m, Currency = "USD", WeeklyTokensMillions = 79.67m * 20, TokenMultiplier = 20 },

        // V3 国际版 — base 87 M / week
        new() { Id = "v3-lite-int", ShortName = "Lite 国际 V3", FullName = "GLM Coding Plan Lite 国际版 V3",
                Tier = "Lite", Version = "V3", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 12.6m, Currency = "USD", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-int", ShortName = "Pro 国际 V3", FullName = "GLM Coding Plan Pro 国际版 V3",
                Tier = "Pro", Version = "V3", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 56m, Currency = "USD", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-int", ShortName = "Max 国际 V3", FullName = "GLM Coding Plan Max 国际版 V3",
                Tier = "Max", Version = "V3", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 117.6m, Currency = "USD", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },

        // V3 国内版 — base 0.87 亿 = 87 M / week；年付=七折 季付=八折 月付=原价
        new() { Id = "v3-lite-cn-year", ShortName = "Lite 国内 V3 年付", FullName = "GLM Coding Plan Lite 国内版 V3 年付",
                Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 82.6m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn-year", ShortName = "Pro 国内 V3 年付", FullName = "GLM Coding Plan Pro 国内版 V3 年付",
                Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 376.6m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn-year", ShortName = "Max 国内 V3 年付", FullName = "GLM Coding Plan Max 国内版 V3 年付",
                Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 754.6m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-lite-cn-quarter", ShortName = "Lite 国内 V3 季付", FullName = "GLM Coding Plan Lite 国内版 V3 季付",
                Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 94.4m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn-quarter", ShortName = "Pro 国内 V3 季付", FullName = "GLM Coding Plan Pro 国内版 V3 季付",
                Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 430.4m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn-quarter", ShortName = "Max 国内 V3 季付", FullName = "GLM Coding Plan Max 国内版 V3 季付",
                Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 862.4m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-lite-cn-month", ShortName = "Lite 国内 V3 月付", FullName = "GLM Coding Plan Lite 国内版 V3 月付",
                Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 118m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn-month", ShortName = "Pro 国内 V3 月付", FullName = "GLM Coding Plan Pro 国内版 V3 月付",
                Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 538m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn-month", ShortName = "Max 国内 V3 月付", FullName = "GLM Coding Plan Max 国内版 V3 月付",
                Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 1078m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },

        // DeepSeek V4 按量付费（国内，CNY，每 1M token 单价；分空闲/高峰时段，高峰=空闲×2）
        new() { Id = "dsv4-flash", ShortName = "DeepSeek V4 Flash", FullName = "DeepSeek V4 Flash 按量付费",
                Tier = "Flash", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.05m, CacheHitPricePeak = 0.10m,
                CacheMissPrice = 1.5m, CacheMissPricePeak = 3.0m,
                OutputPrice = 4.5m, OutputPricePeak = 9.0m },
        new() { Id = "dsv4-pro", ShortName = "DeepSeek V4 Pro", FullName = "DeepSeek V4 Pro 按量付费",
                Tier = "Pro", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.15m, CacheHitPricePeak = 0.30m,
                CacheMissPrice = 4.5m, CacheMissPricePeak = 9.0m,
                OutputPrice = 13.5m, OutputPricePeak = 27.0m },

        // 以下按量付费模型不分时段：高峰单价与空闲同价
        new() { Id = "glm-5.3-payg", ShortName = "GLM-5.3", FullName = "GLM-5.3 按量付费",
                Tier = "GLM", Version = "5.3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 2m, CacheHitPricePeak = 2m,
                CacheMissPrice = 8m, CacheMissPricePeak = 8m,
                OutputPrice = 28m, OutputPricePeak = 28m },
        new() { Id = "qwen3.8-max-payg", ShortName = "Qwen3.8-Max", FullName = "Qwen3.8-Max 按量付费",
                Tier = "Qwen", Version = "3.8", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 1.5m, CacheHitPricePeak = 1.5m,
                CacheMissPrice = 12m, CacheMissPricePeak = 12m,
                OutputPrice = 36m, OutputPricePeak = 36m },
        new() { Id = "kimi-k3-payg", ShortName = "Kimi K3", FullName = "Kimi K3 按量付费",
                Tier = "Kimi", Version = "K3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 2m, CacheHitPricePeak = 2m,
                CacheMissPrice = 20m, CacheMissPricePeak = 20m,
                OutputPrice = 100m, OutputPricePeak = 100m, ContextWindowTokens = 1_048_576 },

        // MiniMax Token Plan — 国内 CNY，月付，官方直接给定月配额（Plus 6亿 / Max 18亿 / Ultra 71亿）
        new() { Id = "mm-plus", ShortName = "MiniMax Plus", FullName = "MiniMax Token Plan Plus",
                Tier = "Plus", Version = "—", Region = "MiniMax", Type = PlanType.Subscription,
                PriceMonthly = 49m, Currency = "CNY", MonthlyQuotaMillions = 600m },
        new() { Id = "mm-max", ShortName = "MiniMax Max", FullName = "MiniMax Token Plan Max",
                Tier = "Max", Version = "—", Region = "MiniMax", Type = PlanType.Subscription,
                PriceMonthly = 119m, Currency = "CNY", MonthlyQuotaMillions = 1800m },
        new() { Id = "mm-ultra", ShortName = "MiniMax Ultra", FullName = "MiniMax Token Plan Ultra",
                Tier = "Ultra", Version = "—", Region = "MiniMax", Type = PlanType.Subscription,
                PriceMonthly = 469m, Currency = "CNY", MonthlyQuotaMillions = 7100m },
    ];
}
