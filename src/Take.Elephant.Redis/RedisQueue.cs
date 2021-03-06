﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Take.Elephant.Redis
{
    public class RedisQueue<T> : StorageBase<T>, IBlockingQueue<T>
    {
        private readonly ISerializer<T> _serializer;
        private readonly ConcurrentQueue<Tuple<TaskCompletionSource<T>, CancellationTokenRegistration>> _promisesQueue = new ConcurrentQueue<Tuple<TaskCompletionSource<T>, CancellationTokenRegistration>>();
        private readonly SemaphoreSlim _semaphore;
        private readonly string _channelName;

        private ISubscriber _subscriber;

        public RedisQueue(string queueName, string configuration, ISerializer<T> serializer, int db = 0, CommandFlags readFlags = CommandFlags.None, CommandFlags writeFlags = CommandFlags.None)
            : this(queueName, StackExchange.Redis.ConnectionMultiplexer.Connect(configuration), serializer, db, readFlags, writeFlags)
        {
        }

        public RedisQueue(string queueName, IConnectionMultiplexer connectionMultiplexer, ISerializer<T> serializer, int db = 0, CommandFlags readFlags = CommandFlags.None, CommandFlags writeFlags = CommandFlags.None)
            : base(queueName, connectionMultiplexer, db, readFlags, writeFlags)
        {
            _channelName = $"{db}:{queueName}";
            _serializer = serializer;
            _semaphore = new SemaphoreSlim(1);
            SubscribeChannel();
        }

        public virtual async Task EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var database = GetDatabase();
            var shouldCommit = false;

            ITransaction transaction;
            if (database is ITransaction)
            {
                transaction = (ITransaction)database;
            }
            else if (database is IDatabase)
            {
                transaction = ((IDatabase)database).CreateTransaction();
                shouldCommit = true;
            }
            else
            {
                throw new NotSupportedException("The database instance type is not supported");
            }

            var enqueueTask = transaction.ListLeftPushAsync(Name, _serializer.Serialize(item), flags: WriteFlags);
            var publishTask = transaction.PublishAsync(_channelName, string.Empty, CommandFlags.FireAndForget);

            if (shouldCommit &&
                !await transaction.ExecuteAsync(WriteFlags).ConfigureAwait(false))
            {
                throw new Exception("The transaction has failed");
            }

            await Task.WhenAll(enqueueTask, publishTask).ConfigureAwait(false);
        }

        public virtual async Task<T> DequeueOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            var database = GetDatabase();
            var result = await database.ListRightPopAsync(Name, ReadFlags).ConfigureAwait(false);
            return !result.IsNull ? _serializer.Deserialize((string)result) : default(T);
        }

        public virtual Task<long> GetLengthAsync(CancellationToken cancellationToken = default)
        {
            var database = GetDatabase();
            return database.ListLengthAsync(Name, ReadFlags);
        }

        public virtual async Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>();
            using (var registration = cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                _promisesQueue.Enqueue(Tuple.Create(tcs, registration));
                await GetSubscriber().PublishAsync(_channelName, string.Empty, CommandFlags.FireAndForget);
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private void SubscribeChannel()
        {
            _subscriber = GetSubscriber();
            _subscriber.Subscribe(
                _channelName,
                async (c, v) =>
                {
                    Tuple<TaskCompletionSource<T>, CancellationTokenRegistration> promise = null;
                    await _semaphore.WaitAsync().ConfigureAwait(false);
                    var didDequeued = false;
                    try
                    {
                        if (_promisesQueue.TryDequeue(out promise) && !promise.Item1.Task.IsCanceled)
                        {
                            var database = GetDatabase();
                            var result = await database.ListRightPopAsync(Name).ConfigureAwait(false);
                            if (result.IsNull)
                            {
                                _promisesQueue.Enqueue(promise);
                            }
                            else
                            {
                                didDequeued = true;
                                var item = _serializer.Deserialize((string)result);
                                if (promise.Item1.TrySetResult(item))
                                {
                                    promise.Item2.Dispose();
                                }
                                else
                                {
                                    await EnqueueAsync(item).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!didDequeued && promise != null)
                        {
                            _promisesQueue.Enqueue(promise);
                        }
                        Trace.TraceError(ex.ToString());
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}