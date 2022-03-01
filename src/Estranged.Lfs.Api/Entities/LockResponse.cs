using System.Runtime.Serialization;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Entities
{
    [DataContract]
    public class LockResponse
    {
        [DataMember(Name = "lock")]
        public LockData Lock { get; set; }

    }
}
