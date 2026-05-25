using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    /// <summary>
    /// Հաճախորդ (քարտատեր) — loyalty-ի հաշվառման համար
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }

        /// <summary>Քարտի եզակի կոդ (barcode/QR-ի համար)</summary>
        [Required, StringLength(30)]
        public string CardCode { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        public DateTime? BirthDate { get; set; }

        /// <summary>Ընթացիկ բոնուս միավորներ (1 միավոր = 1 ֏ զեղչ)</summary>
        public int BonusPoints { get; set; }

        /// <summary>Ընդհանուր վաճառքի գումարը (tier-ի հաշվարկի համար)</summary>
        [Column(TypeName = "decimal(14,2)")]
        public decimal TotalSpent { get; set; }

        public LoyaltyTier Tier { get; set; } = LoyaltyTier.Bronze;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastVisitAt { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}".Trim();

        /// <summary>Bonus cashback percentage based on tier</summary>
        [NotMapped]
        public decimal CashbackPercent => Tier switch
        {
            LoyaltyTier.Bronze => 1.0m,
            LoyaltyTier.Silver => 2.0m,
            LoyaltyTier.Gold => 3.0m,
            LoyaltyTier.Platinum => 5.0m,
            _ => 0m
        };
    }

    public enum LoyaltyTier
    {
        Bronze = 1,     // 0 - 50,000 ֏
        Silver = 2,     // 50,001 - 200,000 ֏
        Gold = 3,       // 200,001 - 1,000,000 ֏
        Platinum = 4    // 1,000,001+ ֏
    }
}
