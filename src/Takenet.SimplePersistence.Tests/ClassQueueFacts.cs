﻿using System;
using System.Threading.Tasks;
using Ploeh.AutoFixture;
using Xunit;

namespace Takenet.SimplePersistence.Tests
{
    public abstract class ClassQueueFacts<T> : QueueFacts<T> where T : class
    {
        [Fact(DisplayName = "EnqueueNullItemThrowsArgumentNullException")]
        public virtual async Task EnqueueNullItemThrowsArgumentNullException()
        {
            // Arrange
            var queue = Create();
            T item = null;

            // Act
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await queue.EnqueueAsync(item));   
        }
    }
}