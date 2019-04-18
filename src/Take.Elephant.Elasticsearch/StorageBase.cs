using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Take.Elephant.Elasticsearch
{
    public class StorageBase<T> : IQueryableStorage<T> where T : class
    {
        protected readonly ElasticClient ElasticClient;

        public StorageBase(string host, string username, string password, string defaultIndex)
        {
            var settings = new ConnectionSettings(new Uri(host))
                .BasicAuthentication(username, password)
                .DefaultIndex(defaultIndex);

            ElasticClient = new ElasticClient(settings);
        }

        public async Task<QueryResult<T>> QueryAsync<TResult>(Expression<Func<T, bool>> where, Expression<Func<T, TResult>> select, int skip, int take, CancellationToken cancellationToken)
        {
            var queryDescriptor = where.ParseToQueryContainer<T>();

            var result = await ElasticClient.SearchAsync<T>(s => s.Query(_ => queryDescriptor)
                .From(skip).Size(take), cancellationToken);

            return new QueryResult<T>(new AsyncEnumerableWrapper<T>(result.Documents), (int)result.Total);
        }

        public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            //TODO: Verificar se funciona
            var response = await ElasticClient.DocumentExistsAsync<T>(new DocumentPath<T>(id: key),
                cancellationToken: cancellationToken);

            return response.Exists;
        }

        public async Task<T> GetValueOrDefaultAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await ElasticClient.SearchAsync<T>(s => s.Query(q => q.Ids(a => a.Values(key.ToString()))));
            return result.Documents.FirstOrDefault();
        }

        public async Task<bool> TryAddAsync(string key, T value, bool overwrite = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (overwrite || !await ContainsKeyAsync(key, cancellationToken))
            {
                var result = await ElasticClient.IndexAsync(new IndexRequest<T>(value, id: key.ToString()), cancellationToken);
                return result.IsValid;
            }

            return false;
        }

        public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await ElasticClient.DeleteAsync<T>(key.ToString(), cancellationToken: cancellationToken);
            return result.IsValid;
        }

        protected string GetPropertyValue(T entity, string property)
        {
            return entity.GetType().GetProperties()
               .Single(p => p.Name == property)
               .GetValue(entity, null)
               .ToString();
        }
    }
}
