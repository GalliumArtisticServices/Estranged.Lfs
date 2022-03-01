using System.Runtime.Serialization;

namespace Estranged.Lfs.Data.Entities
{
    [DataContract]
    public class BatchRef
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
