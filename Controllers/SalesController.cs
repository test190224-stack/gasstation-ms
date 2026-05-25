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
    [Authorize(Roles = "Administrator,Manager,Operator")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISalesService _sales;

        public SalesController(ApplicationDbContext db, ISalesService sales)
        {
            _db = db;
            _sales = sales;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddDays(-7);
            var t = to ?? DateTime.Today.AddDays(1);
            var sales = await _db.Sales
                .Include(s => s.FuelType).Include(s => s.Station)
                .Include(s => s.Customer)
                .Where(s => s.SoldAt >= f && s.SoldAt <= t)
                .OrderByDescending(s => s.SoldAt).Take(500).ToListAsync();
            ViewBag.From = f;
            ViewBag.To = t;
            return View(sales);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Sale { PricePerLiter = 500m, VolumeLiters = 20m });
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Dispensers = await _db.Dispensers
                .Include(d => d.FuelTank).ThenInclude(t => t!.FuelType)
                .Include(d => d.Station)
                .Where(d => d.IsOperational)
                .ToListAsync();
            ViewBag.FuelTypes = await _db.FuelTypes.Where(f => f.IsActive).ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Sale sale, string? couponCode, string? customerCardCode, int pointsToUse = 0)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(sale);
            }

            // Find active shift
            var currentShift = await _db.Shifts
                .Where(s => s.Status == ShiftStatus.Open)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
            if (currentShift == null)
            {
                ModelState.AddModelError("", "Ոչ մի գործող հերթափոխ չկա։ Խնդրում ենք սկսել հերթափոխ։");
                await LoadDropdownsAsync();
                return View(sale);
            }
            sale.ShiftId = currentShift.Id;
            sale.StationId = currentShift.StationId;

            try
            {
                await _sales.RegisterSaleAsync(sale, couponCode, customerCardCode, pointsToUse);
                TempData["Success"] = $"✅ Վաճառքը գրանցվեց հաջողությամբ՝ {sale.ReceiptNumber} ({sale.NetAmount:N0} ֏)";
                if (sale.BonusPointsEarned > 0)
                    TempData["Info"] = $"🎁 Հաճախորդը ստացավ {sale.BonusPointsEarned} բոնուս միավոր";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadDropdownsAsync();
                return View(sale);
            }
        }

        /// <summary>AJAX — validate coupon code before sale</summary>
        [HttpGet]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            var coupon = await _sales.ValidateCouponAsync(code);
            if (coupon == null)
                return Json(new { valid = false, message = "Կ. չի գ. կամ ժ-անց" });

            var summary = coupon.Type switch
            {
                GasStationMS.Models.CouponType.PrepaidVolume   => $"{coupon.VolumeLiters} լ",
                GasStationMS.Models.CouponType.PrepaidAmount   => $"{coupon.FaceValue:N0} ֏",
                GasStationMS.Models.CouponType.PercentDiscount => $"{coupon.DiscountPercentage}%",
                _ => ""
            };

            return Json(new
            {
                valid = true,
                type = (int)coupon.Type,
                summary,
                discountPct = coupon.DiscountPercentage,
                faceValue = coupon.FaceValue,
                volumeLiters = coupon.VolumeLiters,
                expiresAt = coupon.ExpiresAt.ToString("yyyy-MM-dd")
            });
        }

        /// <summary>AJAX endpoint — lookup customer by card code</summary>
        [HttpGet]
        public async Task<IActionResult> LookupCustomer(string code)
        {
            var c = await _sales.FindCustomerByCardAsync(code);
            if (c == null) return Json(new { found = false });
            return Json(new
            {
                found = true,
                name = $"{c.LastName} {c.FirstName}",
                tier = c.Tier.ToString(),
                points = c.BonusPoints,
                cashback = c.CashbackPercent
            });
        }

        /// <summary>AJAX endpoint — get dispenser info (fuel type + price)</summary>
        [HttpGet]
        public async Task<IActionResult> GetDispenserInfo(int id)
        {
            var d = await _db.Dispensers
                .Include(x => x.FuelTank).ThenInclude(t => t!.FuelType)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (d == null || d.FuelTank?.FuelType == null) return Json(null);
            return Json(new
            {
                fuelTypeId = d.FuelTank.FuelType.Id,
                fuelTypeName = d.FuelTank.FuelType.Name,
                price = d.FuelTank.FuelType.PricePerLiter,
                availableLiters = d.FuelTank.CurrentVolumeLiters
            });
        }
    }
}
