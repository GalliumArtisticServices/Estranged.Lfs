using System.Runtime.Serialization;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Entities
{
    [DataContract]
    public class DeleteLockRequest
    {
        [DataMember(Name = "force")]
        public bool Force { get; set; }

        [DataMember(Name = "ref")]
        public BatchRef Ref { get; set; }
    }
}
