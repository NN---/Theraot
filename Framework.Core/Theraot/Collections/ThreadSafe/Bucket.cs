using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Theraot.Collections.ThreadSafe
{
    /// <summary>
    /// Represent a thread-safe wait-free bucket.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
#if NET20 || NET30 || NET35 || NET40 || NET45 || NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2
    [Serializable]
#endif

    public sealed class Bucket<T> : IBucket<T>
    {
        private readonly BucketCore _bucketCore;
        private int _count;

        public Bucket()
        {
            _bucketCore = new BucketCore();
        }

        public int Count => _count;

        public void CopyTo(T[] array, int arrayIndex)
        {
            Extensions.CanCopyTo(Count, array, arrayIndex);
            Extensions.CopyTo(this, array, arrayIndex);
        }

#if FAT
        public IEnumerable<T> EnumerateRange(int indexFrom, int indexTo)
        {
            foreach (var value in _bucketCore.EnumerateRange(indexFrom, indexTo))
            {
                yield return value == BucketHelper.Null ? default : (T)value;
            }
        }
#endif

        public bool Exchange(int index, T item, out T previous)
        {
            var found = BucketHelper.Null;
            previous = default;
            var result = _bucketCore.DoMayIncrement
                (
                    index,
                    (ref object target) =>
                    {
                        found = Interlocked.Exchange(ref target, (object)item ?? BucketHelper.Null);
                        return found == null;
                    }
                );
            if (result)
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            if (found != BucketHelper.Null)
            {
                previous = (T)found;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var value in _bucketCore)
            {
                yield return value == BucketHelper.Null ? default : (T)value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Insert(int index, T item)
        {
            var result = _bucketCore.DoMayIncrement
                (
                    index,
                    (ref object target) =>
                    {
                        var found = Interlocked.CompareExchange(ref target, (object)item ?? BucketHelper.Null, null);
                        return found == null;
                    }
                );
            if (result)
            {
                Interlocked.Increment(ref _count);
            }
            return result;
        }

        public bool Insert(int index, T item, out T previous)
        {
            var found = BucketHelper.Null;
            previous = default;
            var result = _bucketCore.DoMayIncrement
                (
                    index,
                    (ref object target) =>
                    {
                        found = Interlocked.CompareExchange(ref target, (object)item ?? BucketHelper.Null, null);
                        return found == null;
                    }
                );
            if (result)
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            if (found != BucketHelper.Null)
            {
                previous = (T)found;
            }
            return false;
        }

        public bool RemoveAt(int index)
        {
            var result = _bucketCore.DoMayDecrement
                (
                    index,
                    (ref object target) => Interlocked.Exchange(ref target, null) != null
                );
            if (result)
            {
                Interlocked.Decrement(ref _count);
            }
            return result;
        }

        public bool RemoveAt(int index, out T previous)
        {
            var found = BucketHelper.Null;
            previous = default;
            var result = _bucketCore.DoMayDecrement
                (
                    index,
                    (ref object target) =>
                    {
                        found = Interlocked.Exchange(ref target, null);
                        return found != null;
                    }
                );
            if (!result)
            {
                return false;
            }
            Interlocked.Decrement(ref _count);
            if (found != BucketHelper.Null)
            {
                previous = (T)found;
            }
            return true;
        }

        public bool RemoveAt(int index, Predicate<T> check)
        {
            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }
            return _bucketCore.DoMayDecrement
                (
                    index,
                    (ref object target) =>
                    {
                        var found = Interlocked.CompareExchange(ref target, null, null);
                        if (found != null)
                        {
                            var comparisonItem = found == BucketHelper.Null ? default : (T)found;
                            if (check(comparisonItem))
                            {
                                var compare = Interlocked.CompareExchange(ref target, null, found);
                                if (found == compare)
                                {
                                    Interlocked.Decrement(ref _count);
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                );
        }

        public void Set(int index, T item, out bool isNew)
        {
            isNew = _bucketCore.DoMayIncrement
                (
                    index,
                    (ref object target) => Interlocked.Exchange(ref target, (object)item ?? BucketHelper.Null) == null
                );
            if (isNew)
            {
                Interlocked.Increment(ref _count);
            }
        }

        public bool TryGet(int index, out T value)
        {
            var found = BucketHelper.Null;
            value = default;
            var done = _bucketCore.Do
                (
                    index,
                    (ref object target) =>
                    {
                        found = Interlocked.CompareExchange(ref target, null, null);
                        return true;
                    }
                );
            if (!done || found == null)
            {
                return false;
            }
            if (found != BucketHelper.Null)
            {
                value = (T)found;
            }
            return true;
        }

        public bool Update(int index, Func<T, T> itemUpdateFactory, Predicate<T> check, out bool isEmpty)
        {
            if (itemUpdateFactory == null)
            {
                throw new ArgumentNullException(nameof(itemUpdateFactory));
            }
            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }
            var found = BucketHelper.Null;
            var compare = BucketHelper.Null;
            var result = false;
            var done = _bucketCore.Do
                (
                    index,
                    (ref object target) =>
                    {
                        found = Interlocked.CompareExchange(ref target, null, null);
                        if (found != null)
                        {
                            var comparisonItem = found == BucketHelper.Null ? default : (T)found;
                            if (check(comparisonItem))
                            {
                                var item = itemUpdateFactory(comparisonItem);
                                compare = Interlocked.CompareExchange(ref target, (object)item ?? BucketHelper.Null, found);
                                result = found == compare;
                            }
                        }
                        return true;
                    }
                );
            if (!done)
            {
                isEmpty = true;
                return false;
            }
            isEmpty = found == null || compare == null;
            return result;
        }

        public IEnumerable<T> Where(Predicate<T> check)
        {
            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }
            return WhereExtracted();
            IEnumerable<T> WhereExtracted()
            {
                foreach (var value in _bucketCore)
                {
                    var castValue = value == BucketHelper.Null ? default : (T)value;
                    if (check(castValue))
                    {
                        yield return castValue;
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<int, T>> WhereIndexed(Predicate<T> check)
        {
            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }
            return WhereExtracted();
            IEnumerable<KeyValuePair<int, T>> WhereExtracted()
            {
                var index = 0;
                foreach (var value in _bucketCore)
                {
                    var castValue = value == BucketHelper.Null ? default : (T)value;
                    if (check(castValue))
                    {
                        yield return new KeyValuePair<int, T>(index, castValue);
                        index++;
                    }
                }
            }
        }
    }
}