// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Miniblog.Core.Configuration
{
    /// <summary>
    /// Defines the <see cref="BlobStorageSettings" />
    /// </summary>
    public class BlobStorageSettings
    {
        /// <summary>
        /// Gets or sets the ConnectionString
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the FilesContainerName
        /// </summary>
        public string FilesContainerName { get; set; }

        /// <summary>
        /// Gets or sets the PostsContainerName
        /// </summary>
        public string PostsContainerName { get; set; }
    }
}
