using Newtonsoft.Json;

namespace Miniblog.Core.Configuration
{
    public class BlogSettings
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "My Miniblog.Core Blog";

        [JsonProperty("owner")]
        public string Owner { get; set; } = "Blog Owner";

        [JsonProperty("postsPerPage")]
        public int PostsPerPage { get; set; } = 2;

        [JsonProperty("commentsCloseAfterDays")]
        public int CommentsCloseAfterDays { get; set; } = 7;
    }
}