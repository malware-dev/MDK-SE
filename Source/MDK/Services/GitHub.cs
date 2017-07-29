using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MDK.Services
{
    /// <summary>
    /// A service providing access to GitHub information
    /// </summary>
    public class GitHub
    {
        string _repositoryOwner;
        string _repository;
        string _userAgent;

        /// <summary>
        /// Creates a new instance of <see cref="GitHub"/>
        /// </summary>
        /// <param name="repositoryOwner"></param>
        /// <param name="repository"></param>
        /// <param name="userAgent"></param>
        public GitHub(string repositoryOwner, string repository, string userAgent)
        {
            _repositoryOwner = repositoryOwner;
            _repository = repository;
            _userAgent = userAgent;
        }

        /// <summary>
        /// Requests information about all available releases on the GitHub repository.
        /// </summary>
        /// <returns></returns>
        public async Task<GitHubRelease[]> ReleasesAsync()
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri($"https://api.github.com/repos/{_repositoryOwner}/{_repository}/releases"));
            request.UserAgent = _userAgent;
            request.Method = "GET";
            request.ContentType = "application/json";

            string result;
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                var stream = response.GetResponseStream();
                if (stream == null)
                    return new GitHubRelease[0];
                var reader = new StreamReader(stream);
                result = await reader.ReadToEndAsync();
            }

            return JsonConvert.DeserializeObject<GitHubRelease[]>(result);
        }
    }
}
