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
    public class StationsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StationsController(ApplicationDbContext db) => _db = db;

        // GET: /Stations
        public async Task<IActionResult> Index()
        {
            var stations = await _db.Stations
                .Include(s => s.Tanks)
                .Include(s => s.Employees)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(stations);
        }

        // GET: /Stations/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var station = await _db.Stations
                .Include(s => s.Tanks).ThenInclude(t => t.FuelType)
                .Include(s => s.Dispensers)
                .Include(s => s.Employees)
                .Include(s => s.Shifts)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (station == null) return NotFound();
            return View(station);
        }

        // GET: /Stations/Create
        [Authorize(Roles = "Administrator,NetworkManager")]
        public IActionResult Create() => View();

        // POST: /Stations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager")]
        public async Task<IActionResult> Create(Station station)
        {
            if (!ModelState.IsValid) return View(station);

            _db.Stations.Add(station);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Կայան «{station.Name}» հաջողությամբ ստեղծվեց";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Stations/Edit/5
        [Authorize(Roles = "Administrator,NetworkManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var station = await _db.Stations.FindAsync(id);
            if (station == null) return NotFound();
            return View(station);
        }

        // POST: /Stations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager")]
        public async Task<IActionResult> Edit(int id, Station station)
        {
            if (id != station.Id) return NotFound();
            if (!ModelState.IsValid) return View(station);

            _db.Update(station);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Փոփոխությունները պահպանվեցին";
            return RedirectToAction(nameof(Index));
        }
    }
}
