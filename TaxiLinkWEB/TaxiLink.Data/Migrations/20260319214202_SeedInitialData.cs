using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaxiLink.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AdditionalServices",
                columns: new[] { "Id", "IsPercentage", "Name", "Price" },
                values: new object[,]
                {
                    { 1, false, "Дитяче крісло", 50m },
                    { 2, true, "Чайові", 10m },
                    { 3, false, "Допомога з багажем", 30m },
                    { 4, false, "Провезення тварини", 20m }
                });

            migrationBuilder.InsertData(
                table: "CancellationReasons",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Клієнт передумав" },
                    { 2, "Водій запізнився" },
                    { 3, "Інша причина" },
                    { 4, "Помилкове замовлення" }
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "Name", "PriceMultiplier" },
                values: new object[,]
                {
                    { 1, "Київ", 1.0m },
                    { 2, "Житомир", 0.8m },
                    { 3, "Черняхів", 0.7m }
                });

            migrationBuilder.InsertData(
                table: "NewsItems",
                columns: new[] { "Id", "Description", "ImagePath", "PublishedAt", "Title" },
                values: new object[] { 1, "Знижено ціни", "/img/tariffs.png", new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), "Оновлення тарифів" });

            migrationBuilder.InsertData(
                table: "OrderStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Очікується" },
                    { 2, "В дорозі" },
                    { 3, "Завершено" },
                    { 4, "Скасовано" }
                });

            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Готівка" },
                    { 2, "Картка" },
                    { 3, "Водію на картку" }
                });

            migrationBuilder.InsertData(
                table: "PromoCodes",
                columns: new[] { "Id", "Code", "CurrentUses", "DiscountPercentage", "ExpiryDate", "MaxUses" },
                values: new object[] { 1, "START2026", 0, 15m, new DateTime(2026, 4, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 100 });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Client" },
                    { 3, "Driver" }
                });

            migrationBuilder.InsertData(
                table: "VehicleClasses",
                columns: new[] { "Id", "BasePrice", "CancellationFee", "Description", "Name", "PricePerKm", "PricePerKmOutsideCity", "PricePerMinuteWaiting" },
                values: new object[,]
                {
                    { 1, 50m, 30m, "Базовий рівень", "Економ", 10m, 15m, 2m },
                    { 2, 80m, 50m, "Авто з кондиціонером", "Комфорт", 15m, 20m, 3m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarPath", "BonusBalance", "DefaultCityId", "Email", "FacebookId", "FirstName", "GoogleId", "LastName", "PasswordHash", "PhoneNumber", "PrefersNoMusic", "PrefersSilentRide", "Rating", "RegistrationDate", "RoleId" },
                values: new object[,]
                {
                    { 1, null, 0m, null, "admin@taxilink.com", null, "Олена", null, "Диспетчер", "admin", "+380000000001", false, false, 0m, new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 2, "/img/client_dasha.png", 150.5m, 1, "dasha@gmail.com", null, "Дарія", "gl_222", "Лемеха", "123", "+380991234567", true, true, 5.0m, new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 2 },
                    { 3, "/img/ivan_driver.png", 0m, 2, "ivan@mail.com", "fb_333", "Іван", null, "Водій", "driver", "+380671234567", false, false, 4.8m, new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 3 }
                });

            migrationBuilder.InsertData(
                table: "Blacklists",
                columns: new[] { "Id", "BlockedAt", "BlockedUserId", "BlockerUserId" },
                values: new object[] { 1, new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 3, 2 });

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "AcceptanceRate", "CommissionRate", "DateOfBirth", "Iban", "IsFopActive", "IsVerified", "IsWorkingMode", "Patronymic", "ReferralCode", "TaxId", "UserId", "WalletBalance" },
                values: new object[] { 1, 98.5m, 12.5m, new DateTime(1990, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "UA123456789012345678901234567", true, true, true, "Петрович", "IVAN2026", "1234567890", 3, 1500.25m });

            migrationBuilder.InsertData(
                table: "SavedAddresses",
                columns: new[] { "Id", "AddressText", "Title", "UserId" },
                values: new object[] { 1, "вул. Хрещатик, 22", "Дім", 2 });

            migrationBuilder.InsertData(
                table: "UserPaymentCards",
                columns: new[] { "Id", "CardMask", "IsDefault", "PaymentSystem", "UserId" },
                values: new object[] { 1, "4149********1234", true, "Visa", 2 });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Brand", "Color", "DriverId", "InsuranceExpiryDate", "LicensePlate", "Model", "PassengerSeats", "Year" },
                values: new object[] { 1, "Renault", "Білий", 1, new DateTime(2027, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), "AA1234BC", "Clio 4", 4, 2014 });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CancellationReasonId", "CityId", "ClientComment", "ClientPriceBonus", "CompletedAt", "CreatedAt", "Distance", "DriverId", "DropoffAddress", "OrderStatusId", "PassengerName", "PassengerPhone", "PaymentMethodId", "PickupAddress", "PromoCodeId", "ScheduledTime", "TotalPrice", "UserId", "VehicleClassId", "VehicleId" },
                values: new object[] { 1, null, 1, "Буду з валізою", 5m, new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 19, 11, 0, 0, 0, DateTimeKind.Unspecified), 5.2m, 1, "Залізничний вокзал", 3, "Даша", "+380991234567", 2, "вул. Хрещатик, 22", 1, null, 180.50m, 2, 2, 1 });

            migrationBuilder.InsertData(
                table: "VehicleAdditionalServices",
                columns: new[] { "AdditionalServiceId", "VehicleId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "VehiclePhotos",
                columns: new[] { "Id", "PhotoPath", "VehicleId" },
                values: new object[] { 1, "/img/renault_clio.png", 1 });

            migrationBuilder.InsertData(
                table: "VehicleVehicleClasses",
                columns: new[] { "VehicleClassId", "VehicleId" },
                values: new object[] { 2, 1 });

            migrationBuilder.InsertData(
                table: "OrderAdditionalServices",
                columns: new[] { "AdditionalServiceId", "OrderId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "Comment", "CreatedAt", "OrderId", "Rating" },
                values: new object[] { 1, "Дуже швидка поїздка на Renault!", new DateTime(2026, 3, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 1, 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AdditionalServices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AdditionalServices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AdditionalServices",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Blacklists",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CancellationReasons",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CancellationReasons",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CancellationReasons",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CancellationReasons",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "NewsItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OrderAdditionalServices",
                keyColumns: new[] { "AdditionalServiceId", "OrderId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "OrderStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OrderStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "OrderStatuses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SavedAddresses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "UserPaymentCards",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VehicleAdditionalServices",
                keyColumns: new[] { "AdditionalServiceId", "VehicleId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "VehicleClasses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VehiclePhotos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VehicleVehicleClasses",
                keyColumns: new[] { "VehicleClassId", "VehicleId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "AdditionalServices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OrderStatuses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PromoCodes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "VehicleClasses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
