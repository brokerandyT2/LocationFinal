using Akavache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;

namespace Location.Core.Infrastructure.Services
{
    public class AkavacheCacheService : ICacheService
    {
        private readonly TimeSpan defaultExpiration = TimeSpan.FromDays(7);

        public AkavacheCacheService()
        {
            BlobCache.ApplicationName = "MyApp"; // change as appropriate
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            BlobCache.LocalMachine.InsertObject(key, value, expiration ?? defaultExpiration);
            return;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                return await BlobCache.LocalMachine.GetObject<T>(key).ToTask<T>();
                 
            }
            catch (KeyNotFoundException)
            {
                return default;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var objects = await BlobCache.LocalMachine.GetAllKeys().ToTask();
            return objects.Contains(key);
        }

        public Task RemoveAsync(string key)
        {
            return BlobCache.LocalMachine.Invalidate(key).ToTask();
        }

        public Task ClearAllAsync()
        {
            return BlobCache.LocalMachine.InvalidateAll().ToTask();
        }
    }

}
