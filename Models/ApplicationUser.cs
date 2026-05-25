using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GasStationMS.Models
{
    /// <summary>
    /// Համակարգի օգտատեր (Identity-ի ընդլայնում)
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
