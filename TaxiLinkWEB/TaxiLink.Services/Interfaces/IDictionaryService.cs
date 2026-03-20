using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Services.Interfaces
{
    public interface IDictionaryService
    {
        //  Міста
        Task<IEnumerable<City>> GetAllCitiesAsync();
        Task<City?> GetCityByIdAsync(int id);
        Task CreateCityAsync(City city);
        Task UpdateCityAsync(City city);
        Task DeleteCityAsync(int id);

        //  Класи авто
        Task<IEnumerable<VehicleClass>> GetAllVehicleClassesAsync();
        Task<VehicleClass?> GetVehicleClassByIdAsync(int id);
        Task CreateVehicleClassAsync(VehicleClass vehicleClass);
        Task UpdateVehicleClassAsync(VehicleClass vehicleClass);
        Task DeleteVehicleClassAsync(int id);

        //  Read для системних довідників без видалення
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<IEnumerable<OrderStatus>> GetAllOrderStatusesAsync();
        Task<IEnumerable<PaymentMethod>> GetAllPaymentMethodsAsync();
    }
}
