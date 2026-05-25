using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Models;

namespace GasStationMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Station> Stations => Set<Station>();
        public DbSet<FuelType> FuelTypes => Set<FuelType>();
        public DbSet<FuelTank> FuelTanks => Set<FuelTank>();
        public DbSet<Dispenser> Dispensers => Set<Dispenser>();
        public DbSet<FuelDelivery> FuelDeliveries => Set<FuelDelivery>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<EmployeePermission> EmployeePermissions => Set<EmployeePermission>();
        public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== Indices =====
            builder.Entity<Sale>().HasIndex(s => s.ReceiptNumber).IsUnique();
            builder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
            builder.Entity<Customer>().HasIndex(c => c.CardCode).IsUnique();
            builder.Entity<Station>().HasIndex(s => s.Name);
            builder.Entity<EmployeePermission>()
                .HasIndex(p => new { p.EmployeeId, p.Permission }).IsUnique();

            // ===== FuelTank =====
            builder.Entity<FuelTank>()
                .HasOne(t => t.Station).WithMany(s => s.Tanks)
                .HasForeignKey(t => t.StationId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FuelTank>()
                .HasOne(t => t.FuelType).WithMany(f => f.Tanks)
                .HasForeignKey(t => t.FuelTypeId).OnDelete(DeleteBehavior.Restrict);

            // ===== Dispenser =====
            builder.Entity<Dispenser>()
                .HasOne(d => d.Station).WithMany(s => s.Dispensers)
                .HasForeignKey(d => d.StationId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Dispenser>()
                .HasOne(d => d.FuelTank).WithMany(t => t.Dispensers)
                .HasForeignKey(d => d.FuelTankId).OnDelete(DeleteBehavior.Restrict);

            // ===== FuelDelivery =====
            builder.Entity<FuelDelivery>()
                .HasOne(fd => fd.FuelTank).WithMany(t => t.Deliveries)
                .HasForeignKey(fd => fd.FuelTankId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FuelDelivery>()
                .HasOne(fd => fd.Supplier).WithMany(s => s.Deliveries)
                .HasForeignKey(fd => fd.SupplierId).OnDelete(DeleteBehavior.SetNull);

            // ===== Employee =====
            builder.Entity<Employee>()
                .HasOne(e => e.Station).WithMany(s => s.Employees)
                .HasForeignKey(e => e.StationId).OnDelete(DeleteBehavior.SetNull);

            // ===== Shift =====
            builder.Entity<Shift>()
                .HasOne(sh => sh.Station).WithMany(s => s.Shifts)
                .HasForeignKey(sh => sh.StationId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Shift>()
                .HasOne(sh => sh.Employee).WithMany(e => e.Shifts)
                .HasForeignKey(sh => sh.EmployeeId).OnDelete(DeleteBehavior.Restrict);

            // ===== Sale =====
            builder.Entity<Sale>()
                .HasOne(s => s.Station).WithMany()
                .HasForeignKey(s => s.StationId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Sale>()
                .HasOne(s => s.Shift).WithMany(sh => sh.Sales)
                .HasForeignKey(s => s.ShiftId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Sale>()
                .HasOne(s => s.Dispenser).WithMany()
                .HasForeignKey(s => s.DispenserId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Sale>()
                .HasOne(s => s.FuelType).WithMany()
                .HasForeignKey(s => s.FuelTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Sale>()
                .HasOne(s => s.Coupon).WithMany(c => c.Sales)
                .HasForeignKey(s => s.CouponId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Sale>()
                .HasOne(s => s.Customer).WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);

            // ===== Coupon =====
            builder.Entity<Coupon>()
                .HasOne(c => c.FuelType).WithMany()
                .HasForeignKey(c => c.FuelTypeId).OnDelete(DeleteBehavior.Restrict);

            // ===== ApplicationUser ↔ Employee =====
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Employee).WithMany()
                .HasForeignKey(u => u.EmployeeId).OnDelete(DeleteBehavior.SetNull);

            // ===== EmployeePermission =====
            builder.Entity<EmployeePermission>()
                .HasOne(p => p.Employee).WithMany()
                .HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);

            // ===== FinancialTransaction =====
            builder.Entity<FinancialTransaction>()
                .HasOne(ft => ft.Station).WithMany()
                .HasForeignKey(ft => ft.StationId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<FinancialTransaction>()
                .HasOne(ft => ft.Employee).WithMany()
                .HasForeignKey(ft => ft.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<FinancialTransaction>()
                .HasOne(ft => ft.Supplier).WithMany()
                .HasForeignKey(ft => ft.SupplierId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
