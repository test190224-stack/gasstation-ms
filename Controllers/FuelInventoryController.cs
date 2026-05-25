using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;
using GasStationMS.Services;

namespace GasStationMS.Controllers
{
    [Authorize]
    public class FuelInventoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFuelInventoryService _service;

        public FuelInventoryController(ApplicationDbContext db, IFuelInventoryService service)
        {
            _db = db;
            _service = service;
        }

        public async Task<IActionResult> Index(int? stationId)
        {
            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.SelectedStationId = stationId;

            var tanks = stationId.HasValue
                ? await _service.GetTanksByStationAsync(stationId.Value)
                : await _db.FuelTanks
                    .Include(t => t.FuelType)
                    .Include(t => t.Station)
                    .Include(t => t.Deliveries)   // needed for WeightedAverageCost
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Station!.Name).ThenBy(t => t.TankCode)
                    .ToListAsync();

            return View(tanks);
        }

        public async Task<IActionResult> LowStock()
        {
            var tanks = await _service.GetLowStockTanksAsync();
            return View(tanks);
        }

        [Authorize(Roles = "Administrator,NetworkManager,Manager")]
        public async Task<IActionResult> RegisterDelivery(int tankId)
        {
            var tank = await _db.FuelTanks
                .Include(t => t.FuelType)
                .Include(t => t.Station)
                .FirstOrDefaultAsync(t => t.Id == tankId);
            if (tank == null) return NotFound();

            ViewBag.Tank = tank;
            return View(new FuelDelivery { FuelTankId = tankId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager,Manager")]
        public async Task<IActionResult> RegisterDelivery(FuelDelivery delivery)
        {
            async Task ReloadTankAsync()
            {
                ViewBag.Tank = await _db.FuelTanks
                    .Include(t => t.FuelType)
                    .Include(t => t.Station)
                    .FirstOrDefaultAsync(t => t.Id == delivery.FuelTankId);
            }

            if (!ModelState.IsValid)
            {
                await ReloadTankAsync();
                return View(delivery);
            }
            try
            {
                await _service.RegisterDeliveryAsync(delivery);
                TempData["Success"] = $"Մատակարարումը գրանցվեց՝ {delivery.VolumeLiters} լ";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await ReloadTankAsync();
                return View(delivery);
            }
        }
    }
}
