using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;
using GasStationMS.ViewModels;

namespace GasStationMS.Controllers
{
    [Authorize(Roles = "Administrator,NetworkManager,Manager,Accountant")]
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public WalletController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _userManager = um;
        }

        // GET: /Wallet
        public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? stationId)
        {
            var f = from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); // start of month
            var t = to ?? DateTime.Today.AddDays(1);

            var vm = await BuildWalletAsync(f, t, stationId);

            ViewBag.From = f;
            ViewBag.To = t;
            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.SelectedStationId = stationId;
            return View(vm);
        }

        // GET: /Wallet/Transactions
        public async Task<IActionResult> Transactions(DateTime? from, DateTime? to,
            FinancialTransactionType? type, int? stationId)
        {
            var f = from ?? DateTime.Today.AddDays(-30);
            var t = to ?? DateTime.Today.AddDays(1);

            var q = _db.FinancialTransactions
                .Include(x => x.Station).Include(x => x.Employee).Include(x => x.Supplier)
                .Where(x => x.OccurredAt >= f && x.OccurredAt <= t);

            if (type.HasValue) q = q.Where(x => x.Type == type.Value);
            if (stationId.HasValue) q = q.Where(x => x.StationId == stationId.Value);

            var list = await q.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync();

            ViewBag.From = f; ViewBag.To = t;
            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.SelectedStationId = stationId;
            ViewBag.SelectedType = type;
            ViewBag.TotalIncome = list.Where(x => x.Direction == TransactionDirection.Income).Sum(x => x.Amount);
            ViewBag.TotalExpense = list.Where(x => x.Direction == TransactionDirection.Expense).Sum(x => x.Amount);
            return View(list);
        }

        // GET: /Wallet/PaySalary — form
        [Authorize(Roles = "Administrator,NetworkManager,Accountant")]
        public async Task<IActionResult> PaySalary()
        {
            ViewBag.Employees = await _db.Employees
                .Include(e => e.Station)
                .Where(e => e.IsActive)
                .OrderBy(e => e.LastName).ToListAsync();
            return View();
        }

        // POST: /Wallet/PaySalary
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager,Accountant")]
        public async Task<IActionResult> PaySalary(int employeeId, decimal amount,
            string? reference, string? notes, DateTime? occuredAt)
        {
            var emp = await _db.Employees.Include(e => e.Station).FirstOrDefaultAsync(e => e.Id == employeeId);
            if (emp == null)
            {
                TempData["Error"] = "Աշխատակիցը չի գտնվել";
                return RedirectToAction(nameof(PaySalary));
            }

            var userId = _userManager.GetUserId(User);
            _db.FinancialTransactions.Add(new FinancialTransaction
            {
                Type = FinancialTransactionType.SalaryPayment,
                Direction = TransactionDirection.Expense,
                Description = $"Աշ. վարձ՝ {emp.FullName} ({emp.Role})",
                Amount = amount,
                StationId = emp.StationId,
                EmployeeId = employeeId,
                Reference = reference,
                Notes = notes,
                OccurredAt = occuredAt ?? DateTime.UtcNow,
                CreatedByUserId = userId
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Աշ. վարձ {amount:N0} ֏ վճարվեց {emp.FullName}-ին";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Wallet/AddExpense
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager,Manager,Accountant")]
        public async Task<IActionResult> AddExpense(string description, decimal amount,
            int? stationId, string? reference, string? notes, DateTime? occuredAt)
        {
            var userId = _userManager.GetUserId(User);
            _db.FinancialTransactions.Add(new FinancialTransaction
            {
                Type = FinancialTransactionType.OtherExpense,
                Direction = TransactionDirection.Expense,
                Description = description,
                Amount = amount,
                StationId = stationId,
                Reference = reference,
                Notes = notes,
                OccurredAt = occuredAt ?? DateTime.UtcNow,
                CreatedByUserId = userId
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"✅ Ծախս {amount:N0} ֏ գրանցվեց";
            return RedirectToAction(nameof(Index));
        }

        private async Task<WalletViewModel> BuildWalletAsync(DateTime from, DateTime to, int? stationId)
        {
            // --- Sales revenue from Sale table ---
            var salesQ = _db.Sales.Where(s => s.SoldAt >= from && s.SoldAt <= to);
            if (stationId.HasValue) salesQ = salesQ.Where(s => s.StationId == stationId.Value);
            var salesData = await salesQ.ToListAsync();

            var revenue = salesData.Sum(s => s.NetAmount);
            var discounts = salesData.Sum(s => s.DiscountAmount);

            // --- Fuel cost from FuelDelivery ---
            var delQ = _db.FuelDeliveries.Where(d => d.DeliveredAt >= from && d.DeliveredAt <= to);
            if (stationId.HasValue)
                delQ = delQ.Where(d => d.FuelTank!.StationId == stationId.Value);
            var fuelCost = await delQ.SumAsync(d => (decimal?)d.TotalCost) ?? 0m;

            // --- Financial transactions ---
            var txQ = _db.FinancialTransactions
                .Include(t => t.Employee).Include(t => t.Station).Include(t => t.Supplier)
                .Where(t => t.OccurredAt >= from && t.OccurredAt <= to);
            if (stationId.HasValue) txQ = txQ.Where(t => t.StationId == stationId.Value);
            var txList = await txQ.OrderByDescending(t => t.OccurredAt).Take(50).ToListAsync();

            var salaries = await txQ
                .Where(t => t.Type == FinancialTransactionType.SalaryPayment)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
            var otherExp = await txQ
                .Where(t => t.Type == FinancialTransactionType.OtherExpense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            // --- Monthly breakdown ---
            var allSales = await _db.Sales
                .Where(s => s.SoldAt >= from && s.SoldAt <= to)
                .ToListAsync();
            var allDel = await _db.FuelDeliveries
                .Where(d => d.DeliveredAt >= from && d.DeliveredAt <= to)
                .ToListAsync();

            var months = Enumerable
                .Range(0, (int)((to - from).TotalDays / 28) + 2)
                .Select(i => from.AddMonths(i))
                .TakeWhile(m => m <= to)
                .Select(m => new DateTime(m.Year, m.Month, 1))
                .Distinct().ToList();

            var monthlySummaries = months.Select(m =>
            {
                var nextM = m.AddMonths(1);
                var rev = allSales.Where(s => s.SoldAt >= m && s.SoldAt < nextM).Sum(s => s.NetAmount);
                var exp = allDel.Where(d => d.DeliveredAt >= m && d.DeliveredAt < nextM).Sum(d => d.TotalCost);
                return new MonthlySummary
                {
                    Month = m.ToString("yyyy-MM"),
                    Revenue = rev,
                    Expenses = exp
                };
            }).ToList();

            return new WalletViewModel
            {
                From = from, To = to,
                TotalRevenue = revenue,
                TotalFuelCost = fuelCost,
                TotalSalaries = salaries,
                TotalOtherExpenses = otherExp,
                TotalDiscounts = discounts,
                RecentTransactions = txList,
                MonthlySummaries = monthlySummaries,
                ExpenseBreakdown = new()
                {
                    new() { Category = "Վառելիքի գնում", Amount = fuelCost, Color = "#ef4444" },
                    new() { Category = "Աշ. վարձ", Amount = salaries, Color = "#f59e0b" },
                    new() { Category = "Այլ ծախսեր", Amount = otherExp, Color = "#8b5cf6" },
                    new() { Category = "Զեղչեր", Amount = discounts, Color = "#3b82f6" },
                },
                SalaryEntries = txList
                    .Where(t => t.Type == FinancialTransactionType.SalaryPayment)
                    .Select(t => new SalaryEntry
                    {
                        EmployeeName = t.Employee?.FullName ?? "—",
                        Role = t.Employee?.Role.ToString() ?? "",
                        Amount = t.Amount,
                        PaidAt = t.OccurredAt,
                        Reference = t.Reference
                    }).ToList()
            };
        }
    }
}
