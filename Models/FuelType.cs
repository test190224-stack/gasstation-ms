using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    public class FuelType
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Code { get; set; } // A92, A95, A98, DSL, LPG

        /// <summary>Recommended sale price per liter (fallback)</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerLiter { get; set; }

        /// <summary>
        /// Markup percentage above cost (e.g., 15 means 15% profit margin).
        /// If markup > 0, price is auto-calculated as: cost * (1 + markup/100)
        /// </summary>
        [Range(0, 200)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal MarkupPercent { get; set; } = 15m;

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<FuelTank> Tanks { get; set; } = new List<FuelTank>();
    }
}
