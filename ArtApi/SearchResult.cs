﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArtApi
{
    public class SearchResult
    {
        [JsonProperty("items")]
        public JToken Items { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("maxSearchResultsHit")]
        public bool MaxSearchResultsHit { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("searchText")]
        public string SearchText { get; set; }

        [JsonProperty("searchFrom")]
        public int SearchFrom { get; set; }

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }

        [JsonProperty("hideNudity")]
        public bool HideNudity { get; set; }
    }
}