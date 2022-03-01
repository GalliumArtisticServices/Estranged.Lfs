using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Entities
{
    [DataContract]
    public class LockVerifyResponse
    {
        [DataMember(Name = "ours")]
        public IEnumerable<LockData> Ours { get; set; } = Enumerable.Empty<LockData>();

        [DataMember(Name = "theirs")]
        public IEnumerable<LockData> Theirs { get; set; } = Enumerable.Empty<LockData>();

        [DataMember(Name = "next_cursor")]
        public string NextCursor { get; set; }
    }
}
