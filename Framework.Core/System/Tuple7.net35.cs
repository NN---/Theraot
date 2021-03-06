#if NET20 || NET30 || NET35

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System
{
    [Serializable]
    public class Tuple<T1, T2, T3, T4, T5, T6, T7> : IStructuralEquatable, IStructuralComparable, IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }

        public T1 Item1 { get; }

        public T2 Item2 { get; }

        public T3 Item3 { get; }

        public T4 Item4 { get; }

        public T5 Item5 { get; }

        public T6 Item6 { get; }

        public T7 Item7 { get; }

        int IStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            return CompareTo(other, comparer);
        }

        int IComparable.CompareTo(object obj)
        {
            return CompareTo(obj, Comparer<object>.Default);
        }

        public override bool Equals(object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<object>.Default);
        }

        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            if (!(other is Tuple<T1, T2, T3, T4, T5, T6, T7> tuple))
            {
                return false;
            }
            return
                comparer.Equals(Item1, tuple.Item1) &&
                comparer.Equals(Item2, tuple.Item2) &&
                comparer.Equals(Item3, tuple.Item3) &&
                comparer.Equals(Item4, tuple.Item4) &&
                comparer.Equals(Item5, tuple.Item5) &&
                comparer.Equals(Item6, tuple.Item6) &&
                comparer.Equals(Item7, tuple.Item7);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<object>.Default);
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            var hash = comparer.GetHashCode(Item1);
            hash = (hash << 5) - hash + comparer.GetHashCode(Item2);
            hash = (hash << 5) - hash + comparer.GetHashCode(Item3);
            hash = (hash << 5) - hash + comparer.GetHashCode(Item4);
            hash = (hash << 5) - hash + comparer.GetHashCode(Item5);
            hash = (hash << 5) - hash + comparer.GetHashCode(Item6);
            hash = (hash << 5) - hash + comparer.GetHashCode(Item7);
            return hash;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3}, {4}, {5}, {6})", Item1, Item2, Item3, Item4, Item5, Item6, Item7);
        }

        private int CompareTo(object other, IComparer comparer)
        {
            if (other == null)
            {
                return 1;
            }
            if (!(other is Tuple<T1, T2, T3, T4, T5, T6, T7> tuple))
            {
                throw new ArgumentException(nameof(other));
            }
            var result = comparer.Compare(Item1, tuple.Item1);
            if (result == 0)
            {
                result = comparer.Compare(Item2, tuple.Item2);
            }
            if (result == 0)
            {
                result = comparer.Compare(Item3, tuple.Item3);
            }
            if (result == 0)
            {
                result = comparer.Compare(Item4, tuple.Item4);
            }
            if (result == 0)
            {
                result = comparer.Compare(Item5, tuple.Item5);
            }
            if (result == 0)
            {
                result = comparer.Compare(Item6, tuple.Item6);
            }
            if (result == 0)
            {
                result = comparer.Compare(Item7, tuple.Item7);
            }
            return result;
        }
    }
}

#endif