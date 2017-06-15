﻿using AutoMapper;
using Microsoft.Extensions.Logging;
using Ombi.Api.TheMovieDb;
using Ombi.Api.TheMovieDb.Models;
using Ombi.Core.Models.Requests;
using Ombi.Core.Models.Requests.Movie;
using Ombi.Core.Models.Search;
using Ombi.Core.Rules;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;
using Ombi.Store.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace Ombi.Core.Engine
{
    public class MovieSearchEngine : BaseMediaEngine, IMovieEngine
    {
        public MovieSearchEngine(IPrincipal identity, IRequestServiceMain service, IMovieDbApi movApi, IMapper mapper,
            ISettingsService<PlexSettings> plexSettings,
            ISettingsService<EmbySettings> embySettings, IPlexContentRepository repo,
            ILogger<MovieSearchEngine> logger, IRuleEvaluator r)
            : base(identity, service, r)
        {
            MovieApi = movApi;
            Mapper = mapper;
            PlexSettings = plexSettings;
            EmbySettings = embySettings;
            Logger = logger;
            PlexContentRepo = repo;
        }

        private IMovieDbApi MovieApi { get; }
        private IMapper Mapper { get; }
        private ISettingsService<PlexSettings> PlexSettings { get; }
        private ISettingsService<EmbySettings> EmbySettings { get; }
        private ILogger<MovieSearchEngine> Logger { get; }
        private IPlexContentRepository PlexContentRepo { get; }

        public async Task<IEnumerable<SearchMovieViewModel>> LookupImdbInformation(
            IEnumerable<SearchMovieViewModel> movies)
        {
            var searchMovieViewModels
                = movies as IList<SearchMovieViewModel> ?? movies.ToList();
            if (searchMovieViewModels == null || !searchMovieViewModels.Any())
                return new List<SearchMovieViewModel>();

            var retVal = new List<SearchMovieViewModel>();
            var dbMovies = await GetMovieRequests();

            var plexSettings = await PlexSettings.GetSettingsAsync();
            var embySettings = await EmbySettings.GetSettingsAsync();

            foreach (var m in searchMovieViewModels)
            {
                var movieInfo = await MovieApi.GetMovieInformationWithVideo(m.Id);
                var viewMovie = Mapper.Map<SearchMovieViewModel>(movieInfo);

                retVal.Add(await ProcessSingleMovie(viewMovie, dbMovies, plexSettings, embySettings));
            }
            return retVal;
        }

        public async Task<SearchMovieViewModel> LookupImdbInformation(int theMovieDbId)
        {
            var dbMovies = await GetMovieRequests();

            var plexSettings = await PlexSettings.GetSettingsAsync();
            var embySettings = await EmbySettings.GetSettingsAsync();

            var movieInfo = await MovieApi.GetMovieInformationWithVideo(theMovieDbId);
            var viewMovie = Mapper.Map<SearchMovieViewModel>(movieInfo);

            return await ProcessSingleMovie(viewMovie, dbMovies, plexSettings, embySettings, true);
        }

        public async Task<IEnumerable<SearchMovieViewModel>> Search(string search)
        {
            using (MiniProfiler.Current.Step("Starting Movie Search Engine"))
            using (MiniProfiler.Current.Step("Searching Movie"))
            {
                var result = await MovieApi.SearchMovie(search);

                using (MiniProfiler.Current.Step("Fin API, Transforming"))
                {
                    if (result != null)
                    {
                        Logger.LogDebug("Search Result: {result}", result);
                        return await TransformMovieResultsToResponse(result);
                    }
                }


                return null;
            }
        }


        public async Task<IEnumerable<SearchMovieViewModel>> PopularMovies()
        {
            var result = await MovieApi.PopularMovies();
            if (result != null)
            {
                Logger.LogDebug("Search Result: {result}", result);
                return await TransformMovieResultsToResponse(result);
            }
            return null;
        }

        public async Task<IEnumerable<SearchMovieViewModel>> TopRatedMovies()
        {
            var result = await MovieApi.TopRated();
            if (result != null)
            {
                Logger.LogDebug("Search Result: {result}", result);
                return await TransformMovieResultsToResponse(result);
            }
            return null;
        }

        public async Task<IEnumerable<SearchMovieViewModel>> UpcomingMovies()
        {
            var result = await MovieApi.Upcoming();
            if (result != null)
            {
                Logger.LogDebug("Search Result: {result}", result);
                return await TransformMovieResultsToResponse(result);
            }
            return null;
        }

        public async Task<IEnumerable<SearchMovieViewModel>> NowPlayingMovies()
        {
            var result = await MovieApi.NowPlaying();
            if (result != null)
            {
                Logger.LogDebug("Search Result: {result}", result);
                return await TransformMovieResultsToResponse(result);
            }
            return null;
        }

        private async Task<List<SearchMovieViewModel>> TransformMovieResultsToResponse(
            IEnumerable<MovieSearchResult> movies)
        {

            var viewMovies = new List<SearchMovieViewModel>();
            Dictionary<int, MovieRequestModel> dbMovies;
            Settings.Models.External.PlexSettings plexSettings;
            Settings.Models.External.EmbySettings embySettings;
            using (MiniProfiler.Current.Step("Gettings Movies and Settings"))
            {
                dbMovies = await GetMovieRequests();

                plexSettings = await PlexSettings.GetSettingsAsync();
                embySettings = await EmbySettings.GetSettingsAsync();
            }
            foreach (var movie in movies)
            {
                viewMovies.Add(await ProcessSingleMovie(movie, dbMovies, plexSettings, embySettings));
            }
            return viewMovies;
        }

        private async Task<SearchMovieViewModel> ProcessSingleMovie(SearchMovieViewModel viewMovie,
            Dictionary<int, MovieRequestModel> existingRequests, PlexSettings plexSettings, EmbySettings embySettings, bool lookupExtraInfo = false)
        {

            if (plexSettings.Enable)
            {
                if (lookupExtraInfo)
                {
                    var showInfo = await MovieApi.GetMovieInformation(viewMovie.Id);
                    viewMovie.Id = showInfo.Id; // TheMovieDbId
                    var item = await PlexContentRepo.Get(showInfo.ImdbId);
                    if (item != null)
                    {
                        viewMovie.Available = true;
                        viewMovie.PlexUrl = item.Url;
                    }
                }

                //        var content = PlexContentRepository.GetAll();
                //        var plexMovies = PlexChecker.GetPlexMovies(content);

                //        var plexMovie = PlexChecker.GetMovie(plexMovies.ToArray(), movie.Title,
                //            movie.ReleaseDate?.Year.ToString(),
                //            viewMovie.ImdbId);
                //        if (plexMovie != null)
                //        {
                //            viewMovie.Available = true;
                //            viewMovie.PlexUrl = plexMovie.Url;
                //        }
            }
            if (embySettings.Enable)
            {
                //        var embyContent = EmbyContentRepository.GetAll();
                //        var embyMovies = EmbyChecker.GetEmbyMovies(embyContent);

                //        var embyMovie = EmbyChecker.GetMovie(embyMovies.ToArray(), movie.Title,
                //            movie.ReleaseDate?.Year.ToString(), viewMovie.ImdbId);
                //        if (embyMovie != null)
                //        {
                //            viewMovie.Available = true;
                //        }
            }

            if (existingRequests.ContainsKey(viewMovie.Id)) // Do we already have a request for this?
            {
                var requestedMovie = existingRequests[viewMovie.Id];

                viewMovie.Requested = true;
                viewMovie.Approved = requestedMovie.Approved;
                viewMovie.Available = requestedMovie.Available;
            }

            RunSearchRules(viewMovie);

            return viewMovie;
        }


        private async Task<SearchMovieViewModel> ProcessSingleMovie(MovieSearchResult movie,
            Dictionary<int, MovieRequestModel> existingRequests, PlexSettings plexSettings, EmbySettings embySettings)
        {
            var viewMovie = Mapper.Map<SearchMovieViewModel>(movie);
            return await ProcessSingleMovie(viewMovie, existingRequests, plexSettings, embySettings);
        }
    }
}