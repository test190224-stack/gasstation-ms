using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;

namespace GasStationMS.Controllers
{
    [Authorize(Roles = "Administrator,NetworkManager,Manager")]
    public class FuelTanksController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FuelTanksController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Create(int? stationId)
        {
            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.FuelTypes = await _db.FuelTypes.Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync();

            var model = new FuelTank
            {
                StationId = stationId ?? 0,
                CapacityLiters = 20000m,
                MinThresholdLiters = 1500m,
                IsActive = true
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuelTank tank)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
                ViewBag.FuelTypes = await _db.FuelTypes.Where(f => f.IsActive).ToListAsync();
                return View(tank);
            }

            // Auto-generate tank code if empty
            if (string.IsNullOrWhiteSpace(tank.TankCode))
            {
                var count = await _db.FuelTanks.CountAsync() + 1;
                tank.TankCode = $"T-{count:D3}";
            }

            tank.CurrentVolumeLiters = 0;
            tank.LastUpdated = System.DateTime.UtcNow;
            _db.FuelTanks.Add(tank);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Ռեզերվուար «{tank.TankCode}» ստեղծվեց";
            return RedirectToAction("Index", "FuelInventory");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tank = await _db.FuelTanks.FindAsync(id);
            if (tank == null) return NotFound();
            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.FuelTypes = await _db.FuelTypes.Where(f => f.IsActive).ToListAsync();
            return View(tank);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FuelTank tank)
        {
            if (id != tank.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
                ViewBag.FuelTypes = await _db.FuelTypes.Where(f => f.IsActive).ToListAsync();
                return View(tank);
            }

            _db.Update(tank);
            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Փոփոխությունները պահպանվեցին";
            return RedirectToAction("Index", "FuelInventory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var tank = await _db.FuelTanks.FindAsync(id);
            if (tank == null) return NotFound();
            tank.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Ռեզերվուար «{tank.TankCode}» ապաակտիվացվեց";
            return RedirectToAction("Index", "FuelInventory");
        }
    }
}
