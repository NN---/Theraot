#if NET20 || NET30 || NET35 || NET40

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using Theraot.Reflection;

namespace System.Dynamic.Utils
{
    internal static class TypeUtils
    {
        private static readonly Type[] _arrayAssignableInterfaces = typeof(int[]).GetTypeInfo().GetInterfaces()
         .Where(i => i.GetTypeInfo().IsGenericType)
         .Select(i => i.GetGenericTypeDefinition())
         .ToArray();

        internal static bool AreEquivalent(Type t1, Type t2) => t1 != null && t1 == t2;

        internal static Type FindGenericType(Type definition, Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsConstructedGenericType() && type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }
                var definitionInfo = definition.GetTypeInfo();
                var info = type.GetTypeInfo();
                if (definitionInfo.IsInterface)
                {
                    foreach (var interfaceType in info.GetInterfaces())
                    {
                        var found = FindGenericType(definition, interfaceType);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
                type = info.BaseType;
            }
            return null;
        }

        internal static MethodInfo GetBooleanOperator(Type type, string name)
        {
            do
            {
                var result = type.GetStaticMethodInternal(name, new[] { type });
                if (result != null && result.IsSpecialName && !result.ContainsGenericParameters)
                {
                    return result;
                }
                var info = type.GetTypeInfo();
                type = info.BaseType;
            } while (type != null);
            return null;
        }

        internal static MethodInfo GetStaticMethod(this Type type, string name, Type[] types)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            // Don't use BindingFlags.Static
            foreach (var method in type.GetTypeInfo().GetMethods())
            {
                if (method.Name == name && method.IsStatic && method.MatchesArgumentTypes(types))
                {
                    return method;
                }
            }
            return null;
        }

        internal static MethodInfo GetStaticMethodInternal(this Type type, string name, Type[] types)
        {
            // Don't use BindingFlags.Static
            foreach (var method in type.GetTypeInfo().GetMethods())
            {
                if (method.Name == name && method.IsStatic && method.MatchesArgumentTypes(types))
                {
                    return method;
                }
            }
            return null;
        }

        internal static bool HasBuiltInEqualityOperator(Type left, Type right)
        {
            var leftInfo = left.GetTypeInfo();
            var rightInfo = right.GetTypeInfo();
            if (leftInfo.IsInterface && !rightInfo.IsValueType)
            {
                return true;
            }
            if (rightInfo.IsInterface && !leftInfo.IsValueType)
            {
                return true;
            }
            if (!leftInfo.IsValueType && !rightInfo.IsValueType)
            {
                if (left.IsReferenceAssignableFromInternal(right) || right.IsReferenceAssignableFromInternal(left))
                {
                    return true;
                }
            }
            if (left != right)
            {
                return false;
            }
            var notNullable = left.GetNonNullable();
            var info = notNullable.GetTypeInfo();
            if (notNullable == typeof(bool) || notNullable.IsNumeric() || info.IsEnum)
            {
                return true;
            }
            return false;
        }

        internal static bool HasReferenceConversionTo(this Type source, Type target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            // void -> void conversion is handled elsewhere
            // (it's an identity conversion)
            // All other void conversions are disallowed.
            if (source == typeof(void) || target == typeof(void))
            {
                return false;
            }
            var nonNullableSource = source.GetNonNullable();
            var nonNullableTarget = target.GetNonNullable();
            // Down conversion
            if (nonNullableSource.IsAssignableFrom(nonNullableTarget))
            {
                return true;
            }
            // Up conversion
            if (nonNullableTarget.IsAssignableFrom(nonNullableSource))
            {
                return true;
            }
            // Interface conversion
            var sourceInfo = source.GetTypeInfo();
            var targetInfo = target.GetTypeInfo();
            if (sourceInfo.IsInterface || targetInfo.IsInterface)
            {
                return true;
            }
            // Variant delegate conversion
            if (IsLegalExplicitVariantDelegateConversion(source, target))
            {
                return true;
            }
            // Object conversion handled by assignable above.
            return (source.IsArray || target.IsArray) && StrictHasReferenceConversionTo(source, target, true);
        }

        internal static bool HasReferenceConversionToInternal(this Type source, Type target)
        {
            // void -> void conversion is handled elsewhere
            // (it's an identity conversion)
            // All other void conversions are disallowed.
            if (source == typeof(void) || target == typeof(void))
            {
                return false;
            }
            var nonNullableSource = source.GetNonNullable();
            var nonNullableTarget = target.GetNonNullable();
            // Down conversion
            if (nonNullableSource.IsAssignableFrom(nonNullableTarget))
            {
                return true;
            }
            // Up conversion
            if (nonNullableTarget.IsAssignableFrom(nonNullableSource))
            {
                return true;
            }
            // Interface conversion
            var sourceInfo = source.GetTypeInfo();
            var targetInfo = target.GetTypeInfo();
            if (sourceInfo.IsInterface || targetInfo.IsInterface)
            {
                return true;
            }
            // Variant delegate conversion
            if (IsLegalExplicitVariantDelegateConversion(source, target))
            {
                return true;
            }
            // Object conversion handled by assignable above.
            return (source.IsArray || target.IsArray) && StrictHasReferenceConversionToInternal(source, target, true);
        }

        internal static bool HasReferenceEquality(Type left, Type right)
        {
            var leftInfo = left.GetTypeInfo();
            var rightInfo = right.GetTypeInfo();
            if (leftInfo.IsValueType || rightInfo.IsValueType)
            {
                return false;
            }
            // If we have an interface and a reference type then we can do
            // reference equality.
            // If we have two reference types and one is assignable to the
            // other then we can do reference equality.
            return leftInfo.IsInterface
                   || rightInfo.IsInterface
                   || left.IsReferenceAssignableFromInternal(right)
                   || right.IsReferenceAssignableFromInternal(left);
        }

        internal static bool IsArrayTypeAssignableTo(Type type, Type target)
        {
            if (!type.IsArray || !target.IsArray)
            {
                return false;
            }
            if (type.GetArrayRank() != target.GetArrayRank())
            {
                return false;
            }
            return type.GetElementType().IsAssignableToInternal(target.GetElementType());
        }

        internal static bool IsArrayTypeAssignableToInterface(Type type, Type target)
        {
            if (!type.IsArray)
            {
                return false;
            }
            return
                (
                    target.IsGenericInstanceOf(typeof(IList<>))
                    || target.IsGenericInstanceOf(typeof(ICollection<>))
                    || target.IsGenericInstanceOf(typeof(IEnumerable<>))
                )
                && type.GetElementType() == target.GetGenericArguments()[0];
        }

        internal static bool IsAssignableTo(this Type type, ParameterInfo parameterInfo)
        {
            return IsAssignableTo(type.GetNotNullable(), parameterInfo.GetNonRefType());
        }

        internal static bool IsAssignableTo(this Type type, Type target)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            return type.IsAssignableToInternal(target);
        }

        internal static bool IsAssignableToInternal(this Type type, Type target)
        {
            return target.IsAssignableFrom(type)
                   || IsArrayTypeAssignableTo(type, target)
                   || IsArrayTypeAssignableToInterface(type, target);
        }

        internal static bool IsFloatingPoint(this TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;

                default:
                    return false;
            }
        }

        internal static bool IsGenericImplementationOf(this Type type, Type interfaceGenericTypeDefinition)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (currentInterface.IsGenericInstanceOf(interfaceGenericTypeDefinition))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsGenericImplementationOf(this Type type, params Type[] interfaceGenericTypeDefinitions)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            foreach (var currentInterface in type.GetInterfaces())
            {
                var info = currentInterface.GetTypeInfo();
                if (info.IsGenericTypeDefinition)
                {
                    var match = currentInterface.GetGenericTypeDefinition();
                    if (Array.Exists(interfaceGenericTypeDefinitions, item => item == match))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsGenericImplementationOf(this Type type, out Type interfaceType, Type interfaceGenericTypeDefinition)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (currentInterface.IsGenericInstanceOf(interfaceGenericTypeDefinition))
                {
                    interfaceType = currentInterface;
                    return true;
                }
            }
            interfaceType = null;
            return false;
        }

        internal static bool IsGenericImplementationOf(this Type type, out Type interfaceType, params Type[] interfaceGenericTypeDefinitions)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var implementedInterfaces = type.GetInterfaces();
            foreach (var currentInterface in interfaceGenericTypeDefinitions)
            {
                var index = Array.FindIndex(implementedInterfaces, item => item.IsGenericInstanceOf(currentInterface));
                if (index != -1)
                {
                    interfaceType = implementedInterfaces[index];
                    return true;
                }
            }
            interfaceType = null;
            return false;
        }

        internal static bool IsImplementationOf(this Type type, Type interfaceType)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (currentInterface == interfaceType)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsImplementationOf(this Type type, params Type[] interfaceTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (Array.Exists(interfaceTypes, item => currentInterface == item))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsImplementationOf(this Type type, out Type interfaceType, params Type[] interfaceTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var implementedInterfaces = type.GetInterfaces();
            foreach (var currentInterface in interfaceTypes)
            {
                var index = Array.FindIndex(implementedInterfaces, item => item == currentInterface);
                if (index != -1)
                {
                    interfaceType = implementedInterfaces[index];
                    return true;
                }
            }
            interfaceType = null;
            return false;
        }

        internal static bool IsImplicitlyConvertibleToInternal(this Type source, Type target)
        {
            return source == target
                   || TypeHelper.IsImplicitNumericConversion(source, target)
                   || IsImplicitReferenceConversion(source, target)
                   || TypeHelper.IsImplicitBoxingConversion(source, target)
                   || IsImplicitNullableConversion(source, target);
        }

        internal static bool IsImplicitNullableConversion(Type source, Type target)
        {
            if (target.IsNullable())
            {
                return source.GetNonNullable().IsImplicitlyConvertibleToInternal(target.GetNonNullable());
            }
            return false;
        }

        internal static bool IsImplicitReferenceConversion(Type source, Type target)
        {
            return target.IsAssignableFrom(source);
        }

        internal static bool IsLegalExplicitVariantDelegateConversion(Type source, Type target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            // There *might* be a legal conversion from a generic delegate type S to generic delegate type  T,
            // provided all of the follow are true:
            //   o Both types are constructed generic types of the same generic delegate type, D<X1,... Xk>.
            //     That is, S = D<S1...>, T = D<T1...>.
            //   o If type parameter Xi is declared to be invariant then Si must be identical to Ti.
            //   o If type parameter Xi is declared to be covariant ("out") then Si must be convertible
            //     to Ti via an identify conversion,  implicit reference conversion, or explicit reference conversion.
            //   o If type parameter Xi is declared to be contravariant ("in") then either Si must be identical to Ti,
            //     or Si and Ti must both be reference types.
            var sourceInfo = source.GetTypeInfo();
            var targetInfo = target.GetTypeInfo();
            if (!PrivateIsDelegate(source) || !PrivateIsDelegate(target) || !sourceInfo.IsGenericType || !targetInfo.IsGenericType)
            {
                return false;
            }
            var genericDelegate = source.GetGenericTypeDefinition();
            if (target.GetGenericTypeDefinition() != genericDelegate)
            {
                return false;
            }
            var genericParameters = genericDelegate.GetGenericArguments();
            var sourceArguments = source.GetGenericArguments();
            var destArguments = target.GetGenericArguments();
            for (var index = 0; index < genericParameters.Length; index++)
            {
                var sourceArgument = sourceArguments[index];
                var destArgument = destArguments[index];
                if (sourceArgument == destArgument)
                {
                    continue;
                }
                var genericParameter = genericParameters[index];
                if (PrivateIsInvariant(genericParameter))
                {
                    return false;
                }
                if (PrivateIsCovariant(genericParameter))
                {
                    if (!sourceArgument.HasReferenceConversionToInternal(destArgument))
                    {
                        return false;
                    }
                }
                else if (PrivateIsContravariant(genericParameter))
                {
                    var sourceArgumentInfo = sourceArgument.GetTypeInfo();
                    var destArgumentInfo = destArgument.GetTypeInfo();
                    if (sourceArgumentInfo.IsValueType || destArgumentInfo.IsValueType)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal static bool IsReferenceAssignableFrom(this Type type, Type source)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return type.IsReferenceAssignableFromInternal(source);
        }

        internal static bool IsReferenceAssignableFromInternal(this Type type, Type source)
        {
            // This actually implements "Is this identity assignable and/or reference assignable?"
            if (type == source)
            {
                return true;
            }
            var info = type.GetTypeInfo();
            var sourceInfo = source.GetTypeInfo();
            return !info.IsValueType
                   && !sourceInfo.IsValueType
                   && type.IsAssignableFrom(source);
        }

        internal static bool IsUnsigned(this TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        internal static bool IsValidInstanceType(MemberInfo member, Type instanceType)
        {
            var targetType = member.DeclaringType;
            if (targetType == null)
            {
                // Can this happen?
                return false;
            }
            if (targetType.IsReferenceAssignableFromInternal(instanceType))
            {
                return true;
            }
            var instanceInfo = instanceType.GetTypeInfo();
            if (instanceInfo.IsValueType)
            {
                if (targetType.IsReferenceAssignableFromInternal(typeof(object)))
                {
                    return true;
                }
                if (targetType.IsReferenceAssignableFromInternal(typeof(ValueType)))
                {
                    return true;
                }
                if (instanceInfo.IsEnum && targetType.IsReferenceAssignableFromInternal(typeof(Enum)))
                {
                    return true;
                }
                // A call to an interface implemented by a struct is legal whether the struct has
                // been boxed or not.
                var targetInfo = targetType.GetTypeInfo();
                if (targetInfo.IsInterface)
                {
                    foreach (var interfaceType in instanceType.GetInterfaces())
                    {
                        if (targetType.IsReferenceAssignableFromInternal(interfaceType))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal static bool MatchesArgumentTypes(this MethodInfo method, Type[] argTypes)
        {
            if (method == null || argTypes == null)
            {
                return false;
            }
            var parameters = method.GetParameters();
            if (parameters.Length != argTypes.Length)
            {
                return false;
            }
            for (var index = 0; index < parameters.Length; index++)
            {
                if (!IsReferenceAssignableFromInternal(parameters[index].ParameterType, argTypes[index]))
                {
                    return false;
                }
            }
            return true;
        }

        internal static void ValidateType(Type type, string paramName) => ValidateType(type, paramName, false, false);

        internal static void ValidateType(Type type, string paramName, bool allowByRef, bool allowPointer)
        {
            if (ValidateType(type, paramName, -1))
            {
                if (!allowByRef && type.IsByRef)
                {
                    throw Error.TypeMustNotBeByRef(paramName);
                }

                if (!allowPointer && type.IsPointer)
                {
                    throw Error.TypeMustNotBePointer(paramName);
                }
            }
        }

        internal static bool ValidateType(Type type, string paramName, int index)
        {
            if (type == typeof(void))
            {
                return false; // Caller can skip further checks.
            }

            if (type.ContainsGenericParameters)
            {
                throw type.IsGenericTypeDefinition
                    ? Error.TypeIsGeneric(type, paramName, index)
                    : Error.TypeContainsGenericParameters(type, paramName, index);
            }

            return true;
        }

        private static bool HasArrayToInterfaceConversion(Type source, Type target)
        {
            if (!source.IsSafeArray() || !target.GetTypeInfo().IsInterface || !target.GetTypeInfo().IsGenericType)
            {
                return false;
            }
            var targetTypeInfo = target.GetTypeInfo();
            var targetParams = targetTypeInfo.GetGenericArguments();
            if (targetParams.Length != 1)
            {
                return false;
            }
            var targetGen = target.GetGenericTypeDefinition();
            foreach (var currentInterface in _arrayAssignableInterfaces)
            {
                if (targetGen == currentInterface)
                {
                    return StrictHasReferenceConversionToInternal(source.GetElementType(), targetParams[0], false);
                }
            }
            return false;
        }

        private static bool HasInterfaceToArrayConversion(Type source, Type target)
        {
            if (!target.IsSafeArray() || !source.GetTypeInfo().IsInterface || !source.GetTypeInfo().IsGenericType)
            {
                return false;
            }
            var sourceTypeInfo = source.GetTypeInfo();
            var sourceParams = sourceTypeInfo.GetGenericArguments();
            if (sourceParams.Length != 1)
            {
                return false;
            }
            var sourceGen = source.GetGenericTypeDefinition();
            foreach (var currentInterface in _arrayAssignableInterfaces)
            {
                if (sourceGen == currentInterface)
                {
                    return StrictHasReferenceConversionToInternal(sourceParams[0], target.GetElementType(), false);
                }
            }
            return false;
        }

        private static bool PrivateIsContravariant(Type type)
        {
            var info = type.GetTypeInfo();
            return 0 != (info.GenericParameterAttributes & GenericParameterAttributes.Contravariant);
        }

        private static bool PrivateIsCovariant(Type type)
        {
            var info = type.GetTypeInfo();
            return 0 != (info.GenericParameterAttributes & GenericParameterAttributes.Covariant);
        }

        private static bool PrivateIsDelegate(Type type)
        {
            var info = type.GetTypeInfo();
            return info.IsSubclassOf(typeof(MulticastDelegate));
        }

        private static bool PrivateIsInvariant(Type type)
        {
            var info = type.GetTypeInfo();
            return 0 == (info.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        }

        private static bool StrictHasReferenceConversionTo(this Type source, Type target, bool skipNonArray)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            return source.StrictHasReferenceConversionToInternal(target, skipNonArray);
        }

        private static bool StrictHasReferenceConversionToInternal(this Type source, Type target, bool skipNonArray)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            var sourceTypeInfo = source.GetTypeInfo();
            var targetTypeInfo = target.GetTypeInfo();
            // HasReferenceConversionTo was both too strict and too lax. It was too strict in prohibiting
            // some valid conversions involving arrays, and too lax in allowing casts between interfaces
            // and sealed classes that don't implement them. Unfortunately fixing the lax cases would be
            // a breaking change, especially since such expressions will even work if only given null
            // arguments.
            // This method catches the cases that were incorrectly disallowed, but when it needs to
            // examine possible conversions of element or type parameters it applies stricter rules.
            while (true)
            {
                if (!skipNonArray) // Skip if we just came from HasReferenceConversionTo and have just tested these
                {
                    // ReSharper disable once PossibleNullReferenceException
                    if (sourceTypeInfo.IsValueType)
                    {
                        return false;
                    }
                    // ReSharper disable once PossibleNullReferenceException
                    if (targetTypeInfo.IsValueType)
                    {
                        return false;
                    }
                    // Includes to case of either being typeof(object)
                    if
                    (
                        // ReSharper disable once PossibleNullReferenceException
                        sourceTypeInfo.IsAssignableFrom(targetTypeInfo)
                        // ReSharper disable once PossibleNullReferenceException
                        || targetTypeInfo.IsAssignableFrom(sourceTypeInfo)
                    )
                    {
                        return true;
                    }
                    if (sourceTypeInfo.IsInterface)
                    {
                        if (targetTypeInfo.IsInterface || targetTypeInfo.IsClass && !targetTypeInfo.IsSealed)
                        {
                            return true;
                        }
                    }
                    else if (targetTypeInfo.IsInterface)
                    {
                        if (sourceTypeInfo.IsClass && !sourceTypeInfo.IsSealed)
                        {
                            return true;
                        }
                    }
                }
                if (source.IsArray)
                {
                    if (target.IsArray)
                    {
                        if (source.GetArrayRank() != target.GetArrayRank() || source.IsSafeArray() != target.IsSafeArray())
                        {
                            return false;
                        }
                        source = source.GetElementType();
                        target = target.GetElementType();
                        skipNonArray = false;
                    }
                    else
                    {
                        return HasArrayToInterfaceConversion(source, target);
                    }
                }
                else if (target.IsArray)
                {
                    if (HasInterfaceToArrayConversion(source, target))
                    {
                        return true;
                    }
                    return IsImplicitReferenceConversion(typeof(Array), source);
                }
                else
                {
                    return IsLegalExplicitVariantDelegateConversion(source, target);
                }
            }
        }
    }
}

#endif