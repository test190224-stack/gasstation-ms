using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;

namespace GasStationMS.Controllers
{
    [Authorize(Roles = "Administrator,NetworkManager,Manager")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeesController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Employees
        public async Task<IActionResult> Index(int? stationId, bool includeInactive = false)
        {
            var q = _db.Employees.Include(e => e.Station).AsQueryable();
            if (!includeInactive) q = q.Where(e => e.IsActive);
            if (stationId.HasValue) q = q.Where(e => e.StationId == stationId.Value);

            ViewBag.Stations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            ViewBag.SelectedStationId = stationId;
            ViewBag.IncludeInactive = includeInactive;

            var list = await q.OrderBy(e => e.LastName).ToListAsync();
            return View(list);
        }

        // GET: /Employees/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.Station)
                .Include(e => e.Shifts.OrderByDescending(s => s.StartedAt).Take(10))
                .FirstOrDefaultAsync(e => e.Id == id);
            if (emp == null) return NotFound();

            ViewBag.TotalShifts = await _db.Shifts.CountAsync(s => s.EmployeeId == id);
            ViewBag.TotalSales = await _db.Sales
                .CountAsync(s => s.Shift != null && s.Shift.EmployeeId == id);
            ViewBag.TotalRevenue = await _db.Sales
                .Where(s => s.Shift != null && s.Shift.EmployeeId == id)
                .SumAsync(s => (decimal?)s.NetAmount) ?? 0m;

            return View(emp);
        }

        // GET: /Employees/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Employee { IsActive = true, BaseSalary = 150000m });
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Stations = await _db.Stations
                .Where(s => s.Status != StationStatus.Closed)
                .OrderBy(s => s.Name).ToListAsync();
        }

        // POST: /Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string? email, string? password)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(employee);
            }

            // Optionally create login account
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    ModelState.AddModelError("", "Այս էլ․ փոստով հաշիվ արդեն գրանցված է");
                    await LoadDropdownsAsync();
                    return View(employee);
                }

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = $"{employee.LastName} {employee.FirstName}",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    foreach (var err in result.Errors)
                        ModelState.AddModelError("", err.Description);
                    await LoadDropdownsAsync();
                    return View(employee);
                }

                // Assign role based on employee role
                var roleName = employee.Role.ToString();
                if (await _roleManager.RoleExistsAsync(roleName))
                    await _userManager.AddToRoleAsync(user, roleName);

                employee.UserId = user.Id;
                employee.Email = email;
            }

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            // Link back user → employee
            if (!string.IsNullOrEmpty(employee.UserId))
            {
                var u = await _userManager.FindByIdAsync(employee.UserId);
                if (u != null) { u.EmployeeId = employee.Id; await _userManager.UpdateAsync(u); }
            }

            TempData["Success"] = $"✅ Աշխատակից «{employee.FullName}» գրանցվեց";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Employees/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            await LoadDropdownsAsync();
            return View(emp);
        }

        // POST: /Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(employee);
            }

            _db.Update(employee);
            await _db.SaveChangesAsync();

            // Sync user role if linked
            if (!string.IsNullOrEmpty(employee.UserId))
            {
                var user = await _userManager.FindByIdAsync(employee.UserId);
                if (user != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    var newRole = employee.Role.ToString();
                    if (await _roleManager.RoleExistsAsync(newRole))
                        await _userManager.AddToRoleAsync(user, newRole);
                }
            }

            TempData["Success"] = "✅ Փոփոխությունները պահպանվեցին";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Employees/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,NetworkManager")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp == null) return NotFound();

            emp.IsActive = false;
            emp.TerminatedAt = System.DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Աշխատակից «{emp.FullName}» ապաակտիվացվեց";
            return RedirectToAction(nameof(Index));
        }
    }
}
