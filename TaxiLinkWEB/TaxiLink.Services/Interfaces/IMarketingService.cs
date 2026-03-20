using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Data.Repositories.Implementations;

namespace TaxiLink.Services.Interfaces
{
    public interface IMarketingService
    {
        // CRUD для Новин
        Task<IEnumerable<NewsItem>> GetAllNewsAsync();
        Task<NewsItem?> GetNewsByIdAsync(int id);
        Task CreateNewsAsync(NewsItem news);
        Task UpdateNewsAsync(NewsItem news);
        Task DeleteNewsAsync(int id);

        // CRUD для Промокодів + Бізнес-логіка
        Task<IEnumerable<PromoCode>> GetAllPromoCodesAsync();
        Task<PromoCode?> GetPromoCodeByIdAsync(int id);
        Task CreatePromoCodeAsync(PromoCode promoCode);
        Task UpdatePromoCodeAsync(PromoCode promoCode);
        Task DeletePromoCodeAsync(int id);

        // Бізнес-логіка: Перевірка, чи дійсний промокод
        Task<bool> IsPromoCodeValidAsync(int id);
    }
}
