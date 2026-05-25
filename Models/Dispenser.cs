using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    /// <summary>
    /// Բենզակոլոնկա (fuel dispenser)
    /// </summary>
    public class Dispenser
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string DispenserCode { get; set; } = string.Empty; // D-01

        public int StationId { get; set; }
        public Station Station { get; set; } = null!;

        public int FuelTankId { get; set; }
        public FuelTank FuelTank { get; set; } = null!;

        public bool IsOperational { get; set; } = true;

        [Column(TypeName = "decimal(14,2)")]
        public decimal TotalDispensedLiters { get; set; }
    }
}
