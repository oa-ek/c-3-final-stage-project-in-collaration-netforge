using Microsoft.AspNetCore.Mvc;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.UI.Models;

namespace TaxiLink.UI.Controllers
{
    public class MarketingController : Controller
    {
        private readonly IGenericRepository<PromoCode> _promoRepo;
        private readonly IGenericRepository<NewsItem> _newsRepo;
        private readonly IWebHostEnvironment _env;

        public MarketingController(
            IGenericRepository<PromoCode> promoRepo,
            IGenericRepository<NewsItem> newsRepo,
            IWebHostEnvironment env)
        {
            _promoRepo = promoRepo;
            _newsRepo = newsRepo;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminViewModels.MarketingPageViewModel
            {
                PromoCodes = await _promoRepo.GetAllAsync(),
                NewsItems = await _newsRepo.GetAllAsync()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPromoCode(int id) => Json(await _promoRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertPromoCode(PromoCode promo)
        {
            if (promo.Id == 0)
            {
                promo.CurrentUses = 0;
                await _promoRepo.AddAsync(promo);
            }
            else
            {
                var existing = await _promoRepo.GetByIdAsync(promo.Id);
                if (existing != null)
                {
                    existing.Code = promo.Code;
                    existing.DiscountPercentage = promo.DiscountPercentage;
                    existing.ExpiryDate = promo.ExpiryDate;
                    existing.MaxUses = promo.MaxUses;
                    _promoRepo.Update(existing);
                }
            }
            await _promoRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePromoCode(int id)
        {
            var entity = await _promoRepo.GetByIdAsync(id);
            if (entity != null)
            {
                _promoRepo.Delete(entity);
                await _promoRepo.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetNewsItem(int id) => Json(await _newsRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertNewsItem(NewsItem news, IFormFile? ImageFile)
        {
            string? newImagePath = await ProcessNewsImageAsync(ImageFile, news.ImagePath);

            if (news.Id == 0)
            {
                news.PublishedAt = DateTime.Now;
                news.ImagePath = newImagePath;
                await _newsRepo.AddAsync(news);
            }
            else
            {
                var existing = await _newsRepo.GetByIdAsync(news.Id);
                if (existing != null)
                {
                    existing.Title = news.Title;
                    existing.Description = news.Description;
                    if (!string.IsNullOrEmpty(newImagePath)) existing.ImagePath = newImagePath;
                    _newsRepo.Update(existing);
                }
            }
            await _newsRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNewsItem(int id)
        {
            var entity = await _newsRepo.GetByIdAsync(id);
            if (entity != null)
            {
                _newsRepo.Delete(entity);
                await _newsRepo.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> ProcessNewsImageAsync(IFormFile? file, string? currentPath)
        {
            if (file == null || file.Length == 0) return currentPath;
            string uploadsFolder = Path.Combine(_env.WebRootPath, "news");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                await file.CopyToAsync(fileStream);
            return "/news/" + uniqueFileName;
        }
    }
}
