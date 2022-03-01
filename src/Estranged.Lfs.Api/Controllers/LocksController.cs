using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Estranged.Lfs.Api.Entities;
using Estranged.Lfs.Lock;
using Estranged.Lfs.Data.Entities;

namespace Estranged.Lfs.Api.Controllers
{
    [Route("locks")]
    public class LocksController : ControllerBase
    {
        private readonly ILockManager lockManager;
        public LocksController(ILockManager lockManager)
        {
            this.lockManager = lockManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLockAsync([FromBody] LockRequest request, CancellationToken token)
        {
            if (lockManager == null)
                return NotFound();

            try
            {
                (string username, string password) = Request.Headers.GetGitCredentials();

                (bool exists, LockData lockData) = await lockManager.CreateLock(username, request.Path, request.Ref, token);

                if (exists)
                {
                    return Conflict(new LockExistsError() { Lock = lockData });
                }

                LockResponse lockResponse = new LockResponse() { Lock = lockData };

                return Created(Request.Path.Add(new Microsoft.AspNetCore.Http.PathString(lockData.Id)), lockResponse);
            }
            catch (Exception e)
            {
                return StatusCode(500, new GenericLockError { Message = e.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocksAsync(CancellationToken token)
        {
            if (lockManager == null)
                return NotFound();

            try
            {
                (string username, string password) = Request.Headers.GetGitCredentials();

                var query = Request.Query;

                string path = query.ContainsKey(LfsConstants.LockQueryPath) ? query[LfsConstants.LockQueryPath][0] : null;
                string id = query.ContainsKey(LfsConstants.LockQueryId) ? query[LfsConstants.LockQueryId][0] : null;
                string cursor = query.ContainsKey(LfsConstants.LockQueryCursor) ? query[LfsConstants.LockQueryCursor][0] : null;
                int limit = query.ContainsKey(LfsConstants.LockQueryLimit) ? int.Parse(query[LfsConstants.LockQueryLimit][0]) : LfsConstants.LockListLowerLimit;
                string refspec = query.ContainsKey(LfsConstants.LockQueryRefspec) ? query[LfsConstants.LockQueryRefspec][0] : null;

                (string nextCursor, IEnumerable<LockData> locks) = await lockManager.ListLocks(username, limit, token, path, id, cursor, refspec);

                return Ok(new ListLocksResponse() { Locks = locks, NextCursor = nextCursor });
            }
            catch (Exception e)
            {
                return StatusCode(500, new GenericLockError { Message = e.Message });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyLocksAsync([FromBody] LockVerifyRequest request, CancellationToken token)
        {
            if (lockManager == null)
                return NotFound();

            try
            {
                (string username, string password) = Request.Headers.GetGitCredentials();

                (string nextCursor, IEnumerable<LockData> ours, IEnumerable<LockData> theirs) = await lockManager.VerifyLocks(username, request.Limit, token, request.Cursor, request.Ref);

                return Ok(new LockVerifyResponse() { Ours = ours, Theirs = theirs, NextCursor = nextCursor });
            }
            catch (Exception e)
            {
                return StatusCode(500, new GenericLockError { Message = e.Message });
            }
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> DeleteLockAsync([FromRoute]string id, [FromBody]DeleteLockRequest request, CancellationToken token)
        {
            if (lockManager == null)
                return NotFound();

            try
            {
                (string username, string password) = Request.Headers.GetGitCredentials();

                (bool unauthorized, LockData lockData) = await lockManager.DeleteLock(username, id, request.Force, token, request.Ref);

                if (unauthorized)
                {
                    return StatusCode(403, new GenericLockError { Message = "Not authorized to delete lock, did you mean to use force?" });
                }

                return Ok(new LockResponse() { Lock = lockData });
            }
            catch (Exception e)
            {
                return StatusCode(500, new GenericLockError { Message = e.Message});
            }
        }
    }
}
