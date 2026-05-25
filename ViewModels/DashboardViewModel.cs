using System;
using System.Collections.Generic;
using GasStationMS.Models;

namespace GasStationMS.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransaction { get; set; }
        public decimal CurrentFuelVolume { get; set; }
        public int LowStockTankCount { get; set; }
        public int CustomerCount { get; set; }
        public int ActiveCouponCount { get; set; }

        public List<FuelTypeSales> SalesByFuelType { get; set; } = new();
        public List<DailySalesPoint> DailySales { get; set; } = new();
        public List<StationSales> SalesByStation { get; set; } = new();
        public List<PaymentMethodStat> SalesByPaymentMethod { get; set; } = new();
        public List<TopCustomer> TopCustomers { get; set; } = new();
        public List<FuelDepletion> FuelDepletionForecast { get; set; } = new();
        /// <summary>7 days × 24 hours transaction count. Outer = DayOfWeek (0=Sun), Inner = hour.</summary>
        public int[][] HourlyHeatmap { get; set; } = Enumerable.Range(0, 7)
            .Select(_ => new int[24]).ToArray();
    }

    public class FuelTypeSales
    {
        public string FuelTypeName { get; set; } = string.Empty;
        public decimal Volume { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailySalesPoint
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Volume { get; set; }
    }

    public class StationSales
    {
        public string StationName { get; set; } = string.Empty;
        public decimal Regular { get; set; }
        public decimal Premium { get; set; }
        public decimal Super { get; set; }
        public decimal Diesel { get; set; }
        public decimal LPG { get; set; }
        public decimal Total => Regular + Premium + Super + Diesel + LPG;
    }

    public class PaymentMethodStat
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopCustomer
    {
        public string FullName { get; set; } = string.Empty;
        public string CardCode { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int BonusPoints { get; set; }
        public int VisitCount { get; set; }
    }

    public class FuelDepletion
    {
        public string TankCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string FuelTypeName { get; set; } = string.Empty;
        public decimal CurrentVolume { get; set; }
        public decimal DailyAverageConsumption { get; set; }
        public double? DaysUntilEmpty { get; set; }
        public DateTime? EstimatedEmptyDate { get; set; }
    }

    public class SalesReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<Sale> Sales { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalProfit { get; set; }
    }
}
