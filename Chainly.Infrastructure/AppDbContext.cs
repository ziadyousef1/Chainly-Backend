using Chainly.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Chainly.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User, Role, int>(options)
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<ProductionLine> ProductionLines { get; set; }
        public new DbSet<User> Users { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<SupplierMaterial> SupplierMaterials { get; set; }
        public DbSet<DefectiveProduct> DefectiveProducts { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderTracking> OrderTracking { get; set; }
        public DbSet<SalesData> SalesData { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<NonDefectiveProduct> NonDefectiveProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Configure one-to-many relationship between Company and User
            builder.Entity<Company>()
                .HasMany(c => c.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configure one-to-many relationship between ProductionLine and Report
            builder.Entity<ProductionLine>()
                .HasMany(pl => pl.Reports)
                .WithOne(r => r.ProductionLine)
                .HasForeignKey(r => r.ProductionLineId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configure one-to-many relationship between Report and DefectiveProduct
            builder.Entity<Report>()
                .HasMany(r => r.DefectiveProducts)
                .WithOne(dp => dp.Report)
                .HasForeignKey(dp => dp.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configure many-to-many relationship between Supplier and Material through SupplierMaterial
            builder.Entity<SupplierMaterial>()
                .HasKey(sm => new { sm.SupplierId, sm.MaterialId });
            builder.Entity<SupplierMaterial>()
                .HasOne(sm => sm.Supplier)
                .WithMany(s => s.SupplierMaterials)
                .HasForeignKey(sm => sm.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SupplierMaterial>()
                .HasOne(sm => sm.Material)
                .WithMany(m => m.SupplierMaterials)
                .HasForeignKey(sm => sm.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasIndex(o => new { OrderId = o.CompanyOrderId, o.CompanyId })
                .IsUnique();
            builder.Entity<Order>()
                .HasKey(o => o.Id);

            builder.Entity<OrderTracking>()
                .HasOne(ot => ot.Order)
                .WithMany(o => o.Trackings)
                .HasForeignKey(ot => ot.OrderId);

            builder.Entity<OrderTracking>()
                .HasOne(ot => ot.UpdatedByNavigation)
                .WithMany()
                .HasForeignKey(ot => ot.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<OrderTracking>()
                .Property(ot => ot.Id)
                .ValueGeneratedOnAdd();
            builder.Entity<Role>()
                .HasIndex(r => new { r.Name, r.CompanyId })
                .IsUnique();

            builder.Entity<SalesData>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CityId).IsRequired();
                entity.Property(e => e.StoreId).IsRequired();
                entity.Property(e => e.ManagementGroupId).IsRequired();
                entity.Property(e => e.FirstCategoryId).IsRequired();
                entity.Property(e => e.SecondCategoryId).IsRequired();
                entity.Property(e => e.ThirdCategoryId).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.Dt).IsRequired().HasMaxLength(50);

                entity.Property(e => e.SaleAmount).HasPrecision(18, 4);
                entity.Property(e => e.Discount).HasPrecision(18, 4);
                entity.Property(e => e.Precpt).HasPrecision(18, 4);
                entity.Property(e => e.AvgTemperature).HasPrecision(18, 4);
                entity.Property(e => e.AvgHumidity).HasPrecision(18, 4);
                entity.Property(e => e.AvgWindLevel).HasPrecision(18, 4);

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId);

                entity.HasIndex(e => new { e.CityId, e.StoreId, e.ProductId, e.Dt })
                    .IsUnique();

                builder.Entity<Product>()
                    .HasKey(p => new { p.Id, p.CompanyId });

                builder.Entity<Product>()
                    .HasOne(p => p.Company)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CompanyId);
            });
        }
    }
}