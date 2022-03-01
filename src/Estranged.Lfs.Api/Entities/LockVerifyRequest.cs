using System.Runtime.Serialization;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Entities
{
    [DataContract]
    public class LockVerifyRequest
    {
        [DataMember(Name = "cursor")]
        public string Cursor { get; set; }

        [DataMember(Name = "limit")]
        public int Limit { get; set; }

        [DataMember(Name = "ref")]
        public BatchRef Ref { get; set; }
    }
}
