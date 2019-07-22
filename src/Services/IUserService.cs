using WilderMinds.MetaWeblog;

namespace Miniblog.Core.Services
{
    public interface IUserService
    {
        UserInfo GetUser(string userId);

        bool ValidateUser(string userId, string password);
    }
}
