using System.Runtime.Serialization;

namespace Estranged.Lfs.Data.Entities
{
    [DataContract]
    public class LockData
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "locked_at")]
        public string LockedAt { get; set; }

        [DataMember(Name = "owner")]
        public LockOwner Owner { get; set; }
    }
}
