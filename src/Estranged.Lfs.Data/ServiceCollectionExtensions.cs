using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Estranged.Lfs.Lock;

namespace Estranged.Lfs.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLfsData(this IServiceCollection services)
        {
            return services.AddSingleton<IObjectManager, ObjectManager>();
        }

        public static IServiceCollection AddLfsDictionaryAuthenticator(this IServiceCollection services, IReadOnlyDictionary<string, string> credentials)
        {
            return services.AddSingleton<IAuthenticator>(new DictionaryAuthenticator(credentials));
        }
    }
}
