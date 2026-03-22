using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.UI.Models;
using TaxiLink.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Collections.Generic;

namespace TaxiLink.UI.Controllers
{
    public class FleetController : Controller
    {
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IGenericRepository<Driver> _driverRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IDictionaryService _dictService;
        private readonly IGenericRepository<AdditionalService> _serviceRepo;
        private readonly IGenericRepository<VehiclePhoto> _photoRepo;
        private readonly IGenericRepository<VehicleVehicleClass> _vClassLinkRepo;
        private readonly IGenericRepository<VehicleAdditionalService> _vServiceLinkRepo;
        private readonly IWebHostEnvironment _env;

        public FleetController(
            IVehicleRepository vehicleRepo,
            IGenericRepository<Driver> driverRepo,
            IGenericRepository<User> userRepo,
            IDictionaryService dictService,
            IGenericRepository<AdditionalService> serviceRepo,
            IGenericRepository<VehiclePhoto> photoRepo,
            IGenericRepository<VehicleVehicleClass> vClassLinkRepo,
            IGenericRepository<VehicleAdditionalService> vServiceLinkRepo,
            IWebHostEnvironment env)
        {
            _vehicleRepo = vehicleRepo;
            _driverRepo = driverRepo;
            _userRepo = userRepo;
            _dictService = dictService;
            _serviceRepo = serviceRepo;
            _photoRepo = photoRepo;
            _vClassLinkRepo = vClassLinkRepo;
            _vServiceLinkRepo = vServiceLinkRepo;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var vehicles = await _vehicleRepo.GetAllAsync();
            var drivers = await _driverRepo.GetAllAsync();
            var users = await _userRepo.GetAllAsync();
            var photos = await _photoRepo.GetAllAsync();

            foreach (var v in vehicles)
            {
                v.Driver = drivers.FirstOrDefault(d => d.Id == v.DriverId);
                if (v.Driver != null) v.Driver.User = users.FirstOrDefault(u => u.Id == v.Driver.UserId);
                v.Photos = photos.Where(p => p.VehicleId == v.Id).ToList();
            }

            var driverList = drivers.Select(d => new {
                Id = d.Id,
                FullName = users.FirstOrDefault(u => u.Id == d.UserId)?.FirstName + " " + users.FirstOrDefault(u => u.Id == d.UserId)?.LastName
            }).ToList();

            var model = new AdminViewModels.FleetPageViewModel
            {
                Vehicles = vehicles,
                Drivers = new SelectList(driverList, "Id", "FullName"),
                AvailableClasses = await _dictService.GetAllVehicleClassesAsync(),
                AvailableServices = await _serviceRepo.GetAllAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicleDetails(int id)
        {
            var vehicle = await _vehicleRepo.GetVehicleWithDetailsAsync(id);
            if (vehicle == null) return NotFound();

            var vServices = await _vServiceLinkRepo.GetAllAsync();

            return Json(new AdminViewModels.VehicleUpsertDto
            {
                Id = vehicle.Id,
                DriverId = vehicle.DriverId,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Color = vehicle.Color,
                LicensePlate = vehicle.LicensePlate,
                PassengerSeats = vehicle.PassengerSeats,
                InsuranceExpiryDate = vehicle.InsuranceExpiryDate,
                ExistingPhotoPath = vehicle.Photos?.FirstOrDefault()?.PhotoPath,
                SelectedClassIds = vehicle.VehicleClasses?.Select(c => c.VehicleClassId).ToList() ?? new List<int>(),
                SelectedServiceIds = vServices.Where(s => s.VehicleId == vehicle.Id).Select(s => s.AdditionalServiceId).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpsertVehicle(AdminViewModels.VehicleUpsertDto dto, IFormFile? PhotoFile)
        {
            string? newPhotoPath = await ProcessPhotoAsync(PhotoFile, dto.ExistingPhotoPath);

            if (dto.Id == 0)
            {
                var vehicle = new Vehicle
                {
                    DriverId = dto.DriverId,
                    Brand = dto.Brand,
                    Model = dto.Model,
                    Year = dto.Year,
                    Color = dto.Color,
                    LicensePlate = dto.LicensePlate,
                    PassengerSeats = dto.PassengerSeats,
                    InsuranceExpiryDate = dto.InsuranceExpiryDate
                };

                await _vehicleRepo.AddAsync(vehicle);
                await _vehicleRepo.SaveChangesAsync();

                if (!string.IsNullOrEmpty(newPhotoPath))
                {
                    await _photoRepo.AddAsync(new VehiclePhoto { VehicleId = vehicle.Id, PhotoPath = newPhotoPath });
                    await _photoRepo.SaveChangesAsync();
                }

                await SaveVehicleLinksAsync(vehicle.Id, dto.SelectedClassIds, dto.SelectedServiceIds);
            }
            else
            {
                var existingVehicle = await _vehicleRepo.GetByIdAsync(dto.Id);
                if (existingVehicle != null)
                {
                    existingVehicle.DriverId = dto.DriverId;
                    existingVehicle.Brand = dto.Brand;
                    existingVehicle.Model = dto.Model;
                    existingVehicle.Year = dto.Year;
                    existingVehicle.Color = dto.Color;
                    existingVehicle.LicensePlate = dto.LicensePlate;
                    existingVehicle.PassengerSeats = dto.PassengerSeats;
                    existingVehicle.InsuranceExpiryDate = dto.InsuranceExpiryDate;

                    _vehicleRepo.Update(existingVehicle);
                    await _vehicleRepo.SaveChangesAsync();

                    if (PhotoFile != null && !string.IsNullOrEmpty(newPhotoPath))
                    {
                        var oldPhotos = (await _photoRepo.GetAllAsync()).Where(p => p.VehicleId == dto.Id).ToList();
                        foreach (var p in oldPhotos) _photoRepo.Delete(p);
                        await _photoRepo.SaveChangesAsync();

                        await _photoRepo.AddAsync(new VehiclePhoto { VehicleId = dto.Id, PhotoPath = newPhotoPath });
                        await _photoRepo.SaveChangesAsync();
                    }

                    await SaveVehicleLinksAsync(dto.Id, dto.SelectedClassIds, dto.SelectedServiceIds);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id);
            if (vehicle != null)
            {
                var classes = (await _vClassLinkRepo.GetAllAsync()).Where(c => c.VehicleId == id).ToList();
                foreach (var c in classes) _vClassLinkRepo.Delete(c);

                var services = (await _vServiceLinkRepo.GetAllAsync()).Where(s => s.VehicleId == id).ToList();
                foreach (var s in services) _vServiceLinkRepo.Delete(s);

                var photos = (await _photoRepo.GetAllAsync()).Where(p => p.VehicleId == id).ToList();
                foreach (var p in photos) _photoRepo.Delete(p);

                await _vClassLinkRepo.SaveChangesAsync();

                _vehicleRepo.Delete(vehicle);
                await _vehicleRepo.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> ProcessPhotoAsync(IFormFile? file, string? currentPath)
        {
            if (file == null || file.Length == 0) return currentPath;
            string uploadsFolder = Path.Combine(_env.WebRootPath, "img");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                await file.CopyToAsync(fileStream);
            return "/img/" + uniqueFileName;
        }

        private async Task SaveVehicleLinksAsync(int vehicleId, List<int> classIds, List<int> serviceIds)
        {
            var oldClasses = (await _vClassLinkRepo.GetAllAsync()).Where(c => c.VehicleId == vehicleId).ToList();
            foreach (var c in oldClasses) _vClassLinkRepo.Delete(c);

            var oldServices = (await _vServiceLinkRepo.GetAllAsync()).Where(s => s.VehicleId == vehicleId).ToList();
            foreach (var s in oldServices) _vServiceLinkRepo.Delete(s);

            await _vClassLinkRepo.SaveChangesAsync();

            if (classIds != null)
            {
                foreach (var cid in classIds)
                    await _vClassLinkRepo.AddAsync(new VehicleVehicleClass { VehicleId = vehicleId, VehicleClassId = cid });
            }

            if (serviceIds != null)
            {
                foreach (var sid in serviceIds)
                    await _vServiceLinkRepo.AddAsync(new VehicleAdditionalService { VehicleId = vehicleId, AdditionalServiceId = sid });
            }

            await _vClassLinkRepo.SaveChangesAsync();
        }
    }
}