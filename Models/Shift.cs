using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasStationMS.Models
{
    /// <summary>
    /// Հերթափոխ (shift)
    /// </summary>
    public class Shift
    {
        public int Id { get; set; }

        public int StationId { get; set; }
        public Station Station { get; set; } = null!;

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal OpeningCash { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal ClosingCash { get; set; }

        public ShiftStatus Status { get; set; } = ShiftStatus.Open;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        [NotMapped]
        public TimeSpan? Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;
    }

    public enum ShiftStatus
    {
        Open = 1,
        Closed = 2,
        Audited = 3
    }
}
