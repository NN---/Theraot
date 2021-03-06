﻿#if NET20 || NET30 || NET35

using System.Collections.Generic;
using System.Threading;
using Theraot.Collections;
using Theraot.Collections.Specialized;
using Theraot.Core;

namespace System.Collections.Concurrent
{
    public static class Partitioner
    {
        // How many chunks do we want to divide the range into?  If this is 1, then the
        // answer is "one chunk per core".  Generally, though, you'll achieve better
        // load balancing on a busy system if you make it higher than 1.
        const int _coreOversubscriptionRate = 3;

        public static OrderablePartitioner<TSource> Create<TSource>(IList<TSource> list, bool loadBalance)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (loadBalance)
            {
                return new DynamicOrderablePartitioner<TSource>(list);
            }
            return new StaticOrderablePartitioner<TSource>(list);
        }

        public static OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (loadBalance)
            {
                return new DynamicOrderablePartitioner<TSource>(array);
            }
            return new StaticOrderablePartitioner<TSource>(array);
        }

        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source)
        {
            return Create(source, EnumerablePartitionerOptions.None);
        }

        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if ((partitionerOptions & ~EnumerablePartitionerOptions.NoBuffering) != 0)
            {
                // We could support NoBuffering, no problem.
                // In fact, what is that about chunking? We do nothing of that.
                // I bet I could just use the current implementation as is...
                // ... it was created to partition on demand, no buffering.
                // However this is the documented behaviour.
                // Which also means there are no examples for a dynamic no buffering partition.
                throw new ArgumentOutOfRangeException(nameof(partitionerOptions));
            }
            return new DynamicOrderablePartitioner<TSource>(source);
        }

        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive)
        {
            if (toExclusive <= fromInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(toExclusive));
            }
            var rangeCount = EnvironmentHelper.ProcessorCount * _coreOversubscriptionRate;
            var size = toExclusive - fromInclusive;
            var rangeSize = size / rangeCount;
            if (rangeSize * rangeCount < size)
            {
                rangeSize++;
            }
            return new StaticOrderablePartitioner<Tuple<long, long>>(Range(fromInclusive, toExclusive, rangeSize));
        }

        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize)
        {
            if (toExclusive <= fromInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(toExclusive));
            }
            if (rangeSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeSize));
            }
            return new StaticOrderablePartitioner<Tuple<long, long>>(Range(fromInclusive, toExclusive, rangeSize));
        }

        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive)
        {
            if (toExclusive <= fromInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(toExclusive));
            }
            var rangeCount = EnvironmentHelper.ProcessorCount * _coreOversubscriptionRate;
            var size = toExclusive - fromInclusive;
            var rangeSize = size / rangeCount;
            if (rangeSize * rangeCount < size)
            {
                rangeSize++;
            }
            return new StaticOrderablePartitioner<Tuple<int, int>>(Range(fromInclusive, toExclusive, rangeSize));
        }

        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize)
        {
            if (toExclusive <= fromInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(toExclusive));
            }
            if (rangeSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeSize));
            }
            return new StaticOrderablePartitioner<Tuple<int, int>>(Range(fromInclusive, toExclusive, rangeSize));
        }

        private static IEnumerable<Tuple<long, long>> Range(long fromInclusive, long toExclusive, long rangeSize)
        {
            for (var index = fromInclusive; index < toExclusive; index += rangeSize)
            {
                var top = index + rangeSize;
                if (top > toExclusive)
                {
                    top = toExclusive;
                }
                top++;
                yield return new Tuple<long, long>(index, top);
            }
        }

        private static IEnumerable<Tuple<int, int>> Range(int fromInclusive, int toExclusive, int rangeSize)
        {
            for (var index = fromInclusive; index < toExclusive; index += rangeSize)
            {
                var top = index + rangeSize;
                if (top > toExclusive)
                {
                    top = toExclusive;
                }
                top++;
                yield return new Tuple<int, int>(index, top);
            }
        }
    }

    public abstract class Partitioner<TSource>
    {
        public virtual bool SupportsDynamicPartitions => true;

        public virtual IEnumerable<TSource> GetDynamicPartitions()
        {
            throw new NotSupportedException();
        }

        public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);
    }

    internal sealed class DynamicOrderablePartitioner<T> : OrderablePartitioner<T>
    {
        private readonly IEnumerable<T> _source;

        public DynamicOrderablePartitioner(IEnumerable<T> source)
            :base(true, false, true)
        {
            _source = source;
        }

        public override bool SupportsDynamicPartitions => true;

        public override IEnumerable<T> GetDynamicPartitions()
        {
            if (_source is T[] array)
            {
                return Progressor<T>.CreateFromArray(array);
            }
            if (_source is IList<T> list)
            {
                return Progressor<T>.CreateFromIList(list);
            }
            return Progressor<T>.CreateFromIEnumerable(_source);
        }

        public override IEnumerable<KeyValuePair<long, T>> GetOrderableDynamicPartitions()
        {
            var source = GetDynamicPartitions();
            var subIndex = 0L;
            foreach (var item in source)
            {
                yield return new KeyValuePair<long, T>(subIndex, item);
                subIndex++;
            }
        }

        public override IList<IEnumerator<KeyValuePair<long, T>>> GetOrderablePartitions(int partitionCount)
        {
            var list = new List<IEnumerator<KeyValuePair<long, T>>>(partitionCount);
            var source = GetDynamicPartitions();
            for (int index = 0; index < partitionCount; index++)
            {
                list.Add(Enumerator(index));
            }
            return list.AsReadOnly();
            IEnumerator<KeyValuePair<long, T>> Enumerator(int index)
            {
                int subIndex = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                {
                    yield return new KeyValuePair<long, T>(NumericHelper.BuildInt64(index, subIndex), item);
                    subIndex++;
                }
            }
        }

        public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
        {
            var list = new List<IEnumerator<T>>(partitionCount);
            var source = GetDynamicPartitions();
            for (int index = 0; index < partitionCount; index++)
            {
                list.Add(Enumerator());
            }
            return list.AsReadOnly();
            IEnumerator<T> Enumerator()
            {
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                {
                    yield return item;
                }
            }
        }
    }

    internal sealed class StaticOrderablePartitioner<T> : OrderablePartitioner<T>
    {
        private readonly IList<T> _source;

        public StaticOrderablePartitioner(IEnumerable<T> source)
            : base(true, false, true)
        {
            _source = Extensions.AsIList(source);
        }

        public override IEnumerable<T> GetDynamicPartitions()
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<KeyValuePair<long, T>> GetOrderableDynamicPartitions()
        {
            throw new NotSupportedException();
        }

        public override IList<IEnumerator<KeyValuePair<long, T>>> GetOrderablePartitions(int partitionCount)
        {
            var list = new List<IEnumerator<KeyValuePair<long, T>>>(partitionCount);
            var source = GetDynamicPartitions();
            for (int index = 0; index < partitionCount; index++)
            {
                list.Add(Enumerator(index));
            }
            return list.AsReadOnly();
            IEnumerator<KeyValuePair<long, T>> Enumerator(int index)
            {
                int subIndex = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                {
                    yield return new KeyValuePair<long, T>(NumericHelper.BuildInt64(index, subIndex), item);
                    subIndex++;
                }
            }
        }

        public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
        {
            var index = -1;
            var groups = GroupBuilder<int, T, T>.CreateGroups
            (
                _source,
                EqualityComparer<int>.Default,
                arg => Interlocked.Increment(ref index) % partitionCount,
                obj => obj
            );
            return Extensions.WrapAsIList(groups.ConvertProgressive(g => g.GetEnumerator()));
        }
    }
}

#endif