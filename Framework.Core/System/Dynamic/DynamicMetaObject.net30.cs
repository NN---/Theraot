#if NET20 || NET30

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using Theraot.Collections.ThreadSafe;

namespace System.Dynamic
{
    /// <summary>
    /// Represents the dynamic binding and a binding logic of an object participating in the dynamic binding.
    /// </summary>
    public class DynamicMetaObject
    {
        /// <summary>
        /// Represents an empty array of type <see cref="DynamicMetaObject"/>. This field is read-only.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public static readonly DynamicMetaObject[] EmptyMetaObjects = ArrayReservoir<DynamicMetaObject>.EmptyArray;

        // having sentinel value means having no value. (this way we do not need a separate hasValue field)
        private static readonly object _noValueSentinel = new object();

        private readonly object _value = _noValueSentinel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMetaObject"/> class.
        /// </summary>
        /// <param name="expression">The expression representing this <see cref="DynamicMetaObject"/> during the dynamic binding process.</param>
        /// <param name="restrictions">The set of binding restrictions under which the binding is valid.</param>
        public DynamicMetaObject(Expression expression, BindingRestrictions restrictions)
        {
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            ContractUtils.RequiresNotNull(restrictions, nameof(restrictions));

            Expression = expression;
            Restrictions = restrictions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMetaObject"/> class.
        /// </summary>
        /// <param name="expression">The expression representing this <see cref="DynamicMetaObject"/> during the dynamic binding process.</param>
        /// <param name="restrictions">The set of binding restrictions under which the binding is valid.</param>
        /// <param name="value">The runtime value represented by the <see cref="DynamicMetaObject"/>.</param>
        public DynamicMetaObject(Expression expression, BindingRestrictions restrictions, object value)
            : this(expression, restrictions)
        {
            _value = value;
        }

        /// <summary>
        /// The expression representing the <see cref="DynamicMetaObject"/> during the dynamic binding process.
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="DynamicMetaObject"/> has the runtime value.
        /// </summary>
        public bool HasValue => _value != _noValueSentinel;

        /// <summary>
        /// Gets the limit type of the <see cref="DynamicMetaObject"/>.
        /// </summary>
        /// <remarks>Represents the most specific type known about the object represented by the <see cref="DynamicMetaObject"/>. <see cref="RuntimeType"/> if runtime value is available, a type of the <see cref="Expression"/> otherwise.</remarks>
        public Type LimitType => RuntimeType ?? Expression.Type;

        /// <summary>
        /// The set of binding restrictions under which the binding is valid.
        /// </summary>
        public BindingRestrictions Restrictions { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the runtime value or null if the <see cref="DynamicMetaObject"/> has no value associated with it.
        /// </summary>
        public Type RuntimeType
        {
            get
            {
                if (HasValue)
                {
                    Type ct = Expression.Type;
                    // ValueType at compile time, type cannot change.
                    if (ct.IsValueType)
                    {
                        return ct;
                    }

                    return Value?.GetType();
                }

                return null;
            }
        }

        /// <summary>
        /// The runtime value represented by this <see cref="DynamicMetaObject"/>.
        /// </summary>
        public object Value => HasValue ? _value : null;

        /// <summary>
        /// Creates a meta-object for the specified object.
        /// </summary>
        /// <param name="value">The object to get a meta-object for.</param>
        /// <param name="expression">The expression representing this <see cref="DynamicMetaObject"/> during the dynamic binding process.</param>
        /// <returns>
        /// If the given object implements <see cref="IDynamicMetaObjectProvider"/> and is not a remote object from outside the current AppDomain,
        /// returns the object's specific meta-object returned by <see cref="IDynamicMetaObjectProvider.GetMetaObject"/>. Otherwise a plain new meta-object
        /// with no restrictions is created and returned.
        /// </returns>
        public static DynamicMetaObject Create(object value, Expression expression)
        {
            ContractUtils.RequiresNotNull(expression, nameof(expression));

            if (value is IDynamicMetaObjectProvider ido)
            {
                var idoMetaObject = ido.GetMetaObject(expression);

                if (idoMetaObject == null ||
                    !idoMetaObject.HasValue ||
                    idoMetaObject.Value == null ||
                    idoMetaObject.Expression != expression)
                {
                    throw Error.InvalidMetaObjectCreated(ido.GetType());
                }

                return idoMetaObject;
            }

            return new DynamicMetaObject(expression, BindingRestrictions.Empty, value);
        }

        /// <summary>
        /// Performs the binding of the dynamic binary operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="BinaryOperationBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="arg">An instance of the <see cref="DynamicMetaObject"/> representing the right hand side of the binary operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackBinaryOperation(this, arg);
        }

        /// <summary>
        /// Performs the binding of the dynamic conversion operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="ConvertBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackConvert(this);
        }

        /// <summary>
        /// Performs the binding of the dynamic create instance operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="CreateInstanceBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the create instance operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackCreateInstance(this, args);
        }

        /// <summary>
        /// Performs the binding of the dynamic delete index operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="DeleteIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="indexes">An array of <see cref="DynamicMetaObject"/> instances - indexes for the delete index operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackDeleteIndex(this, indexes);
        }

        /// <summary>
        /// Performs the binding of the dynamic delete member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="DeleteMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackDeleteMember(this);
        }

        /// <summary>
        /// Performs the binding of the dynamic get index operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="indexes">An array of <see cref="DynamicMetaObject"/> instances - indexes for the get index operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackGetIndex(this, indexes);
        }

        /// <summary>
        /// Performs the binding of the dynamic get member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackGetMember(this);
        }

        /// <summary>
        /// Performs the binding of the dynamic invoke operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="InvokeBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the invoke operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackInvoke(this, args);
        }

        /// <summary>
        /// Performs the binding of the dynamic invoke member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="InvokeMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the invoke member operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackInvokeMember(this, args);
        }

        /// <summary>
        /// Performs the binding of the dynamic set index operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="SetIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="indexes">An array of <see cref="DynamicMetaObject"/> instances - indexes for the set index operation.</param>
        /// <param name="value">The <see cref="DynamicMetaObject"/> representing the value for the set index operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackSetIndex(this, indexes, value);
        }

        /// <summary>
        /// Performs the binding of the dynamic set member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="SetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="value">The <see cref="DynamicMetaObject"/> representing the value for the set member operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackSetMember(this, value);
        }

        /// <summary>
        /// Performs the binding of the dynamic unary operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="UnaryOperationBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public virtual DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.FallbackUnaryOperation(this);
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>The list of dynamic member names.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual IEnumerable<string> GetDynamicMemberNames() => ArrayReservoir<string>.EmptyArray;

        /// <summary>
        /// Returns the list of expressions represented by the <see cref="DynamicMetaObject"/> instances.
        /// </summary>
        /// <param name="objects">An array of <see cref="DynamicMetaObject"/> instances to extract expressions from.</param>
        /// <returns>The array of expressions.</returns>
        internal static Expression[] GetExpressions(DynamicMetaObject[] objects)
        {
            ContractUtils.RequiresNotNull(objects, nameof(objects));

            Expression[] res = new Expression[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                DynamicMetaObject mo = objects[i];
                ContractUtils.RequiresNotNull(mo, nameof(objects));
                Expression expr = mo.Expression;
                Debug.Assert(expr != null, "Unexpected null expression; ctor should have caught this.");
                res[i] = expr;
            }

            return res;
        }
    }
}

#endif