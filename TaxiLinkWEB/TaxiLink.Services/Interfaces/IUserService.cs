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
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserWithDetailsAsync(int id); 
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
        Task AddSavedAddressAsync(SavedAddress address);
        Task DeleteSavedAddressAsync(int addressId);
        Task AddToBlacklistAsync(Blacklist blacklist);
        Task RemoveFromBlacklistAsync(int blacklistId);
    }
}
