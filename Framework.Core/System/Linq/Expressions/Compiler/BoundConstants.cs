#if NET20 || NET30

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Theraot.Collections;
using Theraot.Core;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions.Compiler
{
    /// <summary>
    /// This type tracks "runtime" constants--live objects that appear in
    /// ConstantExpression nodes and must be bound to the delegate.
    /// </summary>
    internal sealed class BoundConstants
    {
        /// <summary>
        /// IL locals for storing frequently used constants
        /// </summary>
        private readonly Dictionary<TypedConstant, LocalBuilder> _cache = new Dictionary<TypedConstant, LocalBuilder>();

        /// <summary>
        /// The index of each constant in the constant array
        /// </summary>
        private readonly Dictionary<object, int> _indexes = new Dictionary<object, int>(ReferenceEqualityComparer<object>.Instance);

        /// <summary>
        /// Each constant referenced within this lambda, and how often it was referenced
        /// </summary>
        private readonly Dictionary<TypedConstant, int> _references = new Dictionary<TypedConstant, int>();

        /// <summary>
        /// The list of constants in the order they appear in the constant array
        /// </summary>
        private readonly List<object> _values = new List<object>();

        internal int Count => _values.Count;

        /// <summary>
        /// Called by VariableBinder. Adds the constant to the list (if needed)
        /// and increases the reference count by one
        /// </summary>
        internal void AddReference(object value, Type type)
        {
            if (_indexes.TryAdd(value, _values.Count))
            {
                _values.Add(value);
            }
            Helpers.IncrementCount(new TypedConstant(value, type), _references);
        }

        /// <summary>
        /// Emit code to cache frequently used constants into IL locals,
        /// instead of pulling them out of the array each time
        /// </summary>
        internal void EmitCacheConstants(LambdaCompiler lc)
        {
            int count = 0;
            foreach (KeyValuePair<TypedConstant, int> reference in _references)
            {
                if (!lc.CanEmitBoundConstants)
                {
                    throw Error.CannotCompileConstant(reference.Key.Value);
                }
                if (ShouldCache(reference.Value))
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return;
            }
            EmitConstantsArray(lc);

            // The same lambda can be in multiple places in the tree, so we
            // need to clear any locals from last time.
            _cache.Clear();

            foreach (KeyValuePair<TypedConstant, int> reference in _references)
            {
                if (ShouldCache(reference.Value))
                {
                    if (--count > 0)
                    {
                        // Dup array to keep it on the stack
                        lc.IL.Emit(OpCodes.Dup);
                    }
                    LocalBuilder local = lc.IL.DeclareLocal(reference.Key.Type);
                    EmitConstantFromArray(lc, reference.Key.Value, local.LocalType);
                    lc.IL.Emit(OpCodes.Stloc, local);
                    _cache.Add(reference.Key, local);
                }
            }
        }

        /// <summary>
        /// Emits a live object as a constant
        /// </summary>
        internal void EmitConstant(LambdaCompiler lc, object value, Type type)
        {
            Debug.Assert(!ILGen.CanEmitConstant(value, type));

            if (!lc.CanEmitBoundConstants)
            {
                throw Error.CannotCompileConstant(value);
            }

            if (_cache.TryGetValue(new TypedConstant(value, type), out var local))
            {
                lc.IL.Emit(OpCodes.Ldloc, local);
                return;
            }
            EmitConstantsArray(lc);
            EmitConstantFromArray(lc, value, type);
        }

        internal object[] ToArray()
        {
            return _values.ToArray();
        }

        private static void EmitConstantsArray(LambdaCompiler lc)
        {
            Debug.Assert(lc.CanEmitBoundConstants); // this should've been checked already

            lc.EmitClosureArgument();
            lc.IL.Emit(OpCodes.Ldfld, ClosureConstants);
        }

        private static bool ShouldCache(int refCount)
        {
            // This caching is too aggressive in the face of conditionals and
            // switch. Also, it is too conservative for variables used inside
            // of loops.
            return refCount > 2;
        }

        private void EmitConstantFromArray(LambdaCompiler lc, object value, Type type)
        {
            if (!_indexes.TryGetValue(value, out var index))
            {
                _indexes.Add(value, index = _values.Count);
                _values.Add(value);
            }

            lc.IL.EmitPrimitive(index);
            lc.IL.Emit(OpCodes.Ldelem_Ref);
            if (type.IsValueType)
            {
                lc.IL.Emit(OpCodes.Unbox_Any, type);
            }
            else if (type != typeof(object))
            {
                lc.IL.Emit(OpCodes.Castclass, type);
            }
        }

        /// <summary>
        /// Constants can emit themselves as different types
        /// For caching purposes, we need to treat each distinct Type as a
        /// separate thing to cache. (If we have to cast it on the way out, it
        /// ends up using a JIT temp and defeats the purpose of caching the
        /// value in a local)
        /// </summary>
        private /*readonly*/ struct TypedConstant : IEquatable<TypedConstant>
        {
            internal readonly Type Type;
            internal readonly object Value;

            internal TypedConstant(object value, Type type)
            {
                Value = value;
                Type = type;
            }

            public bool Equals(TypedConstant other)
            {
                return ReferenceEquals(Value, other.Value) && Type == other.Type;
            }

            [SuppressMessage("Microsoft.Usage", "CA2231:OverloadOperatorEqualsOnOverridingValueTypeEquals")]
            public override bool Equals(object obj)
            {
                return obj is TypedConstant constant && Equals(constant);
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(Value) ^ Type.GetHashCode();
            }
        }
    }
}

#endif