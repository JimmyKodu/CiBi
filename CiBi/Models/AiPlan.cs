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

    // 按量付费字段：每 1M token 单价（套餐币种）
    public decimal CacheHitPrice { get; init; }        // 缓存命中
    public decimal CacheMissPrice { get; init; }       // 缓存未命中
    public decimal OutputPrice { get; init; }          // 输出

    // 52 weeks / 12 months
    public decimal MonthlyTokensMillions => Type == PlanType.Subscription ? WeeklyTokensMillions * (52m / 12m) : 0m;

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

        // DeepSeek V4 按量付费（国内，CNY，每 1M token 单价）
        new() { Id = "dsv4-flash", ShortName = "DeepSeek V4 Flash", FullName = "DeepSeek V4 Flash 按量付费",
                Tier = "Flash", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.05m, CacheMissPrice = 1.5m, OutputPrice = 4.5m },
        new() { Id = "dsv4-pro", ShortName = "DeepSeek V4 Pro", FullName = "DeepSeek V4 Pro 按量付费",
                Tier = "Pro", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.15m, CacheMissPrice = 4.5m, OutputPrice = 13.5m },
    ];
}
