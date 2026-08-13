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
        foreach (var v in Plans)
        {
            var price = Convert(v.OriginalPrice, v.Currency, DisplayCurrency, ExchangeRate);
            v.PriceDisplay = $"{symbol}{price:#,##0.##}";
            var perM = v.MonthlyTokensMillions > 0 ? price / v.MonthlyTokensMillions : 0m;
            v.PerMillionValue = perM;
            v.PerMillionDisplay = perM <= 0 ? "-" : $"{symbol}{perM:#,##0.0000}";
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
    }

    private static decimal Convert(decimal amount, string from, string to, decimal rate)
    {
        if (from == to) return amount;
        if (from == "USD" && to == "CNY") return amount * rate;
        if (from == "CNY" && to == "USD") return amount / rate;
        return amount;
    }
}

