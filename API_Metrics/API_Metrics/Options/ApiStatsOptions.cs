using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Metrics.Options
{
    public class ApiStatsOptions
    {
        /// <summary>Paths exactly to exclude (case-insensitive)</summary>
        public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Prefix-based excludes (e.g. "/static", "/swagger")</summary>
        public HashSet<string> ExcludedPathPrefixes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Query parameter names to mask in logs (case-insensitive)</summary>
        public HashSet<string> MaskQueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase){
        "password","token","access_token","api_key"
    };

        /// <summary>Header names to mask fully (case-insensitive)</summary>
        public HashSet<string> MaskHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase){
        "authorization","cookie","set-cookie"
    };

        /// <summary>Max number of recent errors to keep per endpoint</summary>
        public int MaxErrorRecordsPerEndpoint { get; set; } = 50;
    }

}
