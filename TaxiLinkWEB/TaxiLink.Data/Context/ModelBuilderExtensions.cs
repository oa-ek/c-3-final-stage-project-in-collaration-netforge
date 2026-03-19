using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Data.Context
{
    public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            var dt = new DateTime(2026, 3, 19, 12, 0, 0);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Client" },
                new Role { Id = 3, Name = "Driver" }
            );

            modelBuilder.Entity<City>().HasData(
                new City { Id = 1, Name = "Київ", PriceMultiplier = 1.0m },
                new City { Id = 2, Name = "Житомир", PriceMultiplier = 0.8m },
                new City { Id = 3, Name = "Черняхів", PriceMultiplier = 0.7m }
            );

            modelBuilder.Entity<OrderStatus>().HasData(
                new OrderStatus { Id = 1, Name = "Очікується" },
                new OrderStatus { Id = 2, Name = "В дорозі" },
                new OrderStatus { Id = 3, Name = "Завершено" },
                new OrderStatus { Id = 4, Name = "Скасовано" }
            );

            modelBuilder.Entity<PaymentMethod>().HasData(
                new PaymentMethod { Id = 1, Name = "Готівка" },
                new PaymentMethod { Id = 2, Name = "Картка" },
                new PaymentMethod { Id = 3, Name = "Водію на картку" }
            );

            modelBuilder.Entity<CancellationReason>().HasData(
                new CancellationReason { Id = 1, Name = "Клієнт передумав" },
                new CancellationReason { Id = 2, Name = "Водій запізнився" },
                new CancellationReason { Id = 3, Name = "Інша причина" },
                new CancellationReason { Id = 4, Name = "Помилкове замовлення" }
            );

            modelBuilder.Entity<AdditionalService>().HasData(
                new AdditionalService { Id = 1, Name = "Дитяче крісло", Price = 50m, IsPercentage = false },
                new AdditionalService { Id = 2, Name = "Чайові", Price = 10m, IsPercentage = true },
                new AdditionalService { Id = 3, Name = "Допомога з багажем", Price = 30m, IsPercentage = false },
                new AdditionalService { Id = 4, Name = "Провезення тварини", Price = 20m, IsPercentage = false }
            );

            modelBuilder.Entity<PromoCode>().HasData(
                new PromoCode { Id = 1, Code = "START2026", DiscountPercentage = 15m, ExpiryDate = dt.AddMonths(1), MaxUses = 100, CurrentUses = 0 }
            );

            modelBuilder.Entity<VehicleClass>().HasData(
                new VehicleClass { Id = 1, Name = "Економ", Description = "Базовий рівень", BasePrice = 50m, PricePerKm = 10m, PricePerKmOutsideCity = 15m, PricePerMinuteWaiting = 2m, CancellationFee = 30m },
                new VehicleClass { Id = 2, Name = "Комфорт", Description = "Авто з кондиціонером", BasePrice = 80m, PricePerKm = 15m, PricePerKmOutsideCity = 20m, PricePerMinuteWaiting = 3m, CancellationFee = 50m }
            );

            modelBuilder.Entity<NewsItem>().HasData(
                new NewsItem { Id = 1, Title = "Оновлення тарифів", Description = "Знижено ціни", ImagePath = "/img/tariffs.png", PublishedAt = dt }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, RoleId = 1, FirstName = "Олена", LastName = "Диспетчер", PhoneNumber = "+380000000001", Email = "admin@taxilink.com", PasswordHash = "admin", AvatarPath = null, Rating = 0m, BonusBalance = 0m, PrefersSilentRide = false, PrefersNoMusic = false, DefaultCityId = null, FacebookId = null, GoogleId = null, RegistrationDate = dt },
                new User { Id = 2, RoleId = 2, FirstName = "Дарія", LastName = "Лемеха", PhoneNumber = "+380991234567", Email = "dasha@gmail.com", PasswordHash = "123", AvatarPath = "/img/client_dasha.png", Rating = 5.0m, BonusBalance = 150.5m, PrefersSilentRide = true, PrefersNoMusic = true, DefaultCityId = 1, FacebookId = null, GoogleId = "gl_222", RegistrationDate = dt },
                new User { Id = 3, RoleId = 3, FirstName = "Іван", LastName = "Водій", PhoneNumber = "+380671234567", Email = "ivan@mail.com", PasswordHash = "driver", AvatarPath = "/img/ivan_driver.png", Rating = 4.8m, BonusBalance = 0m, PrefersSilentRide = false, PrefersNoMusic = false, DefaultCityId = 2, FacebookId = "fb_333", GoogleId = null, RegistrationDate = dt }
            );

            modelBuilder.Entity<Driver>().HasData(
                new Driver { Id = 1, UserId = 3, Patronymic = "Петрович", DateOfBirth = new DateTime(1990, 5, 15), TaxId = "1234567890", IsVerified = true, AcceptanceRate = 98.5m, WalletBalance = 1500.25m, IsFopActive = true, Iban = "UA123456789012345678901234567", CommissionRate = 12.5m, ReferralCode = "IVAN2026", IsWorkingMode = true }
            );

            modelBuilder.Entity<SavedAddress>().HasData(
                new SavedAddress { Id = 1, UserId = 2, Title = "Дім", AddressText = "вул. Хрещатик, 22" }
            );

            modelBuilder.Entity<UserPaymentCard>().HasData(
                new UserPaymentCard { Id = 1, UserId = 2, CardMask = "4149********1234", PaymentSystem = "Visa", IsDefault = true }
            );

            modelBuilder.Entity<Blacklist>().HasData(
                new Blacklist { Id = 1, BlockerUserId = 2, BlockedUserId = 3, BlockedAt = dt }
            );

            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle { Id = 1, DriverId = 1, Brand = "Renault", Model = "Clio 4", Year = 2014, Color = "Білий", LicensePlate = "AA1234BC", PassengerSeats = 4, InsuranceExpiryDate = dt.AddYears(1) }
            );

            modelBuilder.Entity<VehiclePhoto>().HasData(
                new VehiclePhoto { Id = 1, VehicleId = 1, PhotoPath = "/img/renault_clio.png" }
            );

            modelBuilder.Entity<VehicleVehicleClass>().HasData(
                new VehicleVehicleClass { VehicleId = 1, VehicleClassId = 2 }
            );

            modelBuilder.Entity<VehicleAdditionalService>().HasData(
                new VehicleAdditionalService { VehicleId = 1, AdditionalServiceId = 1 }
            );

            modelBuilder.Entity<Order>().HasData(
                new Order { Id = 1, UserId = 2, DriverId = 1, VehicleId = 1, VehicleClassId = 2, CityId = 1, OrderStatusId = 3, PaymentMethodId = 2, PromoCodeId = 1, CancellationReasonId = null, PickupAddress = "вул. Хрещатик, 22", DropoffAddress = "Залізничний вокзал", ScheduledTime = null, PassengerName = "Даша", PassengerPhone = "+380991234567", ClientComment = "Буду з валізою", Distance = 5.2m, TotalPrice = 180.50m, ClientPriceBonus = 5m, CreatedAt = dt.AddHours(-1), CompletedAt = dt }
            );

            modelBuilder.Entity<OrderAdditionalService>().HasData(
                new OrderAdditionalService { OrderId = 1, AdditionalServiceId = 1 }
            );

            modelBuilder.Entity<Review>().HasData(
                new Review { Id = 1, OrderId = 1, Rating = 5, Comment = "Дуже швидка поїздка на Renault!", CreatedAt = dt }
            );
        }
    }
}

