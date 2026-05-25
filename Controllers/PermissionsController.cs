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
    [Authorize(Roles = "Administrator")]
    public class PermissionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPermissionService _perms;

        public PermissionsController(ApplicationDbContext db, IPermissionService perms)
        {
            _db = db;
            _perms = perms;
        }

        // GET: /Permissions — list all employees with their roles
        public async Task<IActionResult> Index()
        {
            var employees = await _db.Employees
                .Include(e => e.Station)
                .Where(e => e.IsActive)
                .OrderBy(e => e.LastName)
                .ToListAsync();
            return View(employees);
        }

        // GET: /Permissions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.Station)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (emp == null) return NotFound();

            var currentPerms = await _perms.GetEmployeePermissionsAsync(id);
            var roleDefaults = Permissions.DefaultsForRole(emp.Role).ToHashSet();

            ViewBag.Employee = emp;
            ViewBag.CurrentPerms = currentPerms;
            ViewBag.RoleDefaults = roleDefaults;
            ViewBag.AllPermissions = Permissions.All;
            return View();
        }

        // POST: /Permissions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] granted)
        {
            await _perms.SetEmployeePermissionsAsync(id, granted.ToList());

            var emp = await _db.Employees.FindAsync(id);
            TempData["Success"] = $"✅ {emp?.FullName}-ի թույլտվությունները թարմացվեցին";
            return RedirectToAction(nameof(Index));
        }
    }
}
