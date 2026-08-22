using System.Collections.Generic;

namespace CiBi.Models;

public enum PlanType { Subscription, PayAsYouGo }

public sealed class AiPlan
{
    public required string Id { get; init; }
    public required string ShortName { get; init; }   // e.g. "Max 国际 V3"
    public required string FullName { get; init; }    // e.g. "GLM Coding Plan Max 国际版 V3"
    public required string Brand { get; init; }       // GLM / DeepSeek / Qwen / Kimi / MiniMax
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

    // GLM Coding Plan 高峰时段（北京时间每周一至周五 14:00-18:00）token 消耗计入配额的倍率：V2 ×3 / V3 ×2；1 = 不分时段
    public int PeakMultiplier => Brand == "GLM" && Type == PlanType.Subscription
        ? (Version == "V2" ? 3 : Version == "V3" ? 2 : 1)
        : 1;

    // 上下文窗口（tokens；0 = 未公布）
    public long ContextWindowTokens { get; init; }

    // 52 weeks / 12 months；直接给定月配额的套餐（MonthlyQuotaMillions > 0）不折算
    public decimal MonthlyTokensMillions => Type == PlanType.Subscription
        ? (MonthlyQuotaMillions > 0 ? MonthlyQuotaMillions : WeeklyTokensMillions * (52m / 12m))
        : 0m;

    public static readonly IReadOnlyList<AiPlan> All =
    [
        // V2 国际版 — base 79.67 M / week；年付订阅（价格已折算为月费）
        new() { Id = "v2-lite-int", ShortName = "Lite 国际 V2 年付", FullName = "GLM Coding Plan Lite 国际版 V2 年付",
                Brand = "GLM", Tier = "Lite", Version = "V2", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 12.6m, Currency = "USD", WeeklyTokensMillions = 79.67m, TokenMultiplier = 1 },
        new() { Id = "v2-pro-int", ShortName = "Pro 国际 V2 年付", FullName = "GLM Coding Plan Pro 国际版 V2 年付",
                Brand = "GLM", Tier = "Pro", Version = "V2", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 50.4m, Currency = "USD", WeeklyTokensMillions = 79.67m * 5, TokenMultiplier = 5 },
        new() { Id = "v2-max-int", ShortName = "Max 国际 V2 年付", FullName = "GLM Coding Plan Max 国际版 V2 年付",
                Brand = "GLM", Tier = "Max", Version = "V2", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 112m, Currency = "USD", WeeklyTokensMillions = 79.67m * 20, TokenMultiplier = 20 },

        // V3 国际版 — base 87 M / week；年付订阅（价格已折算为月费）
        new() { Id = "v3-lite-int", ShortName = "Lite 国际 V3 年付", FullName = "GLM Coding Plan Lite 国际版 V3 年付",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 12.6m, Currency = "USD", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-int", ShortName = "Pro 国际 V3 年付", FullName = "GLM Coding Plan Pro 国际版 V3 年付",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 56m, Currency = "USD", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-int", ShortName = "Max 国际 V3 年付", FullName = "GLM Coding Plan Max 国际版 V3 年付",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 117.6m, Currency = "USD", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },

        // V3 国内版 — base 0.87 亿 = 87 M / week；年付=七折 季付=八折 月付=原价
        new() { Id = "v3-lite-cn-year", ShortName = "Lite 国内 V3 年付", FullName = "GLM Coding Plan Lite 国内版 V3 年付",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 82.6m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn-year", ShortName = "Pro 国内 V3 年付", FullName = "GLM Coding Plan Pro 国内版 V3 年付",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 376.6m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn-year", ShortName = "Max 国内 V3 年付", FullName = "GLM Coding Plan Max 国内版 V3 年付",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 754.6m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-lite-cn-quarter", ShortName = "Lite 国内 V3 季付", FullName = "GLM Coding Plan Lite 国内版 V3 季付",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 94.4m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn-quarter", ShortName = "Pro 国内 V3 季付", FullName = "GLM Coding Plan Pro 国内版 V3 季付",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 430.4m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn-quarter", ShortName = "Max 国内 V3 季付", FullName = "GLM Coding Plan Max 国内版 V3 季付",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "季付",
                PriceMonthly = 862.4m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },
        new() { Id = "v3-lite-cn-month", ShortName = "Lite 国内 V3 月付", FullName = "GLM Coding Plan Lite 国内版 V3 月付",
                Brand = "GLM", Tier = "Lite", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 118m, Currency = "CNY", WeeklyTokensMillions = 87m, TokenMultiplier = 1 },
        new() { Id = "v3-pro-cn-month", ShortName = "Pro 国内 V3 月付", FullName = "GLM Coding Plan Pro 国内版 V3 月付",
                Brand = "GLM", Tier = "Pro", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 538m, Currency = "CNY", WeeklyTokensMillions = 87m * 6, TokenMultiplier = 6 },
        new() { Id = "v3-max-cn-month", ShortName = "Max 国内 V3 月付", FullName = "GLM Coding Plan Max 国内版 V3 月付",
                Brand = "GLM", Tier = "Max", Version = "V3", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 1078m, Currency = "CNY", WeeklyTokensMillions = 87m * 14, TokenMultiplier = 14 },

        // DeepSeek V4 按量付费（国内，CNY，每 1M token 单价；高峰=北京时间每日 9:00-12:00、14:00-18:00，单价=空闲×2）
        new() { Id = "dsv4-flash", ShortName = "DeepSeek V4 Flash", FullName = "DeepSeek V4 Flash 按量付费",
                Brand = "DeepSeek", Tier = "Flash", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.05m, CacheHitPricePeak = 0.10m,
                CacheMissPrice = 1.5m, CacheMissPricePeak = 3.0m,
                OutputPrice = 4.5m, OutputPricePeak = 9.0m },
        new() { Id = "dsv4-pro", ShortName = "DeepSeek V4 Pro", FullName = "DeepSeek V4 Pro 按量付费",
                Brand = "DeepSeek", Tier = "Pro", Version = "V4", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.15m, CacheHitPricePeak = 0.30m,
                CacheMissPrice = 4.5m, CacheMissPricePeak = 9.0m,
                OutputPrice = 13.5m, OutputPricePeak = 27.0m },

        // 以下按量付费模型不分时段：高峰单价与空闲同价
        new() { Id = "glm-5.3-payg", ShortName = "GLM-5.3", FullName = "GLM-5.3 按量付费",
                Brand = "GLM", Tier = "GLM", Version = "5.3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 2m, CacheHitPricePeak = 2m,
                CacheMissPrice = 8m, CacheMissPricePeak = 8m,
                OutputPrice = 28m, OutputPricePeak = 28m },
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
        // MiniMax-M3 按量付费 — 按上下文窗口分两档定价，永久五折（表中单价为折后实付价），不分时段；Tier 兼作筛选键（默认仅一档参与排行）
        new() { Id = "mm-m3-payg-512k", ShortName = "MiniMax-M3 ≤512K", FullName = "MiniMax-M3 按量付费（上下文 ≤ 512K，永久五折）",
                Brand = "MiniMax", Tier = "M3 ≤512K", Version = "M3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.42m, CacheHitPricePeak = 0.42m,
                CacheMissPrice = 2.1m, CacheMissPricePeak = 2.1m,
                OutputPrice = 8.4m, OutputPricePeak = 8.4m, ContextWindowTokens = 524_288 },
        new() { Id = "mm-m3-payg-1m", ShortName = "MiniMax-M3 1M", FullName = "MiniMax-M3 按量付费（上下文 512K ~ 1M，永久五折）",
                Brand = "MiniMax", Tier = "M3 1M", Version = "M3", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "CNY",
                CacheHitPrice = 0.84m, CacheHitPricePeak = 0.84m,
                CacheMissPrice = 4.2m, CacheMissPricePeak = 4.2m,
                OutputPrice = 16.8m, OutputPricePeak = 16.8m, ContextWindowTokens = 1_048_576 },
        // GPT-5.6 Sol 按量付费（USD，不分时段）— 按上下文分两档：≤272K 标准档；>272K 长上下文档（输入 ×2、输出 ×1.5）；官方另收缓存写入费（=输入 ×1.25，$5 / $10），本表不建模
        new() { Id = "gpt56-sol-payg-272k", ShortName = "GPT-5.6 Sol ≤272K", FullName = "GPT-5.6 Sol 按量付费（上下文 ≤ 272K）",
                Brand = "OpenAI", Tier = "Sol ≤272K", Version = "5.6", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "USD",
                CacheHitPrice = 0.40m, CacheHitPricePeak = 0.40m,
                CacheMissPrice = 4m, CacheMissPricePeak = 4m,
                OutputPrice = 20m, OutputPricePeak = 20m },
        new() { Id = "gpt56-sol-payg-lc", ShortName = "GPT-5.6 Sol >272K", FullName = "GPT-5.6 Sol 按量付费（上下文 > 272K，长上下文）",
                Brand = "OpenAI", Tier = "Sol >272K", Version = "5.6", Region = "按量付费", Type = PlanType.PayAsYouGo, Currency = "USD",
                CacheHitPrice = 0.80m, CacheHitPricePeak = 0.80m,
                CacheMissPrice = 8m, CacheMissPricePeak = 8m,
                OutputPrice = 30m, OutputPricePeak = 30m },

        // ChatGPT Plus — 月付 $20；无官方配额，按 Sol（≤272K）API 综合单价 1/20 暴力折算（无国内版，不参与地区/周期筛选）
        new() { Id = "chatgpt-plus", ShortName = "ChatGPT Plus", FullName = "ChatGPT Plus 订阅",
                Brand = "OpenAI", Tier = "Plus", Version = "—", Region = "国际版", Type = PlanType.Subscription,
                PriceMonthly = 20m, Currency = "USD", PaygAnchorId = "gpt56-sol-payg-272k", PaygAnchorFraction = 0.05m },

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

        // MiniMax Token Plan — 国内 CNY，月付，官方直接给定月配额（Plus 6亿 / Max 18亿 / Ultra 71亿）
        new() { Id = "mm-plus", ShortName = "MiniMax Plus", FullName = "MiniMax Token Plan Plus",
                Brand = "MiniMax", Tier = "Plus", Version = "—", Region = "国内版", Type = PlanType.Subscription,
                PriceMonthly = 49m, Currency = "CNY", MonthlyQuotaMillions = 600m },
        new() { Id = "mm-max", ShortName = "MiniMax Max", FullName = "MiniMax Token Plan Max",
                Brand = "MiniMax", Tier = "Max", Version = "—", Region = "国内版", Type = PlanType.Subscription,
                PriceMonthly = 119m, Currency = "CNY", MonthlyQuotaMillions = 1800m },
        new() { Id = "mm-ultra", ShortName = "MiniMax Ultra", FullName = "MiniMax Token Plan Ultra",
                Brand = "MiniMax", Tier = "Ultra", Version = "—", Region = "国内版", Type = PlanType.Subscription,
                PriceMonthly = 469m, Currency = "CNY", MonthlyQuotaMillions = 7100m },

        // Kimi For Coding 月付订阅 — 国际 USD / 国内 CNY，每周额度官方给定（Moderato 26.8M/周；Allegretto / Allegro 为其 5 / 15 倍）
        new() { Id = "kimi-moderato-int", ShortName = "Moderato 国际", FullName = "Kimi For Coding Moderato 国际版 月付",
                Brand = "Kimi", Tier = "Moderato", Version = "—", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 19m, Currency = "USD", WeeklyTokensMillions = 26.8m, TokenMultiplier = 1 },
        new() { Id = "kimi-moderato-cn", ShortName = "Moderato 国内", FullName = "Kimi For Coding Moderato 国内版 月付",
                Brand = "Kimi", Tier = "Moderato", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 99m, Currency = "CNY", WeeklyTokensMillions = 26.8m, TokenMultiplier = 1 },
        new() { Id = "kimi-allegretto-int", ShortName = "Allegretto 国际", FullName = "Kimi For Coding Allegretto 国际版 月付",
                Brand = "Kimi", Tier = "Allegretto", Version = "—", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 39m, Currency = "USD", WeeklyTokensMillions = 26.8m * 5, TokenMultiplier = 5 },
        new() { Id = "kimi-allegretto-cn", ShortName = "Allegretto 国内", FullName = "Kimi For Coding Allegretto 国内版 月付",
                Brand = "Kimi", Tier = "Allegretto", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 199m, Currency = "CNY", WeeklyTokensMillions = 26.8m * 5, TokenMultiplier = 5 },
        new() { Id = "kimi-allegro-int", ShortName = "Allegro 国际", FullName = "Kimi For Coding Allegro 国际版 月付",
                Brand = "Kimi", Tier = "Allegro", Version = "—", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 99m, Currency = "USD", WeeklyTokensMillions = 26.8m * 15, TokenMultiplier = 15 },
        new() { Id = "kimi-allegro-cn", ShortName = "Allegro 国内", FullName = "Kimi For Coding Allegro 国内版 月付",
                Brand = "Kimi", Tier = "Allegro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "月付",
                PriceMonthly = 699m, Currency = "CNY", WeeklyTokensMillions = 26.8m * 15, TokenMultiplier = 15 },

        // Kimi 年付 — 价格已折算为月费（国内 948/1908/6708 元每年 ÷ 12 = 79/159/559，约八折）
        new() { Id = "kimi-moderato-int-year", ShortName = "Moderato 国际 年付", FullName = "Kimi For Coding Moderato 国际版 年付",
                Brand = "Kimi", Tier = "Moderato", Version = "—", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 15m, Currency = "USD", WeeklyTokensMillions = 26.8m, TokenMultiplier = 1 },
        new() { Id = "kimi-moderato-cn-year", ShortName = "Moderato 国内 年付", FullName = "Kimi For Coding Moderato 国内版 年付",
                Brand = "Kimi", Tier = "Moderato", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 79m, Currency = "CNY", WeeklyTokensMillions = 26.8m, TokenMultiplier = 1 },
        new() { Id = "kimi-allegretto-int-year", ShortName = "Allegretto 国际 年付", FullName = "Kimi For Coding Allegretto 国际版 年付",
                Brand = "Kimi", Tier = "Allegretto", Version = "—", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 31m, Currency = "USD", WeeklyTokensMillions = 26.8m * 5, TokenMultiplier = 5 },
        new() { Id = "kimi-allegretto-cn-year", ShortName = "Allegretto 国内 年付", FullName = "Kimi For Coding Allegretto 国内版 年付",
                Brand = "Kimi", Tier = "Allegretto", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 159m, Currency = "CNY", WeeklyTokensMillions = 26.8m * 5, TokenMultiplier = 5 },
        new() { Id = "kimi-allegro-int-year", ShortName = "Allegro 国际 年付", FullName = "Kimi For Coding Allegro 国际版 年付",
                Brand = "Kimi", Tier = "Allegro", Version = "—", Region = "国际版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 79m, Currency = "USD", WeeklyTokensMillions = 26.8m * 15, TokenMultiplier = 15 },
        new() { Id = "kimi-allegro-cn-year", ShortName = "Allegro 国内 年付", FullName = "Kimi For Coding Allegro 国内版 年付",
                Brand = "Kimi", Tier = "Allegro", Version = "—", Region = "国内版", Type = PlanType.Subscription, BillingCycle = "年付",
                PriceMonthly = 559m, Currency = "CNY", WeeklyTokensMillions = 26.8m * 15, TokenMultiplier = 15 },
    ];
}
