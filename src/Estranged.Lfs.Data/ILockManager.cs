using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Lock
{
    public interface ILockManager
    {
        Task<(bool exists, LockData)> CreateLock(string owner, string path, BatchRef refspec, CancellationToken token);

        Task<(string nextCursor, IEnumerable<LockData> locks)> ListLocks(string owner, int limit, CancellationToken token, string path = null, string id = null, string cursor = null, string refspec = null);

        Task<(string nextCursor, IEnumerable<LockData> ours, IEnumerable<LockData> theirs)> VerifyLocks(string owner, int limit, CancellationToken token, string cursor = null, BatchRef refspec = null);

        Task<(bool unauthorized, LockData lockData)> DeleteLock(string owner, string id, bool force, CancellationToken token, BatchRef refspec = null);
    }
}
