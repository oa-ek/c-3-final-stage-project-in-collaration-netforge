using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Services.Interfaces
{
    public interface IDriverService
    {
        // Водії
        Task<IEnumerable<Driver>> GetAllDriversAsync();
        Task<Driver?> GetDriverWithDetailsAsync(int id);
        Task CreateDriverAsync(Driver driver);
        Task UpdateDriverAsync(Driver driver);
        Task DeleteDriverAsync(int id);

        // Машини
        Task<Vehicle?> GetVehicleWithDetailsAsync(int id);
        Task CreateVehicleAsync(Vehicle vehicle);
        Task UpdateVehicleAsync(Vehicle vehicle);
        Task DeleteVehicleAsync(int id);

        // Залежний CRUD
        Task AddVehiclePhotoAsync(VehiclePhoto photo);
        Task DeleteVehiclePhotoAsync(int photoId);
    }
}
