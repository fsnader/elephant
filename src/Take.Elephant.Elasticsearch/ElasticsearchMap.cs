using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Nest;

namespace Take.Elephant.Elasticsearch
{
    public class ElasticsearchMap<TKey, T> : StorageBase<T>, IMap<TKey, T> where T : class
    {
        public ElasticsearchMap(string host, string username, string password, string defaultIndex)
            : base(host, username, password, defaultIndex)
        {
        }

        public async Task<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await ContainsKeyAsync(key.ToString(), cancellationToken);
        }

        public async Task<T> GetValueOrDefaultAsync(TKey key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetValueOrDefaultAsync(key.ToString(), cancellationToken);
        }

        public async Task<bool> TryAddAsync(TKey key, T value, bool overwrite = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TryAddAsync(key.ToString(), value, overwrite, cancellationToken);
        }

        public async Task<bool> TryRemoveAsync(TKey key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await DeleteAsync(key.ToString(), cancellationToken);
        }
    }
}
