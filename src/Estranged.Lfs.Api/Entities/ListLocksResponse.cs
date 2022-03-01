using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Entities
{
    [DataContract]
    public class ListLocksResponse
    {
        [DataMember(Name = "locks")]
        public IEnumerable<LockData> Locks { get; set; } = Enumerable.Empty<LockData>();

        [DataMember(Name = "next_cursor")]
        public string NextCursor { get; set; }
    }
}
