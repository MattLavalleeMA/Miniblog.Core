using System.Collections.Generic;
using System.Threading.Tasks;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services
{
    public interface IBlogService
    {
        Task DeletePost(Post post);

        Task<IEnumerable<string>> GetCategories();

        Task<Post> GetPostById(string id);

        Task<Post> GetPostBySlug(string slug);

        Task<IEnumerable<Post>> GetPosts(int count, int skip = 0);

        Task<IEnumerable<Post>> GetPostsByCategory(string category);

        Task<PagedResultModel<Post>> GetPostsPaged(int pageSize, int pageNumber = 1, string category = "", bool isAdmin = false);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);

        Task SavePost(Post post);
    }
}