// Needed for NET35 (ThreadLocal)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Theraot.Threading.Needles
{
    [DebuggerNonUserCode]
    public struct ReadOnlyStructNeedle<T> : INeedle<T>, IEquatable<ReadOnlyStructNeedle<T>>
    {
        public ReadOnlyStructNeedle(T target)
        {
            Value = target;
        }

        public bool IsAlive => Value != null;

        T INeedle<T>.Value
        {
            get => Value;

            set => throw new NotSupportedException();
        }

        public T Value { get; }

        public static explicit operator T(ReadOnlyStructNeedle<T> needle)
        {
            return needle.Value;
        }

        public static implicit operator ReadOnlyStructNeedle<T>(T field)
        {
            return new ReadOnlyStructNeedle<T>(field);
        }

        public static bool operator !=(ReadOnlyStructNeedle<T> left, ReadOnlyStructNeedle<T> right)
        {
            var leftValue = left.Value;
            if (left.IsAlive)
            {
                var rightValue = right.Value;
                return !right.IsAlive || !EqualityComparer<T>.Default.Equals(leftValue, rightValue);
            }
            return right.IsAlive;
        }

        public static bool operator ==(ReadOnlyStructNeedle<T> left, ReadOnlyStructNeedle<T> right)
        {
            var leftValue = left.Value;
            if (left.IsAlive)
            {
                var rightValue = right.Value;
                return right.IsAlive && EqualityComparer<T>.Default.Equals(leftValue, rightValue);
            }
            return !right.IsAlive;
        }

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyStructNeedle<T> right)
            {
                if (!right.IsAlive)
                {
                    return !IsAlive;
                }
                obj = right.Value;
            }
            if (obj is T rightValue)
            {
                var value = Value;
                return IsAlive && EqualityComparer<T>.Default.Equals(value, rightValue);
            }
            return false;
        }

        public bool Equals(ReadOnlyStructNeedle<T> other)
        {
            var leftValue = Value;
            if (IsAlive)
            {
                var rightValue = other.Value;
                return other.IsAlive && EqualityComparer<T>.Default.Equals(leftValue, rightValue);
            }
            return !other.IsAlive;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            if (IsAlive)
            {
                return Value.ToString();
            }
            return "<Dead Needle>";
        }
    }
}