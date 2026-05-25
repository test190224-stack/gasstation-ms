using System;
using System.Collections.Generic;
using GasStationMS.Models;

namespace GasStationMS.ViewModels
{
    public class WalletViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // ===== Summary =====
        public decimal TotalRevenue { get; set; }       // from sales
        public decimal TotalFuelCost { get; set; }      // paid to suppliers
        public decimal TotalSalaries { get; set; }      // salaries paid
        public decimal TotalOtherExpenses { get; set; } // other
        public decimal TotalDiscounts { get; set; }     // coupons + loyalty

        public decimal GrossProfit      => TotalRevenue - TotalFuelCost;
        public decimal OperatingExpenses => TotalSalaries + TotalOtherExpenses;
        public decimal NetProfit         => GrossProfit - OperatingExpenses - TotalDiscounts;
        public decimal ProfitMarginPct   => TotalRevenue > 0
            ? Math.Round(NetProfit / TotalRevenue * 100m, 1) : 0m;

        // ===== Detail lists =====
        public List<FinancialTransaction> RecentTransactions { get; set; } = new();
        public List<MonthlySummary> MonthlySummaries { get; set; } = new();
        public List<CategoryBreakdown> ExpenseBreakdown { get; set; } = new();
        public List<SalaryEntry> SalaryEntries { get; set; } = new();
    }

    public class MonthlySummary
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit => Revenue - Expenses;
    }

    public class CategoryBreakdown
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Color { get; set; } = "#667eea";
    }

    public class SalaryEntry
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? Reference { get; set; }
    }
}
