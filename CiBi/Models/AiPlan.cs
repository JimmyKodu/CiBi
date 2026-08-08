using System.Collections.Generic;

namespace CiBi.Models;

public sealed class AiPlan
{
    public required string Id { get; init; }
    public required string ShortName { get; init; }   // e.g. "Max 国际 V3"
    public required string FullName { get; init; }   // e.g. "GLM Coding Plan Max 国际版 V3"
    public required string Tier { get; init; }        // Lite / Pro / Max
    public required string Version { get; init; }     // V2 / V3
    public required string Region { get; init; }      // 国际版 / 国内版
    public required decimal PriceMonthly { get; init; }
    public required string Currency { get; init; }    // USD / CNY
    public required decimal WeeklyTokensMillions { get; init; }
    public required int TokenMultiplier { get; init; } // ×1 / ×5 / ×6 / ×14 / ×20

    // 52 weeks / 12 months
    public decimal MonthlyTokensMillions => WeeklyTokensMillions * (52m / 12m);

    public static readonly IReadOnlyList<AiPlan> All =
    [
        // V2 国际版 — base 80.18 M / week
        new() { Id = "v2-lite-int", ShortName = "Lite 国际 V2", FullName = "GLM Coding Plan Lite 国际版 V2",
                Tier = "Lite", Version = "V2", Region = "国际版",
                PriceMonthly = 12.6m, Currency = "USD", WeeklyTokensMillions = 80.18m, TokenMultiplier = 1 },
        new() { Id = "v2-pro-int", ShortName = "Pro 国际 V2", FullName = "GLM Coding Plan Pro 国际版 V2",
                Tier = "Pro", Version = "V2", Region = "国际版",
                PriceMonthly = 50.4m, Currency = "USD", WeeklyTokensMillions = 80.18m * 5, TokenMultiplier = 5 },
        new() { Id = "v2-max-int", ShortName = "Max 国际 V2", FullName = "GLM Coding Plan Max 国际版 V2",
                Tier = "Max", Version = "V2", Region = "国际版",
                PriceMonthly = 112m, Currency = "USD", WeeklyTokensMillions = 80.18m * 20, TokenMultiplier = 20 },

        // V3 国际版 — base 87 M / week
        new() { Id = "v3-lite-int", ShortName = "Lite 国际 V3", FullName = "GLM Coding Plan Lite 国际版 V3",
                Tier = "Lite", Version = "V3", Region = "国际版",
                PriceMonthly = 12.6m, Currency = "USD", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-int", ShortName = "Pro 国际 V3", FullName = "GLM Coding Plan Pro 国际版 V3",
                Tier = "Pro", Version = "V3", Region = "国际版",
                PriceMonthly = 56m, Currency = "USD", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-int", ShortName = "Max 国际 V3", FullName = "GLM Coding Plan Max 国际版 V3",
                Tier = "Max", Version = "V3", Region = "国际版",
                PriceMonthly = 117.6m, Currency = "USD", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },

        // V3 国内版 — base 0.87 亿 = 87 M / week
        new() { Id = "v3-lite-cn", ShortName = "Lite 国内 V3", FullName = "GLM Coding Plan Lite 国内版 V3",
                Tier = "Lite", Version = "V3", Region = "国内版",
                PriceMonthly = 82.6m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn", ShortName = "Pro 国内 V3", FullName = "GLM Coding Plan Pro 国内版 V3",
                Tier = "Pro", Version = "V3", Region = "国内版",
                PriceMonthly = 376.6m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn", ShortName = "Max 国内 V3", FullName = "GLM Coding Plan Max 国内版 V3",
                Tier = "Max", Version = "V3", Region = "国内版",
                PriceMonthly = 754.6m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },
    ];
}
