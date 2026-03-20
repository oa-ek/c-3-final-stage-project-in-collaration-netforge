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
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IGenericRepository<SavedAddress> _addressRepo;
        private readonly IGenericRepository<Blacklist> _blacklistRepo;

        public UserService(
            IUserRepository userRepo,
            IGenericRepository<SavedAddress> addressRepo,
            IGenericRepository<Blacklist> blacklistRepo)
        {
            _userRepo = userRepo;
            _addressRepo = addressRepo;
            _blacklistRepo = blacklistRepo;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync() => await _userRepo.GetAllAsync();

        public async Task<User?> GetUserWithDetailsAsync(int id) => await _userRepo.GetUserWithDetailsAsync(id);

        public async Task CreateUserAsync(User user)
        {
            user.RegistrationDate = DateTime.Now;
            user.Rating = 5.0m; // новим клієнтам даю базовий рейтинг 5.0
            user.BonusBalance = 0;

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user != null)
            {
                _userRepo.Delete(user);
                await _userRepo.SaveChangesAsync();
            }
        }

        public async Task AddSavedAddressAsync(SavedAddress address)
        {
            await _addressRepo.AddAsync(address);
            await _addressRepo.SaveChangesAsync();
        }

        public async Task DeleteSavedAddressAsync(int addressId)
        {
            var address = await _addressRepo.GetByIdAsync(addressId);
            if (address != null)
            {
                _addressRepo.Delete(address);
                await _addressRepo.SaveChangesAsync();
            }
        }


        public async Task AddToBlacklistAsync(Blacklist blacklist)
        {
            blacklist.BlockedAt = DateTime.Now;
            await _blacklistRepo.AddAsync(blacklist);
            await _blacklistRepo.SaveChangesAsync();
        }

        public async Task RemoveFromBlacklistAsync(int blacklistId)
        {
            var block = await _blacklistRepo.GetByIdAsync(blacklistId);
            if (block != null)
            {
                _blacklistRepo.Delete(block);
                await _blacklistRepo.SaveChangesAsync();
            }
        }
    }
}
