using System;

namespace EstateCRM.App.Services;

public static class CommissionCalculator
{
    public const decimal MaxMoney = 999999999999.99m;
    public static bool IsValidAmount(decimal amount) =>
        amount > 0 && amount <= MaxMoney && decimal.Round(amount, 2) == amount;
    public static bool IsValidPercent(decimal percent) => percent > 0 && percent <= 100;
    public static decimal Calculate(decimal amount, decimal percent)
    {
        if (!IsValidAmount(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!IsValidPercent(percent)) throw new ArgumentOutOfRangeException(nameof(percent));
        return decimal.Round(amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
