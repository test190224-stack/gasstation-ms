using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GasStationMS.Models
{
    /// <summary>
    /// Ռեզերվուար (վառելիքի պահեստային տարա)
    /// </summary>
    public class FuelTank
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string TankCode { get; set; } = string.Empty; // Օր․ T-01

        public int StationId { get; set; }
        [ValidateNever] public Station? Station { get; set; }

        public int FuelTypeId { get; set; }
        [ValidateNever] public FuelType? FuelType { get; set; }

        [Range(100, 500000, ErrorMessage = "Տարողությունը պետք է լինի 100-500000 լիտր")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal CapacityLiters { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal CurrentVolumeLiters { get; set; }

        [Range(0, 50000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MinThresholdLiters { get; set; } = 500m;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<FuelDelivery> Deliveries { get; set; } = new List<FuelDelivery>();
        public ICollection<Dispenser> Dispensers { get; set; } = new List<Dispenser>();

        [NotMapped]
        public decimal FillPercentage =>
            CapacityLiters == 0 ? 0 : Math.Round(CurrentVolumeLiters / CapacityLiters * 100m, 2);

        [NotMapped]
        public bool IsLowStock => CurrentVolumeLiters <= MinThresholdLiters;

        /// <summary>
        /// Բոլոր չսպառված batch-երի weighted average cost
        /// Օգտագործվում է inventory valuation-ի համար
        /// </summary>
        [NotMapped]
        public decimal WeightedAverageCost
        {
            get
            {
                if (Deliveries == null || !Deliveries.Any()) return 0m;
                var totalRemaining = Deliveries.Sum(d => d.RemainingLiters);
                if (totalRemaining == 0) return 0m;
                var totalValue = Deliveries.Sum(d => d.RemainingLiters * d.PricePerLiter);
                return Math.Round(totalValue / totalRemaining, 2);
            }
        }
    }
}
