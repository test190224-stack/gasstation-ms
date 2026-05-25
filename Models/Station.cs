using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    /// <summary>
    /// Բենզալցակայան — ցանցի հիմնական միավոր
    /// </summary>
    public class Station
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string? City { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public StationStatus Status { get; set; } = StationStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<FuelTank> Tanks { get; set; } = new List<FuelTank>();
        public ICollection<Dispenser> Dispensers { get; set; } = new List<Dispenser>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }

    public enum StationStatus
    {
        Active = 1,
        Maintenance = 2,
        Closed = 3
    }
}
