using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Services.Interfaces
{
    public interface IUserService
    {
        // Головний CRUD для юзера
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserWithDetailsAsync(int id); 
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);

        // Залежний CRUD для адрес
        Task AddSavedAddressAsync(SavedAddress address);
        Task DeleteSavedAddressAsync(int addressId);

        // Залежний CRUD для чор списку
        Task AddToBlacklistAsync(Blacklist blacklist);
        Task RemoveFromBlacklistAsync(int blacklistId);
    }
}
