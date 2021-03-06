#if NET20 || NET30

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Theraot.Collections.ThreadSafe;
using static System.Linq.Expressions.CachedReflectionInfo;
using AstUtils = System.Linq.Expressions.Utils;

// ReSharper disable UnusedParameter.Global

namespace System.Dynamic
{
    /// <summary>
    /// Provides a simple class that can be inherited from to create an object with dynamic behavior
    /// at runtime.  Subclasses can override the various binder methods (<see cref="TryGetMember"/>,
    /// <see cref="TrySetMember"/>, <see cref="TryInvokeMember"/>, etc.) to provide custom behavior
    /// that will be invoked at runtime.
    ///
    /// If a method is not overridden then the <see cref="DynamicObject"/> does not directly support
    /// that behavior and the call site will determine how the binding should be performed.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DynamicObject : IDynamicMetaObjectProvider
    {
        /// <summary>
        /// Enables derived types to create a new instance of <see cref="DynamicObject"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="DynamicObject"/> instances cannot be directly instantiated because they have no
        /// implementation of dynamic behavior.
        /// </remarks>
        protected DynamicObject()
        {
        }

#region Public Virtual APIs

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>The list of dynamic member names.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual IEnumerable<string> GetDynamicMemberNames() => ArrayReservoir<string>.EmptyArray;

        /// <summary>
        /// Provides the implementation of performing a binary operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="arg">The right operand for the operation.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of converting the <see cref="DynamicObject"/> to another type.
        /// Derived classes can override this method to customize behavior.  When not overridden the
        /// call site requesting the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the conversion.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryConvert(ConvertBinder binder, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of creating an instance of the <see cref="DynamicObject"/>.
        /// Derived classes can override this method to customize behavior.  When not overridden the
        /// call site requesting the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments used for creation.</param>
        /// <param name="result">The created instance.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a delete index operation.  Derived classes
        /// can override this method to customize behavior.  When not overridden the call site
        /// requesting the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be deleted.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        public virtual bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes) => false;

        /// <summary>
        /// Provides the implementation of deleting a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        public virtual bool TryDeleteMember(DeleteMemberBinder binder) => false;

        /// <summary>
        /// Provides the implementation of performing a get index operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of getting a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the get operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of invoking the <see cref="DynamicObject"/>.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of calling a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a set index operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) => false;

        /// <summary>
        /// Provides the implementation of setting a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        public virtual bool TrySetMember(SetMemberBinder binder, object value) => false;

        /// <summary>
        /// Provides the implementation of performing a unary operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            result = null;
            return false;
        }

#endregion Public Virtual APIs

#region MetaDynamic

        private sealed class MetaDynamic : DynamicMetaObject
        {
            private static readonly Expression[] _noArgs = new Expression[0];

            internal MetaDynamic(Expression expression, DynamicObject value)
                            : base(expression, BindingRestrictions.Empty, value)
            {
            }

            private delegate DynamicMetaObject Fallback<in TBinder>(MetaDynamic @this, TBinder binder, DynamicMetaObject errorSuggestion);

            private new DynamicObject Value => (DynamicObject)base.Value;

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                if (IsOverridden(DynamicObjectTryBinaryOperation))
                {
                    DynamicMetaObject localArg = arg;

                    return CallMethodWithResult(
                        DynamicObjectTryBinaryOperation,
                        binder,
                        new[] { arg.Expression },
                        (@this, b, e) => b.FallbackBinaryOperation(@this, localArg, e)
                    );
                }

                return base.BindBinaryOperation(binder, arg);
            }

            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                if (IsOverridden(DynamicObjectTryConvert))
                {
                    return CallMethodWithResult(
                        DynamicObjectTryConvert,
                        binder,
                        _noArgs,
                        (@this, b, e) => b.FallbackConvert(@this, e)
                    );
                }

                return base.BindConvert(binder);
            }

            public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
            {
                if (IsOverridden(DynamicObjectTryCreateInstance))
                {
                    DynamicMetaObject[] localArgs = args;

                    return CallMethodWithResult(
                        DynamicObjectTryCreateInstance,
                        binder,
                        GetExpressions(args),
                        (@this, b, e) => b.FallbackCreateInstance(@this, localArgs, e)
                    );
                }

                return base.BindCreateInstance(binder, args);
            }

            public override DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (IsOverridden(DynamicObjectTryDeleteIndex))
                {
                    DynamicMetaObject[] localIndexes = indexes;

                    return CallMethodNoResult(
                        DynamicObjectTryDeleteIndex,
                        binder,
                        GetExpressions(indexes),
                        (@this, b, e) => b.FallbackDeleteIndex(@this, localIndexes, e)
                    );
                }

                return base.BindDeleteIndex(binder, indexes);
            }

            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
            {
                if (IsOverridden(DynamicObjectTryDeleteMember))
                {
                    return CallMethodNoResult(
                        DynamicObjectTryDeleteMember,
                        binder,
                        _noArgs,
                        (@this, b, e) => b.FallbackDeleteMember(@this, e)
                    );
                }

                return base.BindDeleteMember(binder);
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (IsOverridden(DynamicObjectTryGetIndex))
                {
                    DynamicMetaObject[] localIndexes = indexes;

                    return CallMethodWithResult(
                        DynamicObjectTryGetIndex,
                        binder,
                        GetExpressions(indexes),
                        (@this, b, e) => b.FallbackGetIndex(@this, localIndexes, e)
                    );
                }

                return base.BindGetIndex(binder, indexes);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                if (IsOverridden(DynamicObjectTryGetMember))
                {
                    return CallMethodWithResult(
                        DynamicObjectTryGetMember,
                        binder,
                        _noArgs,
                        (@this, b, e) => b.FallbackGetMember(@this, e)
                    );
                }

                return base.BindGetMember(binder);
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                if (IsOverridden(DynamicObjectTryInvoke))
                {
                    DynamicMetaObject[] localArgs = args;

                    return CallMethodWithResult(
                        DynamicObjectTryInvoke,
                        binder,
                        GetExpressions(args),
                        (@this, b, e) => b.FallbackInvoke(@this, localArgs, e)
                    );
                }

                return base.BindInvoke(binder, args);
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                // Generate a tree like:
                //
                // {
                //   object result;
                //   TryInvokeMember(payload, out result)
                //      ? result
                //      : TryGetMember(payload, out result)
                //          ? FallbackInvoke(result)
                //          : fallbackResult
                // }
                //
                // Then it calls FallbackInvokeMember with this tree as the
                // "error", giving the language the option of using this
                // tree or doing .NET binding.
                //
                DynamicMetaObject call = BuildCallMethodWithResult(
                    DynamicObjectTryInvokeMember,
                    binder,
                    GetExpressions(args),
                    BuildCallMethodWithResult(
                        DynamicObjectTryGetMember,
                        new GetBinderAdapter(binder),
                        _noArgs,
                        binder.FallbackInvokeMember(this, args, null),
                        (MetaDynamic @this, GetMemberBinder ignored, DynamicMetaObject e) => binder.FallbackInvoke(e, args, null)
                    ),
                    null
                );

                return binder.FallbackInvokeMember(this, args, call);
            }

            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                if (IsOverridden(DynamicObjectTrySetIndex))
                {
                    DynamicMetaObject[] localIndexes = indexes;
                    DynamicMetaObject localValue = value;

                    return CallMethodReturnLast(
                        DynamicObjectTrySetIndex,
                        binder,
                        GetExpressions(indexes),
                        value.Expression,
                        (@this, b, e) => b.FallbackSetIndex(@this, localIndexes, localValue, e)
                    );
                }

                return base.BindSetIndex(binder, indexes, value);
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                if (IsOverridden(DynamicObjectTrySetMember))
                {
                    DynamicMetaObject localValue = value;

                    return CallMethodReturnLast(
                        DynamicObjectTrySetMember,
                        binder,
                        _noArgs,
                        value.Expression,
                        (@this, b, e) => b.FallbackSetMember(@this, localValue, e)
                    );
                }

                return base.BindSetMember(binder, value);
            }

            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                if (IsOverridden(DynamicObjectTryUnaryOperation))
                {
                    return CallMethodWithResult(
                        DynamicObjectTryUnaryOperation,
                        binder,
                        _noArgs,
                        (@this, b, e) => b.FallbackUnaryOperation(@this, e)
                    );
                }

                return base.BindUnaryOperation(binder);
            }

            public override IEnumerable<string> GetDynamicMemberNames() => Value.GetDynamicMemberNames();

            // used in reference comparison, requires unique object identity

            /// <summary>
            /// Helper method for generating arguments for calling methods
            /// on DynamicObject.  parameters is either a list of ParameterExpressions
            /// to be passed to the method as an object[], or NoArgs to signify that
            /// the target method takes no object[] parameter.
            /// </summary>
            private static Expression[] BuildCallArgs<TBinder>(TBinder binder, Expression[] parameters, Expression arg0, Expression arg1)
                where TBinder : DynamicMetaObjectBinder
            {
                if (parameters != _noArgs)
                {
                    return arg1 != null ? new[] { Constant(binder), arg0, arg1 } : new[] { Constant(binder), arg0 };
                }
                return arg1 != null ? new[] { Constant(binder), arg1 } : new Expression[] { Constant(binder) };
            }

            private static ConstantExpression Constant<TBinder>(TBinder binder)
            {
                return Expression.Constant(binder, typeof(TBinder));
            }

            private static ReadOnlyCollection<Expression> GetConvertedArgs(params Expression[] args)
            {
                var paramArgs = new Expression[args.Length];

                for (int i = 0; i < args.Length; i++)
                {
                    paramArgs[i] = Expression.Convert(args[i], typeof(object));
                }

                return new TrueReadOnlyCollection<Expression>(paramArgs);
            }

            /// <summary>
            /// Helper method for generating expressions that assign byRef call
            /// parameters back to their original variables.
            /// </summary>
            private static Expression ReferenceArgAssign(Expression callArgs, Expression[] args)
            {
                ReadOnlyCollectionBuilder<Expression> block = null;

                for (int i = 0; i < args.Length; i++)
                {
                    ParameterExpression variable = args[i] as ParameterExpression;
                    ContractUtils.Requires(variable != null, nameof(args));

                    // ReSharper disable once PossibleNullReferenceException
                    if (variable.IsByRef)
                    {
                        if (block == null)
                        {
                            block = new ReadOnlyCollectionBuilder<Expression>();
                        }

                        block.Add(
                            Expression.Assign(
                                variable,
                                Expression.Convert(
                                    Expression.ArrayIndex(
                                        callArgs,
                                        AstUtils.Constant(i)
                                    ),
                                    variable.Type
                                )
                            )
                        );
                    }
                }

                if (block != null)
                {
                    return Expression.Block(block);
                }

                return AstUtils.Empty;
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on DynamicObject that returns a result.
            ///
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject BuildCallMethodWithResult<TBinder>(MethodInfo method, TBinder binder, Expression[] args, DynamicMetaObject fallbackResult, Fallback<TBinder> fallbackInvoke)
                where TBinder : DynamicMetaObjectBinder
            {
                if (!IsOverridden(method))
                {
                    return fallbackResult;
                }

                //
                // Build a new expression like:
                // {
                //   object result;
                //   TryGetMember(payload, out result) ? fallbackInvoke(result) : fallbackResult
                // }
                //
                ParameterExpression result = Expression.Parameter(typeof(object), null);
                ParameterExpression callArgs = method != DynamicObjectTryBinaryOperation ? Expression.Parameter(typeof(object[]), null) : Expression.Parameter(typeof(object), null);
                ReadOnlyCollection<Expression> callArgsValue = GetConvertedArgs(args);

                var resultMetaObject = new DynamicMetaObject(result, BindingRestrictions.Empty);

                // Need to add a conversion if calling TryConvert
                if (binder.ReturnType != typeof(object))
                {
                    Debug.Assert(binder is ConvertBinder && fallbackInvoke == null);

                    UnaryExpression convert = Expression.Convert(resultMetaObject.Expression, binder.ReturnType);
                    // will always be a cast or unbox
                    Debug.Assert(convert.Method == null);

                    // Prepare a good exception message in case the convert will fail
                    string convertFailed = Strings.DynamicObjectResultNotAssignable(
                        "{0}",
                        Value.GetType(),
                        binder.GetType(),
                        binder.ReturnType
                    );

                    Expression condition;
                    // If the return type can not be assigned null then just check for type assignability otherwise allow null.
                    if (binder.ReturnType.IsValueType && Nullable.GetUnderlyingType(binder.ReturnType) == null)
                    {
                        condition = Expression.TypeIs(resultMetaObject.Expression, binder.ReturnType);
                    }
                    else
                    {
                        condition = Expression.OrElse(
                                        Expression.Equal(resultMetaObject.Expression, AstUtils.Null),
                                        Expression.TypeIs(resultMetaObject.Expression, binder.ReturnType));
                    }

                    Expression checkedConvert = Expression.Condition(
                        condition,
                        convert,
                        Expression.Throw(
                            Expression.New(
                                InvalidCastExceptionCtorString,
                                new TrueReadOnlyCollection<Expression>(
                                    Expression.Call(
                                        StringFormatStringObjectArray,
                                        Expression.Constant(convertFailed),
                                        Expression.NewArrayInit(
                                            typeof(object),
                                            new TrueReadOnlyCollection<Expression>(
                                                Expression.Condition(
                                                    Expression.Equal(resultMetaObject.Expression, AstUtils.Null),
                                                    Expression.Constant("null"),
                                                    Expression.Call(
                                                        resultMetaObject.Expression,
                                                        ObjectGetType
                                                    ),
                                                    typeof(object)
                                                )
                                            )
                                        )
                                    )
                                )
                            ),
                            binder.ReturnType
                        ),
                        binder.ReturnType
                    );

                    resultMetaObject = new DynamicMetaObject(checkedConvert, resultMetaObject.Restrictions);
                }

                if (fallbackInvoke != null)
                {
                    resultMetaObject = fallbackInvoke(this, binder, resultMetaObject);
                }

                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new TrueReadOnlyCollection<ParameterExpression>(result, callArgs),
                        new TrueReadOnlyCollection<Expression>(
                            method != DynamicObjectTryBinaryOperation ? Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)) : Expression.Assign(callArgs, callArgsValue[0]),
                            Expression.Condition(
                                Expression.Call(
                                    GetLimitedSelf(),
                                    method,
                                    BuildCallArgs(
                                        binder,
                                        args,
                                        callArgs,
                                        result
                                    )
                                ),
                                Expression.Block(
                                    method != DynamicObjectTryBinaryOperation ? ReferenceArgAssign(callArgs, args) : AstUtils.Empty,
                                    resultMetaObject.Expression
                                ),
                                fallbackResult.Expression,
                                binder.ReturnType
                            )
                        )
                    ),
                    GetRestrictions().Merge(resultMetaObject.Restrictions).Merge(fallbackResult.Restrictions)
                );
                return callDynamic;
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic, but uses one of the arguments for
            /// the result.
            ///
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject CallMethodNoResult<TBinder>(MethodInfo method, TBinder binder, Expression[] args, Fallback<TBinder> fallback)
                where TBinder : DynamicMetaObjectBinder
            {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                DynamicMetaObject fallbackResult = fallback(this, binder, null);
                ParameterExpression callArgs = Expression.Parameter(typeof(object[]), null);
                ReadOnlyCollection<Expression> callArgsValue = GetConvertedArgs(args);

                //
                // Build a new expression like:
                //   if (TryDeleteMember(payload)) { } else { fallbackResult }
                //
                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new TrueReadOnlyCollection<ParameterExpression>(callArgs),
                        new TrueReadOnlyCollection<Expression>(
                            Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                            Expression.Condition(
                                Expression.Call(
                                    GetLimitedSelf(),
                                    method,
                                    BuildCallArgs(
                                        binder,
                                        args,
                                        callArgs,
                                        null
                                    )
                                ),
                                Expression.Block(
                                    ReferenceArgAssign(callArgs, args),
                                    AstUtils.Empty
                                ),
                                fallbackResult.Expression,
                                typeof(void)
                            )
                        )
                    ),
                    GetRestrictions().Merge(fallbackResult.Restrictions)
                );

                //
                // Now, call fallback again using our new MO as the error
                // When we do this, one of two things can happen:
                //   1. Binding will succeed, and it will ignore our call to
                //      the dynamic method, OR
                //   2. Binding will fail, and it will use the MO we created
                //      above.
                //
                return fallback(this, binder, callDynamic);
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic, but uses one of the arguments for
            /// the result.
            ///
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject CallMethodReturnLast<TBinder>(MethodInfo method, TBinder binder, Expression[] args, Expression value, Fallback<TBinder> fallback)
                where TBinder : DynamicMetaObjectBinder
            {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                DynamicMetaObject fallbackResult = fallback(this, binder, null);

                //
                // Build a new expression like:
                // {
                //   object result;
                //   TrySetMember(payload, result = value) ? result : fallbackResult
                // }
                //

                ParameterExpression result = Expression.Parameter(typeof(object), null);
                ParameterExpression callArgs = Expression.Parameter(typeof(object[]), null);
                ReadOnlyCollection<Expression> callArgsValue = GetConvertedArgs(args);

                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new TrueReadOnlyCollection<ParameterExpression>(result, callArgs),
                        new TrueReadOnlyCollection<Expression>(
                            Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                            Expression.Condition(
                                Expression.Call(
                                    GetLimitedSelf(),
                                    method,
                                    BuildCallArgs(
                                        binder,
                                        args,
                                        callArgs,
                                        Expression.Assign(result, Expression.Convert(value, typeof(object)))
                                    )
                                ),
                                Expression.Block(
                                    ReferenceArgAssign(callArgs, args),
                                    result
                                ),
                                fallbackResult.Expression,
                                typeof(object)
                            )
                        )
                    ),
                    GetRestrictions().Merge(fallbackResult.Restrictions)
                );

                //
                // Now, call fallback again using our new MO as the error
                // When we do this, one of two things can happen:
                //   1. Binding will succeed, and it will ignore our call to
                //      the dynamic method, OR
                //   2. Binding will fail, and it will use the MO we created
                //      above.
                //
                return fallback(this, binder, callDynamic);
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic that returns a result
            /// </summary>
            private DynamicMetaObject CallMethodWithResult<TBinder>(MethodInfo method, TBinder binder, Expression[] args, Fallback<TBinder> fallback, Fallback<TBinder> fallbackInvoke = null)
                where TBinder : DynamicMetaObjectBinder
            {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                DynamicMetaObject fallbackResult = fallback(this, binder, null);

                DynamicMetaObject callDynamic = BuildCallMethodWithResult(method, binder, args, fallbackResult, fallbackInvoke);

                //
                // Now, call fallback again using our new MO as the error
                // When we do this, one of two things can happen:
                //   1. Binding will succeed, and it will ignore our call to
                //      the dynamic method, OR
                //   2. Binding will fail, and it will use the MO we created
                //      above.
                //
                return fallback(this, binder, callDynamic);
            }

            /// <summary>
            /// Returns our Expression converted to DynamicObject
            /// </summary>
            private Expression GetLimitedSelf()
            {
                // Convert to DynamicObject rather than LimitType, because
                // the limit type might be non-public.
                if (TypeUtils.AreEquivalent(Expression.Type, typeof(DynamicObject)))
                {
                    return Expression;
                }
                return Expression.Convert(Expression, typeof(DynamicObject));
            }

            /// <summary>
            /// Returns a Restrictions object which includes our current restrictions merged
            /// with a restriction limiting our type
            /// </summary>
            private BindingRestrictions GetRestrictions()
            {
                Debug.Assert(Restrictions == BindingRestrictions.Empty, "We don't merge, restrictions are always empty");

                return BindingRestrictions.GetTypeRestriction(this);
            }

            /// <summary>
            /// Checks if the derived type has overridden the specified method.  If there is no
            /// implementation for the method provided then Dynamic falls back to the base class
            /// behavior which lets the call site determine how the binder is performed.
            /// </summary>
            private bool IsOverridden(MethodInfo method)
            {
                MemberInfo[] methods = Value.GetType().GetMember(method.Name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (MethodInfo mi in methods)
                {
                    if (mi.DeclaringType != typeof(DynamicObject) && mi.GetBaseDefinition() == method)
                    {
                        return true;
                    }
                }

                return false;
            }

            // It is okay to throw NotSupported from this binder. This object
            // is only used by DynamicObject.GetMember--it is not expected to
            // (and cannot) implement binding semantics. It is just so the DO
            // can use the Name and IgnoreCase properties.
            private sealed class GetBinderAdapter : GetMemberBinder
            {
                internal GetBinderAdapter(InvokeMemberBinder binder)
                    : base(binder.Name, binder.IgnoreCase)
                {
                }

                public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
                {
                    throw new NotSupportedException();
                }
            }
        }

#endregion MetaDynamic

#region IDynamicMetaObjectProvider Members

        /// <summary>
        /// Returns the <see cref="DynamicMetaObject" /> responsible for binding operations performed on this object,
        /// using the virtual methods provided by this class.
        /// </summary>
        /// <param name="parameter">The expression tree representation of the runtime value.</param>
        /// <returns>
        /// The <see cref="DynamicMetaObject" /> to bind this object.  The object can be encapsulated inside of another
        /// <see cref="DynamicMetaObject"/> to provide custom behavior for individual actions.
        /// </returns>
        public virtual DynamicMetaObject GetMetaObject(Expression parameter) => new MetaDynamic(parameter, this);

#endregion IDynamicMetaObjectProvider Members
    }
}

#endif