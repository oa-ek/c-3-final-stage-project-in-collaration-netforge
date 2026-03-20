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
    public class MarketingService : IMarketingService
    {
        private readonly IGenericRepository<NewsItem> _newsRepo;
        private readonly IGenericRepository<PromoCode> _promoRepo;
        public MarketingService(IGenericRepository<NewsItem> newsRepo, IGenericRepository<PromoCode> promoRepo)
        {
            _newsRepo = newsRepo;
            _promoRepo = promoRepo;
        }

        public async Task<IEnumerable<NewsItem>> GetAllNewsAsync() => await _newsRepo.GetAllAsync();

        public async Task<NewsItem?> GetNewsByIdAsync(int id) => await _newsRepo.GetByIdAsync(id);

        public async Task CreateNewsAsync(NewsItem news)
        {
            news.PublishedAt = DateTime.Now; // автоматично ставлю дату створення
            await _newsRepo.AddAsync(news);
            await _newsRepo.SaveChangesAsync();
        }

        public async Task UpdateNewsAsync(NewsItem news)
        {
            _newsRepo.Update(news);
            await _newsRepo.SaveChangesAsync();
        }

        public async Task DeleteNewsAsync(int id)
        {
            var news = await _newsRepo.GetByIdAsync(id);
            if (news != null)
            {
                _newsRepo.Delete(news);
                await _newsRepo.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<PromoCode>> GetAllPromoCodesAsync() => await _promoRepo.GetAllAsync();

        public async Task<PromoCode?> GetPromoCodeByIdAsync(int id) => await _promoRepo.GetByIdAsync(id);

        public async Task CreatePromoCodeAsync(PromoCode promoCode)
        {
            promoCode.CurrentUses = 0; // новий код ще ніхто не використовував
            await _promoRepo.AddAsync(promoCode);
            await _promoRepo.SaveChangesAsync();
        }

        public async Task UpdatePromoCodeAsync(PromoCode promoCode)
        {
            _promoRepo.Update(promoCode);
            await _promoRepo.SaveChangesAsync();
        }

        public async Task DeletePromoCodeAsync(int id)
        {
            var code = await _promoRepo.GetByIdAsync(id);
            if (code != null)
            {
                _promoRepo.Delete(code);
                await _promoRepo.SaveChangesAsync();
            }
        }

        public async Task<bool> IsPromoCodeValidAsync(int id)
        {
            var promo = await _promoRepo.GetByIdAsync(id);
            if (promo == null) return false;

            // чи не закінчився час і чи не вичерпано ліміт використань
            return promo.ExpiryDate > DateTime.Now && promo.CurrentUses < promo.MaxUses;
        }
    }
}
