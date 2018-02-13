﻿using System.Globalization;
using System.Linq;

namespace Ombi.Helpers
{
    public class PosterPathHelper
    {
        public static string FixPosterPath(string poster)
        {
            // https://image.tmdb.org/t/p/w150/fJAvGOitU8y53ByeHnM4avtKFaG.jpg 

            if (poster.Contains("image.tmdb.org", CompareOptions.IgnoreCase))
            {
                // Somehow we have a full path here for the poster, we only want the last segment
                var posterSegments = poster.Split('/');
                return posterSegments.Last();
            }

            return poster;
        }
    }
}