using System;
using System.Threading;
using System.Threading.Tasks;
using DynamoLock;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Estranged.Lfs.Data;
using Estranged.Lfs.Data.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Estranged.Lfs.Lock.DynamoDB
{
    public class DynamoDBLockManager : ILockManager
    {
        private IDistributedLockManager distributedManager;
        private AmazonDynamoDBClient dynamoClient;
        private readonly string lockTableName;

        private const string idIndex = "IdIndex";
        private const string refspecIndex = "RefspecIndex";

        private const string pathKey = "Path";
        private const string refspecKey = "Refspec";
        private const string idKey = "Id";
        private const string lockedAtKey = "LockedAt";
        private const string ownerKey = "Owner";

        public bool Started { get; private set; }

        public DynamoDBLockManager(AmazonDynamoDBClient client, string lockTableName, string distributedLockTableName)
        {
            distributedManager = new DynamoDbLockManager(client, distributedLockTableName, NullLoggerFactory.Instance);
            dynamoClient = client;
            this.lockTableName = lockTableName;
        }

        public async Task StartAsync()
        {
            if (Started == true)
                return;

            await distributedManager.Start();

            try
            {
                var response = await dynamoClient.DescribeTableAsync(lockTableName);
            }
            catch (ResourceNotFoundException)
            {
                var createRequest = new CreateTableRequest(lockTableName,
                    new List<KeySchemaElement>()
                    {
                        new KeySchemaElement(pathKey, KeyType.HASH),
                        new KeySchemaElement(refspecKey, KeyType.RANGE)
                    },
                     new List<AttributeDefinition>()
                    {
                        new AttributeDefinition(pathKey, ScalarAttributeType.S),
                        new AttributeDefinition(refspecKey, ScalarAttributeType.S)
                    },
                     new ProvisionedThroughput()
                     {
                         ReadCapacityUnits = 1,
                         WriteCapacityUnits = 1
                     }
                     )
                {
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                    {
                        new GlobalSecondaryIndex
                        {
                            IndexName = idIndex,
                            KeySchema = new List<KeySchemaElement> { new KeySchemaElement(idKey, KeyType.HASH) },
                            Projection = new Projection() { ProjectionType = ProjectionType.ALL },
                            ProvisionedThroughput = new ProvisionedThroughput()
                            {
                                ReadCapacityUnits = 1,
                                WriteCapacityUnits = 1
                            }
                        },
                        new GlobalSecondaryIndex
                        {
                            IndexName = refspecIndex,
                            KeySchema = new List<KeySchemaElement> { new KeySchemaElement(refspecKey, KeyType.HASH) },
                            Projection = new Projection() { ProjectionType = ProjectionType.ALL },
                            ProvisionedThroughput = new ProvisionedThroughput()
                            {
                                ReadCapacityUnits = 1,
                                WriteCapacityUnits = 1
                            }
                        }
                    }
                };

                var createResponse = await dynamoClient.CreateTableAsync(createRequest);

                int i = 0;
                bool created = false;
                while ((i < 20) && (!created))
                {
                    try
                    {
                        await Task.Delay(1000);
                        var poll = await dynamoClient.DescribeTableAsync(lockTableName);
                        created = (poll.Table.TableStatus == TableStatus.ACTIVE);
                        i++;
                    }
                    catch (ResourceNotFoundException)
                    {
                    }
                }
            }
        }

        public async Task StopAsync()
        {
            await distributedManager.Stop();
        }

        private const string globalRefspec = "refs/global";

        public async Task<(bool exists, LockData)> CreateLock(string owner, string path, BatchRef refspec, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            bool aquired = false;

            try
            {
                aquired = await distributedManager.AcquireLock(path);

                token.ThrowIfCancellationRequested();

                if (!aquired)
                    throw new TimeoutException(string.Format("Timed out trying to aquire distributed lock for path {0}", path));

                if (refspec == null || string.IsNullOrEmpty(refspec.Name))
                {
                    refspec = new BatchRef() { Name = globalRefspec };
                }

                var queryRequest = new QueryRequest
                {
                    TableName = lockTableName,
                    KeyConditionExpression = "Path = :v_Path",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":v_Path", new AttributeValue { S = path} }
                    }
                };

                var queryResponse = await dynamoClient.QueryAsync(queryRequest, token);

                foreach (Dictionary<string, AttributeValue> item in queryResponse.Items)
                {
                    if (item[refspecKey].S == globalRefspec || item[refspecKey].S == refspec.Name)
                    {
                        return (true, LockDataFromAttributes(item));
                    }
                }

                LockData lockData = new LockData
                {
                    Path = path,
                    Id = Guid.NewGuid().ToString(),
                    LockedAt = DateTime.UtcNow.ToString("o"),
                    Owner = new LockOwner { Name = owner }
                };

                var createRequest = new PutItemRequest
                {
                    TableName = lockTableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { pathKey, new AttributeValue { S = path } },
                        { refspecKey, new AttributeValue { S = refspec.Name } },
                        { idKey, new AttributeValue { S = lockData.Id }},
                        { lockedAtKey, new AttributeValue { S = lockData.LockedAt } },
                        { ownerKey, new AttributeValue { S = owner } }
                    }
                };

                await dynamoClient.PutItemAsync(createRequest, token);

                return (false, lockData);
            }
            finally
            {
                if (aquired) await distributedManager.ReleaseLock(path);
            }
        }

        public async Task<(bool unauthorized, LockData lockData)> DeleteLock(string owner, string id, bool force, CancellationToken token, BatchRef? refspec = null)
        {
            token.ThrowIfCancellationRequested();

            var queryRequest = new QueryRequest
            {
                TableName = lockTableName,
                IndexName = idIndex,
                KeyConditionExpression = "Id = :v_Id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_Id", new AttributeValue { S = id } }
                },
                ScanIndexForward = true
            };

            var queryResponse = await dynamoClient.QueryAsync(queryRequest, token);

            if(queryResponse.Items.Count == 0)
            {
                throw new KeyNotFoundException(String.Format("The lock with Id: {0} does not exist.", id));
            }

            LockData lockData = LockDataFromAttributes(queryResponse.Items[0]);

            if (lockData.Owner.Name != owner && force == false)
            {
                return (true, lockData);
            }

            bool aquired = false;

            try
            {
                aquired = await distributedManager.AcquireLock(lockData.Path);

                token.ThrowIfCancellationRequested();

                var deleteRequest = new DeleteItemRequest(
                    lockTableName,
                    new Dictionary<string, AttributeValue>
                    {
                        { pathKey, new AttributeValue { S = lockData.Path } },
                        { refspecKey, new AttributeValue { S = queryResponse.Items[0][refspecKey].S } }
                    },
                    ReturnValue.NONE
                    );

                var deleteResponse = await dynamoClient.DeleteItemAsync(deleteRequest, token);

                return (false, lockData);
            }
            finally
            {
                if (aquired) await distributedManager.ReleaseLock(lockData.Path);
            }
        }

        private static Dictionary<string, AttributeValue>? LastKeyFromCursor(string? cursor)
        {
            if (cursor == null)
                return null;

            string[] keyParts = cursor.Split(":");

            return new Dictionary<string, AttributeValue>
            {
                {pathKey, new AttributeValue { S = keyParts[0] } },
                {refspecKey, new AttributeValue { S = keyParts[1] } }
            };
        }

        private static string LastKeyToCursor(Dictionary<string, AttributeValue> lastKey)
        {
            if (lastKey == null || lastKey.Count == 0)
                return null;

            return string.Concat(lastKey[pathKey].S, ":", lastKey[refspecKey].S);
        }

        private static IEnumerable<LockData> ItemsToLockData(List<Dictionary<string, AttributeValue>> items)
        {
            LockData[] lockData = new LockData[items.Count];

            int i = 0;

            foreach (Dictionary<string, AttributeValue> item in items)
            {
                lockData[i] = LockDataFromAttributes(item);
                i++;
            }

            return lockData;
        }

        public async Task<(string nextCursor, IEnumerable<LockData> locks)> ListLocks(string owner, int limit, CancellationToken token, string? path = null, string? id = null, string? cursor = null, string? refspec = null)
        {
            token.ThrowIfCancellationRequested();

            if (id != null)
            {
                var queryRequest = new QueryRequest
                {
                    TableName = lockTableName,
                    IndexName = idIndex,
                    KeyConditionExpression = "Id = :v_Id",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_Id", new AttributeValue { S = id } }
                },
                    ScanIndexForward = true
                };

                var queryResponse = await dynamoClient.QueryAsync(queryRequest, token);

                if (queryResponse.Items.Count == 0)
                {
                    return (null, Enumerable.Empty<LockData>());
                }

                LockData lockData = LockDataFromAttributes(queryResponse.Items[0]);

                return (null, new LockData[] { lockData });
            }

            else if (path != null)
            {
                if (refspec != null)
                {
                    var itemRequest = new GetItemRequest
                    {
                        TableName = lockTableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            {pathKey, new AttributeValue { S = path } },
                            {refspecKey, new AttributeValue { S = refspec } }
                        }
                    };

                    var itemResponse = await dynamoClient.GetItemAsync(itemRequest, token);

                    if (itemResponse.IsItemSet == false || itemResponse.Item.Count == 0)
                    {
                        return (null, Enumerable.Empty<LockData>());
                    }

                    LockData lockData = LockDataFromAttributes(itemResponse.Item);

                    return (null, new LockData[] { lockData });
                }
                else
                {
                    Dictionary<string, AttributeValue>? lastKeyEvaluated = LastKeyFromCursor(cursor);

                    var queryRequest = new QueryRequest
                    {
                        TableName = lockTableName,
                        KeyConditionExpression = "Path = :v_Path",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            { ":v_Path", new AttributeValue { S = path } }
                        },
                        Limit = limit,
                        ExclusiveStartKey = lastKeyEvaluated
                    };

                    var queryResponse = await dynamoClient.QueryAsync(queryRequest, token);

                    string newCursor = LastKeyToCursor(queryResponse.LastEvaluatedKey);

                    return (newCursor, ItemsToLockData(queryResponse.Items));
                }
            }
            else if (refspec != null)
            {
                Dictionary<string, AttributeValue>? lastKeyEvaluated = LastKeyFromCursor(cursor);

                var queryRequest = new QueryRequest
                {
                    TableName = lockTableName,
                    IndexName = refspecIndex,
                    KeyConditionExpression = "Refspec = :v_Refspec",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_Refspec", new AttributeValue { S = refspec } }
                },
                    ScanIndexForward = true,
                    Limit = limit,
                    ExclusiveStartKey = lastKeyEvaluated
                };

                var queryResponse = await dynamoClient.QueryAsync(queryRequest, token);

                string newCursor = LastKeyToCursor(queryResponse.LastEvaluatedKey);
                return (newCursor, ItemsToLockData(queryResponse.Items));
            }

            return (null, Enumerable.Empty<LockData>());
        }

        public async Task<(string nextCursor, IEnumerable<LockData> ours, IEnumerable<LockData> theirs)> VerifyLocks(string owner, int limit, CancellationToken token, string? cursor = null, BatchRef? refspec = null)
        {
            token.ThrowIfCancellationRequested();

            Dictionary<string, AttributeValue>? lastKeyEvaluated = LastKeyFromCursor(cursor);

            var scanRequest = new ScanRequest(lockTableName)
            {
                Limit = limit,
                ExclusiveStartKey = lastKeyEvaluated
            };

            var scanResponse = await dynamoClient.ScanAsync(scanRequest, token);

            List<LockData> ours = new List<LockData>();
            List<LockData> theirs = new List<LockData>();

            foreach (var item in scanResponse.Items)
            {
                LockData lockData = LockDataFromAttributes(item);

                if (refspec != null && item[refspecKey].S != refspec.Name)
                    continue;

                if (lockData.Owner.Name == owner)
                    ours.Add(lockData);
                else
                    theirs.Add(lockData);
            }

            string newCursor = LastKeyToCursor(scanResponse.LastEvaluatedKey);

            return (newCursor, ours, theirs);
        }

        private static LockData LockDataFromAttributes(Dictionary<string, AttributeValue> attributes)
        {
            return new LockData
            {
                Path = attributes[pathKey].S,
                Id = attributes[idKey].S,
                LockedAt = attributes[lockedAtKey].S,
                Owner = new LockOwner { Name = attributes[ownerKey].S }
            };
        }
    }
}
