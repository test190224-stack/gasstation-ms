using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GasStationMS.Models
{
    /// <summary>
    /// Վառելիքի մատակարարում ռեզերվուար (batch)
    /// Յուրաքանչյուր delivery-ն առանձին batch է, որն ունի իր գինը (FIFO-ի համար)
    /// </summary>
    public class FuelDelivery
    {
        public int Id { get; set; }

        public int FuelTankId { get; set; }
        [ValidateNever] public FuelTank? FuelTank { get; set; }

        public int? SupplierId { get; set; }
        [ValidateNever] public Supplier? Supplier { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Ծավալը սխալ է")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal VolumeLiters { get; set; }

        /// <summary>Ծավալը որ դեռ չի սպառվել FIFO-ի համար</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal RemainingLiters { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Գինը սխալ է")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerLiter { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal TotalCost { get; set; }

        /// <summary>Deprecated — use Supplier instead</summary>
        [StringLength(100)]
        public string? SupplierName { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
