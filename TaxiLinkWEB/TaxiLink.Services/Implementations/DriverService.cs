using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;

namespace TaxiLink.Services.Implementations
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IGenericRepository<VehiclePhoto> _photoRepo;

        public DriverService(
            IDriverRepository driverRepo,
            IVehicleRepository vehicleRepo,
            IGenericRepository<VehiclePhoto> photoRepo)
        {
            _driverRepo = driverRepo;
            _vehicleRepo = vehicleRepo;
            _photoRepo = photoRepo;
        }
        public async Task<IEnumerable<Driver>> GetAllDriversAsync() => await _driverRepo.GetAllAsync();
        public async Task<Driver?> GetDriverWithDetailsAsync(int id) => await _driverRepo.GetDriverWithDetailsAsync(id);

        public async Task CreateDriverAsync(Driver driver)
        {
            driver.IsVerified = false; //новий водій чекає на перевірку адміном
            driver.WalletBalance = 0;
            await _driverRepo.AddAsync(driver);
            await _driverRepo.SaveChangesAsync();
        }

        public async Task UpdateDriverAsync(Driver driver)
        {
            _driverRepo.Update(driver);
            await _driverRepo.SaveChangesAsync();
        }

        public async Task DeleteDriverAsync(int id)
        {
            var driver = await _driverRepo.GetByIdAsync(id);
            if (driver != null)
            {
                _driverRepo.Delete(driver);
                await _driverRepo.SaveChangesAsync();
            }
        }
        public async Task<Vehicle?> GetVehicleWithDetailsAsync(int id) => await _vehicleRepo.GetVehicleWithDetailsAsync(id);

        public async Task CreateVehicleAsync(Vehicle vehicle)
        {
            await _vehicleRepo.AddAsync(vehicle);
            await _vehicleRepo.SaveChangesAsync();
        }

        public async Task UpdateVehicleAsync(Vehicle vehicle)
        {
            _vehicleRepo.Update(vehicle);
            await _vehicleRepo.SaveChangesAsync();
        }

        public async Task DeleteVehicleAsync(int id)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id);
            if (vehicle != null)
            {
                _vehicleRepo.Delete(vehicle);
                await _vehicleRepo.SaveChangesAsync();
            }
        }

        public async Task AddVehiclePhotoAsync(VehiclePhoto photo)
        {
            await _photoRepo.AddAsync(photo);
            await _photoRepo.SaveChangesAsync();
        }

        public async Task DeleteVehiclePhotoAsync(int photoId)
        {
            var photo = await _photoRepo.GetByIdAsync(photoId);
            if (photo != null)
            {
                _photoRepo.Delete(photo);
                await _photoRepo.SaveChangesAsync();
            }
        }
    }
}
