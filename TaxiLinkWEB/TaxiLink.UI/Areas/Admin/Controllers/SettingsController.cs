using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.UI.Admin_areas.Models;

namespace TaxiLink.UI.Admin_areas.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<AdditionalService> _serviceRepo;
        private readonly IGenericRepository<OrderStatus> _statusRepo;
        private readonly IGenericRepository<VehicleClass> _vehicleClassRepo;
        private readonly IGenericRepository<PaymentMethod> _paymentMethodRepo;
        private readonly IGenericRepository<CancellationReason> _cancellationReasonRepo;

        public SettingsController(
            IGenericRepository<City> cityRepo,
            IGenericRepository<AdditionalService> serviceRepo,
            IGenericRepository<OrderStatus> statusRepo,
            IGenericRepository<VehicleClass> vehicleClassRepo,
            IGenericRepository<PaymentMethod> paymentMethodRepo,
            IGenericRepository<CancellationReason> cancellationReasonRepo)
        {
            _cityRepo = cityRepo;
            _serviceRepo = serviceRepo;
            _statusRepo = statusRepo;
            _vehicleClassRepo = vehicleClassRepo;
            _paymentMethodRepo = paymentMethodRepo;
            _cancellationReasonRepo = cancellationReasonRepo;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminViewModels.SettingsPageViewModel
            {
                Cities = await _cityRepo.GetAllAsync(),
                AdditionalServices = await _serviceRepo.GetAllAsync(),
                OrderStatuses = await _statusRepo.GetAllAsync(),
                VehicleClasses = await _vehicleClassRepo.GetAllAsync(),
                PaymentMethods = await _paymentMethodRepo.GetAllAsync(),
                CancellationReasons = await _cancellationReasonRepo.GetAllAsync()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetCity(int id) => Json(await _cityRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertCity(City city)
        {
            if (city.Id == 0) await _cityRepo.AddAsync(city);
            else
            {
                var existing = await _cityRepo.GetByIdAsync(city.Id);
                if (existing != null)
                {
                    existing.Name = city.Name;
                    existing.PriceMultiplier = city.PriceMultiplier;
                    _cityRepo.Update(existing);
                }
            }
            await _cityRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var entity = await _cityRepo.GetByIdAsync(id);
            if (entity != null) { _cityRepo.Delete(entity); await _cityRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetService(int id) => Json(await _serviceRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertService(AdditionalService service)
        {
            if (service.Id == 0) await _serviceRepo.AddAsync(service);
            else
            {
                var existing = await _serviceRepo.GetByIdAsync(service.Id);
                if (existing != null)
                {
                    existing.Name = service.Name;
                    existing.Price = service.Price;
                    existing.IsPercentage = service.IsPercentage;
                    _serviceRepo.Update(existing);
                }
            }
            await _serviceRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService(int id)
        {
            var entity = await _serviceRepo.GetByIdAsync(id);
            if (entity != null) { _serviceRepo.Delete(entity); await _serviceRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus(int id) => Json(await _statusRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertStatus(OrderStatus status)
        {
            if (status.Id == 0) await _statusRepo.AddAsync(status);
            else
            {
                var existing = await _statusRepo.GetByIdAsync(status.Id);
                if (existing != null)
                {
                    existing.Name = status.Name;
                    _statusRepo.Update(existing);
                }
            }
            await _statusRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var entity = await _statusRepo.GetByIdAsync(id);
            if (entity != null) { _statusRepo.Delete(entity); await _statusRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicleClass(int id) => Json(await _vehicleClassRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertVehicleClass(VehicleClass model)
        {
            if (model.Id == 0) await _vehicleClassRepo.AddAsync(model);
            else _vehicleClassRepo.Update(model);
            await _vehicleClassRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVehicleClass(int id)
        {
            var entity = await _vehicleClassRepo.GetByIdAsync(id);
            if (entity != null) { _vehicleClassRepo.Delete(entity); await _vehicleClassRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentMethod(int id) => Json(await _paymentMethodRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertPaymentMethod(PaymentMethod model)
        {
            if (model.Id == 0) await _paymentMethodRepo.AddAsync(model);
            else _paymentMethodRepo.Update(model);
            await _paymentMethodRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var entity = await _paymentMethodRepo.GetByIdAsync(id);
            if (entity != null) { _paymentMethodRepo.Delete(entity); await _paymentMethodRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCancellationReason(int id) => Json(await _cancellationReasonRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertCancellationReason(CancellationReason model)
        {
            if (model.Id == 0) await _cancellationReasonRepo.AddAsync(model);
            else _cancellationReasonRepo.Update(model);
            await _cancellationReasonRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCancellationReason(int id)
        {
            var entity = await _cancellationReasonRepo.GetByIdAsync(id);
            if (entity != null) { _cancellationReasonRepo.Delete(entity); await _cancellationReasonRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}