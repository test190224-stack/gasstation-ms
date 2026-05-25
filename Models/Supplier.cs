using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GasStationMS.Models
{
    /// <summary>
    /// Վառելիքի մատակարարող
    /// </summary>
    public class Supplier
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TaxId { get; set; } // ՀՎՀՀ

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<FuelDelivery> Deliveries { get; set; } = new List<FuelDelivery>();
    }
}
