using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;

namespace GasStationMS.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
        Task<List<string>> GetUserPermissionsAsync(ClaimsPrincipal user);
        Task<List<string>> GetEmployeePermissionsAsync(int employeeId);
        Task SetEmployeePermissionsAsync(int employeeId, List<string> granted);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _db;

        public PermissionService(ApplicationDbContext db) => _db = db;

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            // Admins always have everything
            if (user.IsInRole("Administrator")) return true;

            var perms = await GetUserPermissionsAsync(user);
            return perms.Contains(permission);
        }

        public async Task<List<string>> GetUserPermissionsAsync(ClaimsPrincipal user)
        {
            if (user.IsInRole("Administrator"))
                return Permissions.All.Select(p => p.Key).ToList();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return new();

            var appUser = await _db.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (appUser?.Employee == null) return new();

            return await GetEmployeePermissionsAsync(appUser.Employee.Id);
        }

        public async Task<List<string>> GetEmployeePermissionsAsync(int employeeId)
        {
            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee == null) return new();

            // Start with role defaults
            var defaults = Permissions.DefaultsForRole(employee.Role).ToHashSet();

            // Apply overrides stored in DB
            var overrides = await _db.EmployeePermissions
                .Where(p => p.EmployeeId == employeeId)
                .ToListAsync();

            foreach (var ov in overrides)
            {
                if (ov.IsGranted)
                    defaults.Add(ov.Permission);
                else
                    defaults.Remove(ov.Permission);
            }

            return defaults.ToList();
        }

        public async Task SetEmployeePermissionsAsync(int employeeId, List<string> granted)
        {
            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee == null) return;

            var roleDefaults = Permissions.DefaultsForRole(employee.Role).ToHashSet();

            // Remove existing overrides for this employee
            var existing = _db.EmployeePermissions
                .Where(p => p.EmployeeId == employeeId);
            _db.EmployeePermissions.RemoveRange(existing);

            // Add overrides only where they differ from role defaults
            var allKeys = Permissions.All.Select(p => p.Key).ToList();
            foreach (var key in allKeys)
            {
                var inGranted = granted.Contains(key);
                var inDefault = roleDefaults.Contains(key);

                if (inGranted != inDefault)
                {
                    _db.EmployeePermissions.Add(new EmployeePermission
                    {
                        EmployeeId = employeeId,
                        Permission = key,
                        IsGranted = inGranted
                    });
                }
            }
            await _db.SaveChangesAsync();
        }
    }
}
