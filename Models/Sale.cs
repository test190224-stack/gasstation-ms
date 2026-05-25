using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GasStationMS.Models
{
    /// <summary>
    /// Վաճառքի գործարք (transaction)
    /// </summary>
    public class Sale
    {
        public int Id { get; set; }

        // ReceiptNumber auto-generated in service, not required from user
        [StringLength(30)]
        [ValidateNever]
        public string ReceiptNumber { get; set; } = string.Empty;

        public int StationId { get; set; }
        [ValidateNever] public Station? Station { get; set; }

        public int ShiftId { get; set; }
        [ValidateNever] public Shift? Shift { get; set; }

        public int DispenserId { get; set; }
        [ValidateNever] public Dispenser? Dispenser { get; set; }

        public int FuelTypeId { get; set; }
        [ValidateNever] public FuelType? FuelType { get; set; }

        [Range(0.01, 10000, ErrorMessage = "Ծավալը պետք է լինի 0.01-10000 լիտր միջակայքում")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal VolumeLiters { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Գինը սխալ է")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerLiter { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal NetAmount { get; set; }

        /// <summary>Weighted average cost per liter at time of sale (FIFO-derived)</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal CostPerLiter { get; set; }

        /// <summary>Profit = (PricePerLiter - CostPerLiter) * VolumeLiters - DiscountAmount</summary>
        [Column(TypeName = "decimal(14,2)")]
        public decimal Profit { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public int? CouponId { get; set; }
        [ValidateNever] public Coupon? Coupon { get; set; }

        public int? CustomerId { get; set; }
        [ValidateNever] public Customer? Customer { get; set; }

        /// <summary>Bonus points earned in this sale</summary>
        public int BonusPointsEarned { get; set; }

        /// <summary>Bonus points used as discount</summary>
        public int BonusPointsUsed { get; set; }

        public DateTime SoldAt { get; set; } = DateTime.UtcNow;

        [StringLength(250)]
        public string? Notes { get; set; }
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Card = 2,
        Coupon = 3,
        Corporate = 4,
        Mixed = 5,
        LoyaltyCard = 6
    }
}
