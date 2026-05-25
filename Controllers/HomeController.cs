using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;

namespace GasStationMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db) => _db = db;

        // Landing page — կարող է տեսնել ով ուզում է
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Եթե օգտատերը authorized է, ուղարկենք dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction(nameof(Dashboard));
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.StationCount = await _db.Stations.CountAsync();
            ViewBag.EmployeeCount = await _db.Employees.CountAsync(e => e.IsActive);
            ViewBag.TodayRevenue = await _db.Sales
                .Where(s => s.SoldAt >= DateTime.Today)
                .SumAsync(s => (decimal?)s.NetAmount) ?? 0m;
            ViewBag.LowStockCount = await _db.FuelTanks
                .CountAsync(t => t.CurrentVolumeLiters <= t.MinThresholdLiters);
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
