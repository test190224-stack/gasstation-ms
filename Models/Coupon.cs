using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    /// <summary>
    /// Կտրոն (coupon) — նախավճարված կամ զեղչի կտրոն
    /// </summary>
    public class Coupon
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string Code { get; set; } = string.Empty;

        public CouponType Type { get; set; }

        public int? FuelTypeId { get; set; }
        public FuelType? FuelType { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? VolumeLiters { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal? FaceValue { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercentage { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public CouponStatus Status { get; set; } = CouponStatus.Active;

        [StringLength(100)]
        public string? IssuedTo { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    public enum CouponType
    {
        PrepaidVolume = 1,    // Նախավճարված լիտրերով
        PrepaidAmount = 2,    // Նախավճարված գումարով
        PercentDiscount = 3,  // %-ային զեղչ
        CorporateAccount = 4  // Կորպորատիվ հաշիվ
    }

    public enum CouponStatus
    {
        Active = 1,
        Used = 2,
        Expired = 3,
        Cancelled = 4
    }
}
