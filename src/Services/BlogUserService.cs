using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;
using Miniblog.Core.Configuration;
using WilderMinds.MetaWeblog;

namespace Miniblog.Core.Services
{
    public class BlogUserService : IUserService
    {
        private readonly UserSettings _userSettings;

        public BlogUserService(IOptionsMonitor<UserSettings> userSettings)
        {
            _userSettings = userSettings.CurrentValue;
        }

        /// <inheritdoc />
        public UserInfo GetUser(string userId)
        {
            if (_userSettings.Users == null)
            {
                return new UserInfo();
            }

            User user = _userSettings.Users.First(u => u.UserId == userId);

            return new UserInfo
            {
                email = user.Email,
                firstname = user.FirstName,
                lastname = user.LastName,
                nickname = user.Nickname,
                url = user.Url,
                userid = user.UserId
            };
        }

        public bool ValidateUser(string userId, string password)
        {
            if (_userSettings.Users == null)
            {
                return false;
            }

            User user = _userSettings.Users.First(u => u.UserId == userId);

            return userId == user.UserId && HashedPassword(password) == user.Password;
        }

        private string HashedPassword(string password)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(_userSettings.Salt);

            byte[] hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 1000,
                numBytesRequested: 256 / 8
            );

            string hashText = BitConverter
                .ToString(hashBytes)
                .Replace("-", string.Empty);

            return hashText;
        }
    }
}
