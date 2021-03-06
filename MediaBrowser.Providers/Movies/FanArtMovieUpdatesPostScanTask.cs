﻿using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Music;
using MediaBrowser.Providers.TV;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Movies
{
    class FanartMovieUpdatesPostScanTask : ILibraryPostScanTask
    {
        private const string UpdatesUrl = "http://webservice.fanart.tv/v3/movies/latest?api_key={0}&date={1}";

        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// The _config
        /// </summary>
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public FanartMovieUpdatesPostScanTask(IJsonSerializer jsonSerializer, IServerConfigurationManager config, ILogger logger, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _jsonSerializer = jsonSerializer;
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var options = FanartSeriesProvider.Current.GetFanartOptions();

            if (!options.EnableAutomaticUpdates)
            {
                progress.Report(100);
                return;
            }

            var path = FanartMovieImageProvider.GetMoviesDataPath(_config.CommonApplicationPaths);

			_fileSystem.CreateDirectory(path);
            
            var timestampFile = Path.Combine(path, "time.txt");

            var timestampFileInfo = _fileSystem.GetFileInfo(timestampFile);

            // Don't check for updates every single time
            if (timestampFileInfo.Exists && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(timestampFileInfo)).TotalDays < 3)
            {
                return;
            }

            // Find out the last time we queried for updates
			var lastUpdateTime = timestampFileInfo.Exists ? _fileSystem.ReadAllText(timestampFile, Encoding.UTF8) : string.Empty;

            var existingDirectories = Directory.EnumerateDirectories(path).Select(Path.GetFileName).ToList();

            // If this is our first time, don't do any updates and just record the timestamp
            if (!string.IsNullOrEmpty(lastUpdateTime))
            {
                var moviesToUpdate = await GetMovieIdsToUpdate(existingDirectories, lastUpdateTime, options, cancellationToken).ConfigureAwait(false);

                progress.Report(5);

                await UpdateMovies(moviesToUpdate, progress, cancellationToken).ConfigureAwait(false);
            }

            var newUpdateTime = Convert.ToInt64(DateTimeToUnixTimestamp(DateTime.UtcNow)).ToString(UsCulture);
            
			_fileSystem.WriteAllText(timestampFile, newUpdateTime, Encoding.UTF8);

            progress.Report(100);
        }

        private async Task<IEnumerable<string>> GetMovieIdsToUpdate(IEnumerable<string> existingIds, string lastUpdateTime, FanartOptions options, CancellationToken cancellationToken)
        {
            var url = string.Format(UpdatesUrl, FanartArtistProvider.ApiKey, lastUpdateTime);

            var clientKey = options.UserApiKey;
            if (!string.IsNullOrWhiteSpace(clientKey))
            {
                url += "&client_key=" + clientKey;
            }

            // First get last time
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                EnableHttpCompression = true,
                ResourcePool = FanartArtistProvider.Current.FanArtResourcePool

            }).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // If empty fanart will return a string of "null", rather than an empty list
                    if (string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        return new List<string>();
                    }

                    var updates = _jsonSerializer.DeserializeFromString<List<RootObject>>(json);

                    var existingDictionary = existingIds.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

                    return updates.SelectMany(i =>
                    {
                        var list = new List<string>();

                        if (!string.IsNullOrWhiteSpace(i.imdb_id))
                        {
                            list.Add(i.imdb_id);
                        }
                        if (!string.IsNullOrWhiteSpace(i.tmdb_id))
                        {
                            list.Add(i.tmdb_id);
                        }

                        return list;

                    }).Where(existingDictionary.ContainsKey);
                }
            }
        }

        private async Task UpdateMovies(IEnumerable<string> idList, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = idList.ToList();
            var numComplete = 0;

            foreach (var id in list)
            {
                _logger.Info("Updating movie " + id);

                await FanartMovieImageProvider.Current.DownloadMovieJson(id, cancellationToken).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= list.Count;
                percent *= 95;

                progress.Report(percent + 5);
            }
        }

        /// <summary>
        /// Dates the time to unix timestamp.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>System.Double.</returns>
        private static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1).ToUniversalTime()).TotalSeconds;
        }

        public class RootObject
        {
            public string tmdb_id { get; set; }
            public string imdb_id { get; set; }
            public string name { get; set; }
            public string new_images { get; set; }
            public string total_images { get; set; }
        }
    }
}
