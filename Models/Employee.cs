using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    /// <summary>
    /// Բենզալցակայանի աշխատակից
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MiddleName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public EmployeeRole Role { get; set; } = EmployeeRole.Operator;

        public int? StationId { get; set; }
        public Station? Station { get; set; }

        public DateTime HiredAt { get; set; } = DateTime.UtcNow;
        public DateTime? TerminatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(12,2)")]
        public decimal BaseSalary { get; set; }

        public string? UserId { get; set; } // Link to ApplicationUser (Identity)

        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

        [NotMapped]
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }

    public enum EmployeeRole
    {
        Operator = 1,        // Օպերատոր (սովորական վաճառող)
        Manager = 2,         // Կայանի մենեջեր
        Accountant = 3,      // Հաշվապահ
        NetworkManager = 4,  // Ցանցի կառավարիչ
        Administrator = 5    // Համակարգի ադմին
    }
}
