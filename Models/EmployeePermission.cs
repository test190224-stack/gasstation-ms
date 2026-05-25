using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GasStationMS.Models
{
    /// <summary>
    /// Fine-grained permission override per employee.
    /// By default, role defines access. Admin can ADD extra or REMOVE standard access.
    /// </summary>
    public class EmployeePermission
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        [ValidateNever] public Employee? Employee { get; set; }

        [Required, StringLength(80)]
        public string Permission { get; set; } = string.Empty; // e.g. "Deliveries.Create"

        public bool IsGranted { get; set; } = true; // true = grant, false = explicit deny
    }

    /// <summary>All known permission keys</summary>
    public static class Permissions
    {
        // Sales
        public const string SalesView   = "Sales.View";
        public const string SalesCreate = "Sales.Create";

        // Inventory
        public const string InventoryView     = "Inventory.View";
        public const string InventoryDeliver  = "Inventory.Deliver";
        public const string InventoryTankEdit = "Inventory.TankEdit";

        // Employees
        public const string EmployeesView   = "Employees.View";
        public const string EmployeesEdit   = "Employees.Edit";

        // Suppliers
        public const string SuppliersView   = "Suppliers.View";
        public const string SuppliersEdit   = "Suppliers.Edit";

        // Reports
        public const string ReportsView     = "Reports.View";
        public const string ReportsExport   = "Reports.Export";

        // Wallet / Finance
        public const string FinanceView     = "Finance.View";
        public const string FinanceSalary   = "Finance.Salary";

        // Stations
        public const string StationsView    = "Stations.View";
        public const string StationsEdit    = "Stations.Edit";

        // All permissions list (for admin UI)
        public static readonly (string Key, string Label, string Group)[] All =
        {
            (SalesView,        "Դիտել վաճառքները",          "Վաճառք"),
            (SalesCreate,      "Գրանցել վաճառք",            "Վաճառք"),
            (InventoryView,    "Դիտել պաշարները",           "Պաշարներ"),
            (InventoryDeliver, "Գրանցել մատակարարում",      "Պաշարներ"),
            (InventoryTankEdit,"Խմբ. ռեզերվուարներ",        "Պաշարներ"),
            (EmployeesView,    "Դիտել աշխատակիցներ",        "Աշխատակիցներ"),
            (EmployeesEdit,    "Խմբ. աշխատակիցներ",         "Աշխատակիցներ"),
            (SuppliersView,    "Դիտել մատակարարներ",        "Մատակարարներ"),
            (SuppliersEdit,    "Խմբ. մատակարարներ",         "Մատակարարներ"),
            (ReportsView,      "Դիտել հաշվետվություններ",   "Հաշվետվություններ"),
            (ReportsExport,    "Արտ. Excel",                "Հաշվետվություններ"),
            (FinanceView,      "Դիտել ֆինանսներ",           "Ֆինանսներ"),
            (FinanceSalary,    "Կատ. աշխ. վարձ",            "Ֆինանսներ"),
            (StationsView,     "Դիտել կայաններ",            "Կայաններ"),
            (StationsEdit,     "Խմբ. կայաններ",             "Կայաններ"),
        };

        /// <summary>Default permissions by role</summary>
        public static string[] DefaultsForRole(EmployeeRole role) => role switch
        {
            EmployeeRole.Operator => new[]
            {
                SalesView, SalesCreate, InventoryView
            },
            EmployeeRole.Manager => new[]
            {
                SalesView, SalesCreate, InventoryView, InventoryDeliver, InventoryTankEdit,
                EmployeesView, SuppliersView, ReportsView, ReportsExport,
                StationsView, FinanceView
            },
            EmployeeRole.Accountant => new[]
            {
                SalesView, ReportsView, ReportsExport, FinanceView, FinanceSalary,
                SuppliersView
            },
            EmployeeRole.NetworkManager => new[]
            {
                SalesView, SalesCreate, InventoryView, InventoryDeliver, InventoryTankEdit,
                EmployeesView, EmployeesEdit, SuppliersView, SuppliersEdit,
                ReportsView, ReportsExport, FinanceView, FinanceSalary,
                StationsView, StationsEdit
            },
            EmployeeRole.Administrator => new[]
            {
                SalesView, SalesCreate, InventoryView, InventoryDeliver, InventoryTankEdit,
                EmployeesView, EmployeesEdit, SuppliersView, SuppliersEdit,
                ReportsView, ReportsExport, FinanceView, FinanceSalary,
                StationsView, StationsEdit
            },
            _ => System.Array.Empty<string>()
        };
    }
}
