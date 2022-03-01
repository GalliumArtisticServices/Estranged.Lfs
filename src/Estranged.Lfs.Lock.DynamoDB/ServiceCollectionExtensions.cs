using Amazon.DynamoDBv2;
using Estranged.Lfs.Lock;
using Microsoft.Extensions.DependencyInjection;

namespace Estranged.Lfs.Lock.DynamoDB
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLfsDynamoDBLocK(this IServiceCollection services, AmazonDynamoDBClient client, string lockTableName, string distributedLockTableName)
        {
            return services.AddSingleton<ILockManager, DynamoDBLockManager>().AddSingleton(client).AddSingleton(lockTableName).AddSingleton(distributedLockTableName);
        }
    }
}
