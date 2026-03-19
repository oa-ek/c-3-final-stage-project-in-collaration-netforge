using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaxiLink.Domain.Models;

namespace TaxiLink.Data.Context
{
    public class DBContextTaxiLink : DbContext
    {
        public DBContextTaxiLink(DbContextOptions<DBContextTaxiLink> options) : base(options)
        {
        }
        public DBContextTaxiLink()
        {
        }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Blacklist> Blacklists { get; set; }
        public DbSet<UserPaymentCard> UserPaymentCards { get; set; }
        public DbSet<SavedAddress> SavedAddresses { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleClass> VehicleClasses { get; set; }
        public DbSet<VehiclePhoto> VehiclePhotos { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<AdditionalService> AdditionalServices { get; set; }
        public DbSet<CancellationReason> CancellationReasons { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<NewsItem> NewsItems { get; set; }
        public DbSet<VehicleVehicleClass> VehicleVehicleClasses { get; set; }
        public DbSet<VehicleAdditionalService> VehicleAdditionalServices { get; set; }
        public DbSet<OrderAdditionalService> OrderAdditionalServices { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // "Server=." працює, якщо у тебе встановлено повний SQL Server. 
                // Якщо LocalDB (стандарт зі студією), краще "(localdb)\\mssqllocaldb"
                optionsBuilder.UseSqlServer("Server=.; Database=DBTaxi; Integrated Security=True; Encrypt=True; TrustServerCertificate=True")
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }

            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<VehicleVehicleClass>()
                .HasKey(vc => new { vc.VehicleId, vc.VehicleClassId });

            modelBuilder.Entity<VehicleAdditionalService>()
                .HasKey(vas => new { vas.VehicleId, vas.AdditionalServiceId });

            modelBuilder.Entity<OrderAdditionalService>()
                .HasKey(oas => new { oas.OrderId, oas.AdditionalServiceId });
            modelBuilder.Entity<Blacklist>()
                .HasOne(b => b.BlockerUser)
                .WithMany(u => u.BlockedByMe)
                .HasForeignKey(b => b.BlockerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Blacklist>()
                .HasOne(b => b.BlockedUser)
                .WithMany(u => u.BlockedMe)
                .HasForeignKey(b => b.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Driver)
                .WithMany()
                .HasForeignKey(o => o.DriverId)
                .OnDelete(DeleteBehavior.SetNull);
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
            modelBuilder.Seed();

        }
    }
}
