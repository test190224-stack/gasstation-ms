using System;
using GasStationMS.Models;

namespace GasStationMS.ViewModels
{
    /// <summary>
    /// Supplier summary row for the Suppliers/Index page
    /// </summary>
    public class SupplierSummaryViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public int DeliveryCount { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastDelivery { get; set; }
    }

    /// <summary>
    /// Supplier report row for date-range based reporting
    /// </summary>
    public class SupplierReportRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DeliveryCount { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AvgPricePerLiter { get; set; }
    }
}
