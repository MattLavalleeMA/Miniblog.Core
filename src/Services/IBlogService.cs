using System.Collections.Generic;
using System.Threading.Tasks;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services
{
    public interface IBlogService
    {
        Task<IEnumerable<Post>> GetPosts(int count, int skip = 0);

        Task<IEnumerable<Post>> GetPostsByCategory(string category);

        Task<Post> GetPostBySlug(string slug);

        Task<Post> GetPostById(string id);

        Task<IEnumerable<string>> GetCategories();

        Task SavePost(Post post);

        Task DeletePost(Post post);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }
}
