using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;
using GasStationMS.ViewModels;

namespace GasStationMS.Controllers
{
    [Authorize(Roles = "Administrator,NetworkManager,Manager,Accountant")]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SuppliersController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            // Fetch all active suppliers with their deliveries eagerly loaded,
            // then build a strongly-typed summary list in memory.
            var suppliers = await _db.Suppliers
                .Where(s => s.IsActive)
                .Include(s => s.Deliveries)
                .ToListAsync();

            var summary = suppliers
                .Select(s => new SupplierSummaryViewModel
                {
                    Supplier = s,
                    DeliveryCount = s.Deliveries.Count,
                    TotalVolume = s.Deliveries.Sum(d => d.VolumeLiters),
                    TotalSpent = s.Deliveries.Sum(d => d.TotalCost),
                    LastDelivery = s.Deliveries.Any()
                        ? s.Deliveries.Max(d => d.DeliveredAt)
                        : (DateTime?)null
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();

            return View(summary);
        }

        public async Task<IActionResult> Details(int id)
        {
            var s = await _db.Suppliers
                .Include(x => x.Deliveries).ThenInclude(d => d.FuelTank).ThenInclude(t => t!.FuelType)
                .Include(x => x.Deliveries).ThenInclude(d => d.FuelTank).ThenInclude(t => t!.Station)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            ViewBag.TotalDeliveries = s.Deliveries.Count;
            ViewBag.TotalVolume = s.Deliveries.Sum(d => d.VolumeLiters);
            ViewBag.TotalSpent = s.Deliveries.Sum(d => d.TotalCost);
            ViewBag.AvgPrice = s.Deliveries.Any() && s.Deliveries.Sum(d => d.VolumeLiters) > 0
                ? s.Deliveries.Sum(d => d.TotalCost) / s.Deliveries.Sum(d => d.VolumeLiters)
                : 0m;

            // Monthly breakdown for chart
            var monthly = s.Deliveries
                .GroupBy(d => new { d.DeliveredAt.Year, d.DeliveredAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    volume = g.Sum(d => d.VolumeLiters),
                    cost = g.Sum(d => d.TotalCost)
                }).ToList();
            ViewBag.MonthlyData = monthly;

            return View(s);
        }

        public IActionResult Create() => View(new Supplier { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (!ModelState.IsValid) return View(supplier);
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"✅ Մատակարար «{supplier.Name}» գրանցվեց";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();
            if (!ModelState.IsValid) return View(supplier);
            _db.Update(supplier);
            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Պահպանվեց";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Report: purchases by supplier in date range</summary>
        public async Task<IActionResult> Report(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddMonths(-3);
            var t = to ?? DateTime.Today.AddDays(1);

            var suppliers = await _db.Suppliers
                .Where(s => s.IsActive)
                .Include(s => s.Deliveries)
                .ToListAsync();

            var rows = suppliers.Select(s =>
            {
                var deliveries = s.Deliveries.Where(d => d.DeliveredAt >= f && d.DeliveredAt <= t).ToList();
                var totalVolume = deliveries.Sum(d => d.VolumeLiters);
                var totalCost = deliveries.Sum(d => d.TotalCost);
                return new SupplierReportRow
                {
                    Id = s.Id,
                    Name = s.Name,
                    DeliveryCount = deliveries.Count,
                    TotalVolume = totalVolume,
                    TotalCost = totalCost,
                    AvgPricePerLiter = totalVolume > 0 ? totalCost / totalVolume : 0m
                };
            })
            .OrderByDescending(x => x.TotalCost)
            .ToList();

            ViewBag.From = f;
            ViewBag.To = t;
            ViewBag.TotalCost = rows.Sum(x => x.TotalCost);
            ViewBag.TotalVolume = rows.Sum(x => x.TotalVolume);
            return View(rows);
        }
    }
}
