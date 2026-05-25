using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;

namespace GasStationMS.Services
{
    public interface ISalesService
    {
        Task<Sale> RegisterSaleAsync(Sale sale, string? couponCode, string? customerCardCode, int pointsToUse);
        Task<Coupon?> ValidateCouponAsync(string code);
        Task<Customer?> FindCustomerByCardAsync(string cardCode);
    }

    public class SalesService : ISalesService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFuelInventoryService _inventory;

        public SalesService(ApplicationDbContext db, IFuelInventoryService inventory)
        {
            _db = db;
            _inventory = inventory;
        }

        public async Task<Sale> RegisterSaleAsync(
            Sale sale, string? couponCode, string? customerCardCode, int pointsToUse)
        {
            // ---- 1. Validate dispenser + fuel availability ----
            var dispenser = await _db.Dispensers
                .Include(d => d.FuelTank).ThenInclude(t => t!.FuelType)
                .FirstOrDefaultAsync(d => d.Id == sale.DispenserId)
                ?? throw new InvalidOperationException("Բենզակոլոնկան չի գտնվել");

            if (!dispenser.IsOperational)
                throw new InvalidOperationException("Բենզակոլոնկան անջատված է");

            // ---- 2. FIFO consume fuel, get cost ----
            var costPerLiter = await _inventory.ConsumeFuelAsync(
                dispenser.FuelTankId, sale.VolumeLiters);
            if (costPerLiter == null)
                throw new InvalidOperationException(
                    "Ռեզերվուարում բավարար վառելիք չկա");

            sale.CostPerLiter = costPerLiter.Value;

            // ---- 2b. If user didn't override price, auto-calculate from cost + markup ----
            if (sale.PricePerLiter <= 0 && dispenser.FuelTank?.FuelType != null)
            {
                var markup = dispenser.FuelTank.FuelType.MarkupPercent;
                sale.PricePerLiter = Math.Round(
                    sale.CostPerLiter * (1m + markup / 100m), 0);
            }

            // ---- 3. Gross calculation ----
            sale.TotalAmount = sale.VolumeLiters * sale.PricePerLiter;
            sale.DiscountAmount = 0m;

            // ---- 4. Apply coupon OR customer loyalty (not both) ----
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await ValidateCouponAsync(couponCode);
                if (coupon != null)
                {
                    sale.CouponId = coupon.Id;
                    if (coupon.Type == CouponType.PercentDiscount && coupon.DiscountPercentage.HasValue)
                        sale.DiscountAmount = sale.TotalAmount * (coupon.DiscountPercentage.Value / 100m);
                    else if (coupon.Type == CouponType.PrepaidAmount && coupon.FaceValue.HasValue)
                        sale.DiscountAmount = Math.Min(coupon.FaceValue.Value, sale.TotalAmount);

                    coupon.Status = CouponStatus.Used;
                    coupon.UsedAt = DateTime.UtcNow;
                }
                else
                    throw new InvalidOperationException("Կտրոնը անվավեր է կամ ժամկետանց");
            }
            else if (!string.IsNullOrWhiteSpace(customerCardCode))
            {
                var customer = await FindCustomerByCardAsync(customerCardCode);
                if (customer == null)
                    throw new InvalidOperationException("Քարտը չի գտնվել");

                sale.CustomerId = customer.Id;

                // Use bonus points as discount (1 point = 1 ֏)
                if (pointsToUse > 0)
                {
                    if (pointsToUse > customer.BonusPoints)
                        throw new InvalidOperationException(
                            $"Հաշվին ունեք {customer.BonusPoints} միավոր, " +
                            $"բայց փորձում եք օգտագործել {pointsToUse}");
                    var maxDiscount = sale.TotalAmount;
                    var actualDiscount = Math.Min(pointsToUse, maxDiscount);
                    sale.DiscountAmount = actualDiscount;
                    sale.BonusPointsUsed = (int)actualDiscount;
                    customer.BonusPoints -= sale.BonusPointsUsed;
                }

                // Earn new bonus points based on tier (on net amount)
                var netBeforeEarning = sale.TotalAmount - sale.DiscountAmount;
                sale.BonusPointsEarned = (int)Math.Floor(
                    netBeforeEarning * customer.CashbackPercent / 100m);
                customer.BonusPoints += sale.BonusPointsEarned;

                // Update customer stats
                customer.TotalSpent += netBeforeEarning;
                customer.LastVisitAt = DateTime.UtcNow;
                customer.Tier = CalculateTier(customer.TotalSpent);

                if (sale.PaymentMethod == PaymentMethod.Cash)
                    sale.PaymentMethod = PaymentMethod.LoyaltyCard;
            }

            // ---- 5. Final calculations ----
            sale.NetAmount = sale.TotalAmount - sale.DiscountAmount;
            sale.Profit = (sale.PricePerLiter - sale.CostPerLiter) * sale.VolumeLiters
                          - sale.DiscountAmount;
            sale.ReceiptNumber = GenerateReceiptNumber();
            sale.SoldAt = DateTime.UtcNow;

            // Dispenser counter
            dispenser.TotalDispensedLiters += sale.VolumeLiters;

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();
            return sale;
        }

        public async Task<Coupon?> ValidateCouponAsync(string code)
        {
            var coupon = await _db.Coupons
                .Include(c => c.FuelType)
                .FirstOrDefaultAsync(c => c.Code == code);
            if (coupon == null || coupon.Status != CouponStatus.Active || coupon.IsExpired)
                return null;
            return coupon;
        }

        public async Task<Customer?> FindCustomerByCardAsync(string cardCode) =>
            await _db.Customers.FirstOrDefaultAsync(c =>
                c.CardCode == cardCode && c.IsActive);

        private static LoyaltyTier CalculateTier(decimal totalSpent) => totalSpent switch
        {
            <= 50_000m       => LoyaltyTier.Bronze,
            <= 200_000m      => LoyaltyTier.Silver,
            <= 1_000_000m    => LoyaltyTier.Gold,
            _                => LoyaltyTier.Platinum
        };

        private static string GenerateReceiptNumber() =>
            $"RCP-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}
