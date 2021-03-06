﻿#if NET20 || NET30

using System.Collections;
using System.Collections.Generic;
using Theraot.Collections.Specialized;
using Theraot.Collections.ThreadSafe;
using Theraot.Reflection;

namespace System.Linq
{
    public class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        private readonly IDictionary<TKey, Grouping<TKey, TElement>> _groupings;

        internal Lookup(IEqualityComparer<TKey> comparer)
        {
            if (typeof(TKey).CanBeNull())
            {
                _groupings = new NullAwareDictionary<TKey, Grouping<TKey, TElement>>(comparer);
            }
            else
            {
                _groupings = new Dictionary<TKey, Grouping<TKey, TElement>>(comparer);
            }
        }

        public int Count => _groupings.Count;

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                if (_groupings.TryGetValue(key, out Grouping<TKey, TElement> grouping))
                {
                    return grouping;
                }
                return ArrayReservoir<TElement>.EmptyArray;
            }
        }

        public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            // MICROSFT does not null check resultSelector
            foreach (var group in _groupings.Values)
            {
                yield return resultSelector(group.Key, group);
            }
        }

        public bool Contains(TKey key)
        {
            return _groupings.ContainsKey(key);
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            foreach (var grouping in _groupings.Values)
            {
                yield return grouping;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (elementSelector == null)
            {
                throw new ArgumentNullException(nameof(elementSelector));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            var result = new Lookup<TKey, TElement>(comparer);
            var collections = new NullAwareDictionary<TKey, List<TElement>>(comparer);
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!collections.TryGetValue(key, out var collection))
                {
                    collection = new List<TElement>();
                    collections.Add(key, collection);
                }
                if (!result._groupings.TryGetValue(key, out var grouping))
                {
                    grouping = new Grouping<TKey, TElement>(key, collection);
                    result._groupings.Add(key, grouping);
                }
                collection.Add(elementSelector(item));
            }
            return result;
        }
    }
}

#endif