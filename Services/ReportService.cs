using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using GasStationMS.Data;
using GasStationMS.Models;
using GasStationMS.ViewModels;

namespace GasStationMS.Services
{
    public interface IReportService
    {
        Task<DashboardViewModel> GetDashboardAsync(int? stationId, DateTime from, DateTime to);
        Task<byte[]> ExportSalesToExcelAsync(DateTime from, DateTime to, int? stationId = null);
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime from, DateTime to, int? stationId = null);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<DashboardViewModel> GetDashboardAsync(
            int? stationId, DateTime from, DateTime to)
        {
            var salesQ = _db.Sales.Where(s => s.SoldAt >= from && s.SoldAt <= to);
            if (stationId.HasValue) salesQ = salesQ.Where(s => s.StationId == stationId.Value);

            var sales = await salesQ
                .Include(s => s.FuelType).Include(s => s.Station)
                .Include(s => s.Customer)
                .ToListAsync();

            var tanksQ = _db.FuelTanks.Where(t => t.IsActive);
            if (stationId.HasValue) tanksQ = tanksQ.Where(t => t.StationId == stationId.Value);
            var tanks = await tanksQ
                .Include(t => t.FuelType).Include(t => t.Station)
                .ToListAsync();

            // ===== Hourly heatmap — jagged array int[][] for JSON serialization =====
            var heatmap = Enumerable.Range(0, 7).Select(_ => new int[24]).ToArray();
            foreach (var s in sales)
            {
                int dow = (int)s.SoldAt.DayOfWeek; // 0=Sunday, 6=Saturday
                int hour = s.SoldAt.Hour;
                heatmap[dow][hour]++;
            }

            // ===== Station × fuel type stacked bars =====
            var byStation = sales
                .GroupBy(s => s.Station?.Name ?? "—")
                .Select(g => new StationSales
                {
                    StationName = g.Key,
                    Regular = g.Where(s => s.FuelType?.Code == "A92").Sum(s => s.NetAmount),
                    Premium = g.Where(s => s.FuelType?.Code == "A95").Sum(s => s.NetAmount),
                    Super = g.Where(s => s.FuelType?.Code == "A98").Sum(s => s.NetAmount),
                    Diesel = g.Where(s => s.FuelType?.Code == "DSL").Sum(s => s.NetAmount),
                    LPG = g.Where(s => s.FuelType?.Code == "LPG").Sum(s => s.NetAmount),
                }).ToList();

            // ===== Payment methods =====
            var paymentStats = sales
                .GroupBy(s => s.PaymentMethod)
                .Select(g => new PaymentMethodStat
                {
                    Method = PaymentLabel(g.Key),
                    Count = g.Count(),
                    Amount = g.Sum(s => s.NetAmount)
                }).ToList();

            // ===== Top 10 customers =====
            var topCustomers = await _db.Customers
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .Select(c => new TopCustomer
                {
                    FullName = c.LastName + " " + c.FirstName,
                    CardCode = c.CardCode,
                    Tier = c.Tier.ToString(),
                    TotalSpent = c.TotalSpent,
                    BonusPoints = c.BonusPoints,
                    VisitCount = c.Sales.Count()
                }).ToListAsync();

            // ===== Fuel depletion forecast =====
            var recentDays = 14; // use last 14 days for avg daily consumption
            var recentStart = DateTime.UtcNow.AddDays(-recentDays);
            var recentSales = await _db.Sales
                .Where(s => s.SoldAt >= recentStart)
                .GroupBy(s => s.Dispenser!.FuelTankId)
                .Select(g => new { TankId = g.Key, DailyAvg = g.Sum(s => s.VolumeLiters) / recentDays })
                .ToListAsync();

            var depletion = tanks.Select(t =>
            {
                var avgDaily = recentSales.FirstOrDefault(r => r.TankId == t.Id)?.DailyAvg ?? 0m;
                double? days = avgDaily > 0 ? (double)(t.CurrentVolumeLiters / avgDaily) : null;
                return new FuelDepletion
                {
                    TankCode = t.TankCode,
                    StationName = t.Station?.Name ?? "",
                    FuelTypeName = t.FuelType?.Name ?? "",
                    CurrentVolume = t.CurrentVolumeLiters,
                    DailyAverageConsumption = avgDaily,
                    DaysUntilEmpty = days,
                    EstimatedEmptyDate = days.HasValue
                        ? DateTime.UtcNow.AddDays(days.Value) : null
                };
            })
            .OrderBy(d => d.DaysUntilEmpty ?? double.MaxValue)
            .ToList();

            return new DashboardViewModel
            {
                TotalRevenue = sales.Sum(s => s.NetAmount),
                TotalVolume = sales.Sum(s => s.VolumeLiters),
                TotalProfit = sales.Sum(s => s.Profit),
                TransactionCount = sales.Count,
                AverageTransaction = sales.Any() ? sales.Average(s => s.NetAmount) : 0,
                CurrentFuelVolume = tanks.Sum(t => t.CurrentVolumeLiters),
                LowStockTankCount = tanks.Count(t => t.IsLowStock),
                CustomerCount = await _db.Customers.CountAsync(c => c.IsActive),
                ActiveCouponCount = await _db.Coupons.CountAsync(c => c.Status == CouponStatus.Active),
                SalesByFuelType = sales.GroupBy(s => s.FuelType?.Name ?? "—")
                    .Select(g => new FuelTypeSales
                    {
                        FuelTypeName = g.Key,
                        Volume = g.Sum(s => s.VolumeLiters),
                        Revenue = g.Sum(s => s.NetAmount)
                    }).ToList(),
                DailySales = sales.GroupBy(s => s.SoldAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailySalesPoint
                    {
                        Date = g.Key,
                        Revenue = g.Sum(s => s.NetAmount),
                        Volume = g.Sum(s => s.VolumeLiters)
                    }).ToList(),
                SalesByStation = byStation,
                SalesByPaymentMethod = paymentStats,
                TopCustomers = topCustomers,
                FuelDepletionForecast = depletion,
                HourlyHeatmap = heatmap
            };
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(
            DateTime from, DateTime to, int? stationId = null)
        {
            var q = _db.Sales
                .Include(s => s.FuelType).Include(s => s.Station)
                .Include(s => s.Shift).ThenInclude(sh => sh!.Employee)
                .Include(s => s.Customer)
                .Where(s => s.SoldAt >= from && s.SoldAt <= to);
            if (stationId.HasValue) q = q.Where(s => s.StationId == stationId.Value);

            var items = await q.OrderByDescending(s => s.SoldAt).ToListAsync();
            return new SalesReportViewModel
            {
                From = from, To = to,
                Sales = items,
                TotalAmount = items.Sum(s => s.NetAmount),
                TotalVolume = items.Sum(s => s.VolumeLiters),
                TotalProfit = items.Sum(s => s.Profit)
            };
        }

        public async Task<byte[]> ExportSalesToExcelAsync(
            DateTime from, DateTime to, int? stationId = null)
        {
            var report = await GetSalesReportAsync(from, to, stationId);
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Վաճառքներ");
            string[] headers = { "Չեկ N", "Ամսաթիվ", "Կայան", "Վառելիք",
                "Լիտր", "Գին/լ", "Գումար", "Զեղչ", "Վճարված", "Շահույթ", "Վճարում", "Հաճախորդ" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
            }
            int row = 2;
            foreach (var s in report.Sales)
            {
                ws.Cells[row, 1].Value = s.ReceiptNumber;
                ws.Cells[row, 2].Value = s.SoldAt.ToString("yyyy-MM-dd HH:mm");
                ws.Cells[row, 3].Value = s.Station?.Name;
                ws.Cells[row, 4].Value = s.FuelType?.Name;
                ws.Cells[row, 5].Value = s.VolumeLiters;
                ws.Cells[row, 6].Value = s.PricePerLiter;
                ws.Cells[row, 7].Value = s.TotalAmount;
                ws.Cells[row, 8].Value = s.DiscountAmount;
                ws.Cells[row, 9].Value = s.NetAmount;
                ws.Cells[row, 10].Value = s.Profit;
                ws.Cells[row, 11].Value = PaymentLabel(s.PaymentMethod);
                ws.Cells[row, 12].Value = s.Customer != null
                    ? $"{s.Customer.LastName} {s.Customer.FirstName}" : "—";
                row++;
            }
            ws.Cells[row, 6].Value = "ԸՆԴԱՄԵՆԸ՝";
            ws.Cells[row, 6].Style.Font.Bold = true;
            ws.Cells[row, 9].Value = report.TotalAmount;
            ws.Cells[row, 9].Style.Font.Bold = true;
            ws.Cells[row, 10].Value = report.TotalProfit;
            ws.Cells[row, 10].Style.Font.Bold = true;
            ws.Cells.AutoFitColumns();
            return pkg.GetAsByteArray();
        }

        private static string PaymentLabel(PaymentMethod m) => m switch
        {
            PaymentMethod.Cash          => "Կանխիկ",
            PaymentMethod.Card          => "Քարտ",
            PaymentMethod.Coupon        => "Կտրոն",
            PaymentMethod.Corporate     => "Կորպորատիվ",
            PaymentMethod.Mixed         => "Խառն",
            PaymentMethod.LoyaltyCard   => "Loyalty քարտ",
            _                           => m.ToString()
        };
    }
}
