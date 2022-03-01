using System.Runtime.Serialization;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Entities
{
    [DataContract]
    public class LockExistsError
    {
        [DataMember(Name = "lock")]
        public LockData Lock { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; } = "already created lock";

        [DataMember(Name = "documentation_url")]
        public string DocumentationUrl { get; set; } = "https://lfs-server.com/docs/errors";

        [DataMember(Name = "request_id")]
        public string RequestId { get; set; }
    }

    [DataContract]
    public class GenericLockError
    {
        [DataMember(Name = "message")]
        public string Message { get; set; } = "already created lock";

        [DataMember(Name = "documentation_url")]
        public string DocumentationUrl { get; set; } = "https://lfs-server.com/docs/errors";

        [DataMember(Name = "request_id")]
        public string RequestId { get; set; }
    }
}
