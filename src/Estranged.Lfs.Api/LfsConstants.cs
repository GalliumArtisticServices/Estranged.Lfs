using Microsoft.Net.Http.Headers;

namespace Estranged.Lfs.Api
{
    public static class LfsConstants
    {
        public static MediaTypeHeaderValue LfsMediaType => new MediaTypeHeaderValue("application/vnd.git-lfs+json");

        public const string LockQueryPath = "path";

        public const string LockQueryId = "id";

        public const string LockQueryCursor = "cursor";

        public const string LockQueryLimit = "limit";

        public const string LockQueryRefspec = "refspec";

        public const int LockListLowerLimit = 10;

        public const int LockListUpperLimit = 100;

        public const string AuthorizationHeader = "Authorization";

        public const string BasicPrefix = "Basic";
    }
}
