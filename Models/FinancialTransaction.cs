using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GasStationMS.Models
{
    /// <summary>
    /// Generic financial transaction log (ledger entry).
    /// Captures every money movement: sales revenue, fuel purchases, salaries, etc.
    /// </summary>
    public class FinancialTransaction
    {
        public int Id { get; set; }

        public FinancialTransactionType Type { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(14,2)")]
        public decimal Amount { get; set; }     // always positive

        public TransactionDirection Direction { get; set; } // Income / Expense

        public int? StationId { get; set; }
        [ValidateNever] public Station? Station { get; set; }

        public int? EmployeeId { get; set; }    // e.g. salary recipient
        [ValidateNever] public Employee? Employee { get; set; }

        public int? SupplierId { get; set; }    // e.g. fuel purchase
        [ValidateNever] public Supplier? Supplier { get; set; }

        public int? SaleId { get; set; }        // link to sale
        public int? DeliveryId { get; set; }    // link to delivery

        [StringLength(100)]
        public string? Reference { get; set; }  // invoice number, payslip #, etc.

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }  // who recorded it
    }

    public enum FinancialTransactionType
    {
        SaleRevenue     = 1,   // Customer payment for fuel
        FuelPurchase    = 2,   // Payment to supplier for fuel
        SalaryPayment   = 3,   // Employee salary
        OtherExpense    = 4,   // Maintenance, utilities, etc.
        OtherIncome     = 5,   // Miscellaneous income
        CouponDiscount  = 6,   // Revenue reduction from coupons
        LoyaltyBonus    = 7,   // Revenue reduction from loyalty points
    }

    public enum TransactionDirection
    {
        Income  = 1,
        Expense = 2
    }
}
