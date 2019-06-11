namespace Miniblog.Core.Configuration
{
    public class BlogSettings
    {
        public string Name { get; set; } = "My Miniblog.Core Blog";
        public string Owner { get; set; } = "Blog Owner";
        public int PostsPerPage { get; set; } = 2;
        public int CommentsCloseAfterDays { get; set; } = 7;
    }
}