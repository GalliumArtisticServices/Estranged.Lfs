using System.Runtime.Serialization;

namespace Estranged.Lfs.Data.Entities
{
    [DataContract]
    public class LockRequest
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "ref")]
        public BatchRef Ref { get; set; }
    }
}
