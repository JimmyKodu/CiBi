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
    public int TokenMultiplier => Plan.TokenMultiplier;
    public decimal WeeklyTokensMillions => Plan.WeeklyTokensMillions;
    public decimal MonthlyTokensMillions => Plan.MonthlyTokensMillions;
    public decimal OriginalPrice => Plan.PriceMonthly;
    public string OriginalPriceText => $"{CurrencySymbol(Plan.Currency)}{Plan.PriceMonthly:0.##}";
    public string WeeklyTokensText => $"{Plan.WeeklyTokensMillions:#,##0.0} M";
    public string MonthlyTokensText => $"{Plan.MonthlyTokensMillions:#,##0.0} M";
    public string Subtitle => $"{Plan.Version} · {Plan.Region} · {WeeklyTokensText}/周 ×{TokenMultiplier}";
    public IBrush TierBrush => new SolidColorBrush(Color.Parse(Tier switch
    {
        "Lite" => "#6B7280",
        "Pro" => "#0EA5E9",
        _ => "#1428A0",
    }));

    private string _priceDisplay = "";
    public string PriceDisplay { get => _priceDisplay; set => this.RaiseAndSetIfChanged(ref _priceDisplay, value); }

    private string _perMillionDisplay = "";
    public string PerMillionDisplay { get => _perMillionDisplay; set => this.RaiseAndSetIfChanged(ref _perMillionDisplay, value); }

    private decimal _perMillionValue;
    public decimal PerMillionValue { get => _perMillionValue; set => this.RaiseAndSetIfChanged(ref _perMillionValue, value); }

    private int _rank;
    public int Rank { get => _rank; set => this.RaiseAndSetIfChanged(ref _rank, value); }

    public string RankText => $"#{Rank}";

    private bool _isBestValue;
    public bool IsBestValue { get => _isBestValue; set => this.RaiseAndSetIfChanged(ref _isBestValue, value); }

    private double _valueScore;
    public double ValueScore { get => _valueScore; set => this.RaiseAndSetIfChanged(ref _valueScore, value); }

    public string ValueLabel => IsBestValue ? "最优性价比" : $"性价比 {ValueScore * 100:0}%";

    public PlanView(AiPlan plan) => Plan = plan;

    public static string CurrencySymbol(string c) => c switch { "CNY" => "¥", "USD" => "$", _ => "" };
}
