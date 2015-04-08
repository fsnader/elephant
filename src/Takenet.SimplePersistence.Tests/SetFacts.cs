﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFluent;
using Ploeh.AutoFixture;
using Xunit;

namespace Takenet.SimplePersistence.Tests
{
    public abstract class SetFacts<T> : AssertionBase
    {
        private readonly Fixture _fixture;

        protected SetFacts()
        {
            _fixture = new Fixture();
        }

        public abstract ISet<T> Create();

        [Fact(DisplayName = "AddNewItemSucceeds")]
        public virtual async Task AddNewItemSucceeds()
        {
            // Arrange
            var set = Create();
            var item = _fixture.Create<T>();

            // Act
            await set.AddAsync(item);

            // Assert
            AssertIsTrue(await set.ContainsAsync(item));
        }

        [Fact(DisplayName = "AddExistingItemSucceeds")]
        public virtual async Task AddExistingItemSucceeds()
        {
            // Arrange
            var set = Create();
            var item = _fixture.Create<T>();
            await set.AddAsync(item);

            // Act
            await set.AddAsync(item);

            // Assert
            AssertIsTrue(await set.ContainsAsync(item));
        }

        [Fact(DisplayName = "TryRemoveExistingItemSucceeds")]
        public virtual async Task TryRemoveExistingItemSucceeds()
        {
            // Arrange
            var set = Create();
            var item = _fixture.Create<T>();
            await set.AddAsync(item);

            // Act
            var result = await set.TryRemoveAsync(item);

            // Assert
            AssertIsTrue(result);
            AssertIsFalse(await set.ContainsAsync(item));
        }

        [Fact(DisplayName = "TryRemoveNonExistingItemFails")]
        public virtual async Task TryRemoveNonExistingItemFails()
        {
            // Arrange
            var set = Create();
            var item = _fixture.Create<T>();

            // Act
            var result = await set.TryRemoveAsync(item);

            // Assert
            AssertIsFalse(result);
        }

        [Fact(DisplayName = "EnumerateExistingItemsSucceeds")]
        public virtual async Task EnumerateExistingItemsSucceeds()
        {
            // Arrange
            var set = Create();
            var item1 = _fixture.Create<T>();
            var item2 = _fixture.Create<T>();
            var item3 = _fixture.Create<T>();
            await set.AddAsync(item1);
            await set.AddAsync(item2);
            await set.AddAsync(item3);

            // Act
            var result = await set.AsEnumerableAsync();

            // Assert
            AssertEquals(await result.CountAsync(), 3);
            AssertIsTrue(await result.ContainsAsync(item1));
            AssertIsTrue(await result.ContainsAsync(item2));
            AssertIsTrue(await result.ContainsAsync(item3));
        }

        [Fact(DisplayName = "EnumerateAfterRemovingItemsSucceeds")]
        public virtual async Task EnumerateAfterRemovingItemsSucceeds()
        {
            // Arrange
            var set = Create();
            var item1 = _fixture.Create<T>();
            var item2 = _fixture.Create<T>();
            var item3 = _fixture.Create<T>();
            await set.AddAsync(item1);
            await set.AddAsync(item2);
            await set.AddAsync(item3);

            // Act
            var result = await set.AsEnumerableAsync();
            await set.TryRemoveAsync(item1);
            await set.TryRemoveAsync(item2);
            await set.TryRemoveAsync(item3);

            // Assert
            AssertEquals(await result.CountAsync(), 0);
            AssertIsFalse(await result.ContainsAsync(item1));
            AssertIsFalse(await result.ContainsAsync(item2));
            AssertIsFalse(await result.ContainsAsync(item3));
        }

        [Fact(DisplayName = "CheckForExistingItemSucceeds")]
        public virtual async Task CheckForExistingItemSucceeds()
        {
            // Arrange
            var set = Create();
            var item1 = _fixture.Create<T>();
            await set.AddAsync(item1);

            // Act
            var result = await set.ContainsAsync(item1);

            // Assert
            AssertIsTrue(result);
        }

        [Fact(DisplayName = "CheckForNonExistingItemFails")]
        public virtual async Task CheckForNonExistingItemFails()
        {
            // Arrange
            var set = Create();
            var item1 = _fixture.Create<T>();
            var item2 = _fixture.Create<T>();
            await set.AddAsync(item1);

            // Act
            var result = await set.ContainsAsync(item2);

            // Assert
            AssertIsFalse(result);
        }
    }
}
