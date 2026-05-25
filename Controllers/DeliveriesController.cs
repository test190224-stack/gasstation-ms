using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;
using GasStationMS.Services;

namespace GasStationMS.Controllers
{
    [Authorize(Roles = "Administrator,NetworkManager,Manager,Accountant")]
    public class DeliveriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFuelInventoryService _service;

        public DeliveriesController(ApplicationDbContext db, IFuelInventoryService service)
        {
            _db = db;
            _service = service;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? stationId)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to ?? DateTime.Today.AddDays(1);

            var q = _db.FuelDeliveries
                .Include(d => d.FuelTank).ThenInclude(t => t!.Station)
                .Include(d => d.FuelTank).ThenInclude(t => t!.FuelType)
                .Include(d => d.Supplier)
                .Where(d => d.DeliveredAt >= f && d.DeliveredAt <= t);

            if (stationId.HasValue)
                q = q.Where(d => d.FuelTank!.StationId == stationId.Value);

            var list = await q.OrderByDescending(d => d.DeliveredAt).ToListAsync();

            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.SelectedStationId = stationId;
            ViewBag.From = f;
            ViewBag.To = t;
            ViewBag.TotalCost = list.Sum(d => d.TotalCost);
            ViewBag.TotalVolume = list.Sum(d => d.VolumeLiters);

            return View(list);
        }

        public async Task<IActionResult> Create(int? tankId = null)
        {
            await LoadDropdownsAsync();
            var model = new FuelDelivery
            {
                FuelTankId = tankId ?? 0,
                DeliveredAt = DateTime.Now
            };
            return View(model);
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Stations = await _db.Stations
                .Where(s => s.Status != StationStatus.Closed)
                .OrderBy(s => s.Name).ToListAsync();
            ViewBag.Tanks = await _db.FuelTanks
                .Include(t => t.Station).Include(t => t.FuelType)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Station!.Name).ThenBy(t => t.TankCode)
                .ToListAsync();
            ViewBag.Suppliers = await _db.Suppliers
                .Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuelDelivery delivery)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(delivery);
            }

            try
            {
                await _service.RegisterDeliveryAsync(delivery);
                TempData["Success"] = $"✅ Մատակարարումը գրանցվեց՝ {delivery.VolumeLiters:N1} լ, " +
                                      $"{delivery.TotalCost:N0} ֏";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadDropdownsAsync();
                return View(delivery);
            }
        }

        /// <summary>AJAX: get tank info when selected from dropdown</summary>
        [HttpGet]
        public async Task<IActionResult> GetTankInfo(int id)
        {
            var t = await _db.FuelTanks
                .Include(x => x.FuelType).Include(x => x.Station)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return Json(null);
            return Json(new
            {
                fuelType = t.FuelType?.Name,
                station = t.Station?.Name,
                current = t.CurrentVolumeLiters,
                capacity = t.CapacityLiters,
                available = t.CapacityLiters - t.CurrentVolumeLiters
            });
        }
    }
}
