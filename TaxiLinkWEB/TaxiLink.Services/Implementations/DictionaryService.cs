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
    public class DictionaryService : IDictionaryService
    {
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<VehicleClass> _vehicleClassRepo;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IGenericRepository<OrderStatus> _statusRepo;
        private readonly IGenericRepository<PaymentMethod> _paymentRepo;

        public DictionaryService(
            IGenericRepository<City> cityRepo,
            IGenericRepository<VehicleClass> vehicleClassRepo,
            IGenericRepository<Role> roleRepo,
            IGenericRepository<OrderStatus> statusRepo,
            IGenericRepository<PaymentMethod> paymentRepo)
        {
            _cityRepo = cityRepo;
            _vehicleClassRepo = vehicleClassRepo;
            _roleRepo = roleRepo;
            _statusRepo = statusRepo;
            _paymentRepo = paymentRepo;
        }

        public async Task<IEnumerable<City>> GetAllCitiesAsync() => await _cityRepo.GetAllAsync();
        public async Task<City?> GetCityByIdAsync(int id) => await _cityRepo.GetByIdAsync(id);

        public async Task CreateCityAsync(City city)
        {
            await _cityRepo.AddAsync(city);
            await _cityRepo.SaveChangesAsync();
        }

        public async Task UpdateCityAsync(City city)
        {
            _cityRepo.Update(city);
            await _cityRepo.SaveChangesAsync();
        }

        public async Task DeleteCityAsync(int id)
        {
            var city = await _cityRepo.GetByIdAsync(id);
            if (city != null)
            {
                _cityRepo.Delete(city);
                await _cityRepo.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<VehicleClass>> GetAllVehicleClassesAsync() => await _vehicleClassRepo.GetAllAsync();
        public async Task<VehicleClass?> GetVehicleClassByIdAsync(int id) => await _vehicleClassRepo.GetByIdAsync(id);

        public async Task CreateVehicleClassAsync(VehicleClass vehicleClass)
        {
            await _vehicleClassRepo.AddAsync(vehicleClass);
            await _vehicleClassRepo.SaveChangesAsync();
        }

        public async Task UpdateVehicleClassAsync(VehicleClass vehicleClass)
        {
            _vehicleClassRepo.Update(vehicleClass);
            await _vehicleClassRepo.SaveChangesAsync();
        }

        public async Task DeleteVehicleClassAsync(int id)
        {
            var vc = await _vehicleClassRepo.GetByIdAsync(id);
            if (vc != null)
            {
                _vehicleClassRepo.Delete(vc);
                await _vehicleClassRepo.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<Role>> GetAllRolesAsync() => await _roleRepo.GetAllAsync();
        public async Task<IEnumerable<OrderStatus>> GetAllOrderStatusesAsync() => await _statusRepo.GetAllAsync();
        public async Task<IEnumerable<PaymentMethod>> GetAllPaymentMethodsAsync() => await _paymentRepo.GetAllAsync();
    }
}
