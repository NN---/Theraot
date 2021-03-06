﻿#if NET20 || NET30 || NET35

using System.Collections.Generic;
using Theraot.Collections.ThreadSafe;

namespace System.Collections
{
    public static class StructuralComparisons
    {
        private static readonly InternalComparer _comparer = new InternalComparer();

        public static IComparer StructuralComparer => _comparer;

        public static IEqualityComparer StructuralEqualityComparer => _comparer;

        private sealed class InternalComparer : IComparer, IEqualityComparer
        {
            int IComparer.Compare(object x, object y)
            {
                if (x is IStructuralComparable comparable)
                {
                    return comparable.CompareTo(y, this);
                }
                return Comparer.Default.Compare(x, y);
            }

            bool IEqualityComparer.Equals(object x, object y)
            {
                if (NullComparison(x, y, out var result))
                {
                    return result;
                }
                if (x is IStructuralEquatable comparable)
                {
                    return comparable.Equals(y, this);
                }
                var typeX = x.GetType();
                var typeY = y.GetType();
                if (typeX.IsArray && typeY.IsArray)
                {
                    if (typeX.GetElementType() == typeY.GetElementType())
                    {
                        CheckRank(x, y, typeX, typeY);
                        var xLengthInfo = typeX.GetProperty("Length");
                        var yLengthInfo = typeY.GetProperty("Length");
                        if (xLengthInfo == null || yLengthInfo == null)
                        {
                            // should never happen
                            throw new ArgumentException("Valid arrays required");
                        }
                        if ((int)xLengthInfo.GetValue(x, ArrayReservoir<object>.EmptyArray) != (int)yLengthInfo.GetValue(y, ArrayReservoir<object>.EmptyArray))
                        {
                            return false;
                        }
                        var xEnumeratorInfo = typeX.GetMethod("GetEnumerator");
                        var yEnumeratorInfo = typeX.GetMethod("GetEnumerator");
                        IEnumerator firstEnumerator = null;
                        IEnumerator secondEnumerator = null;
                        var comparer = this as IEqualityComparer;
                        try
                        {
                            // If there comes the day when an array has no enumerator, let this code fail
                            // ReSharper disable once PossibleNullReferenceException
                            firstEnumerator = (IEnumerator)xEnumeratorInfo.Invoke(x, ArrayReservoir<object>.EmptyArray);
                            // ReSharper disable once PossibleNullReferenceException
                            secondEnumerator = (IEnumerator)yEnumeratorInfo.Invoke(y, ArrayReservoir<object>.EmptyArray);
                            while (firstEnumerator.MoveNext())
                            {
                                if (!secondEnumerator.MoveNext())
                                {
                                    return false;
                                }
                                if (!comparer.Equals(firstEnumerator.Current, secondEnumerator.Current))
                                {
                                    return false;
                                }
                            }
                            return !secondEnumerator.MoveNext();
                        }
                        finally
                        {
                            if (firstEnumerator is IDisposable disposableX)
                            {
                                disposableX.Dispose();
                            }
                            if (secondEnumerator is IDisposable disposableY)
                            {
                                disposableY.Dispose();
                            }
                        }
                    }
                    return false;
                }
                return EqualityComparer<object>.Default.Equals(x, y);
            }

            int IEqualityComparer.GetHashCode(object obj)
            {
                if (obj is IStructuralEquatable comparer)
                {
                    return comparer.GetHashCode(this);
                }
                return EqualityComparer<object>.Default.GetHashCode(obj);
            }

            private static void CheckRank(object x, object y, Type typeX, Type typeY)
            {
                var xRankInfo = typeX.GetProperty("Rank");
                var yRankInfo = typeY.GetProperty("Rank");
                if (xRankInfo == null || yRankInfo == null)
                {
                    // should never happen
                    throw new ArgumentException("Valid arrays required");
                }
                if ((int)xRankInfo.GetValue(x, ArrayReservoir<object>.EmptyArray) != 1)
                {
                    throw new ArgumentException("Only one-dimensional arrays are supported", nameof(x));
                }
                if ((int)yRankInfo.GetValue(y, ArrayReservoir<object>.EmptyArray) != 1)
                {
                    throw new ArgumentException("Only one-dimensional arrays are supported", nameof(y));
                }
            }

            private static bool NullComparison(object x, object y, out bool result)
            {
                var xNull = x == null;
                var yNull = y == null;
                result = xNull == yNull;
                return xNull || yNull;
            }
        }
    }
}

#endif