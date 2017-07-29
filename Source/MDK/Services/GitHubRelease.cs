using System;
using Newtonsoft.Json;

namespace MDK.Services
{
    /// <summary>
    /// Provides basic information about a single release on GitHub.
    /// </summary>
    public class GitHubRelease
    {
        /// <summary>
        /// The ID of this release
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The release tag (version)
        /// </summary>
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// The name of this release
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Determines whether this is a prerelease
        /// </summary>
        public bool Prerelease { get; set; }

        /// <summary>
        /// The time when this release was published
        /// </summary>
        [JsonProperty("published_at")]
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// A description of this release
        /// </summary>
        public string Body { get; set; }
    }
}
