#if NET20 || NET30

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;
using Theraot.Reflection;

namespace System.Linq.Expressions
{
    public partial class Expression
    {
        #region NewArrayInit

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> of the specified type from the provided initializers.
        /// </summary>
        /// <param name="type">A Type that represents the element type of the array.</param>
        /// <param name="initializers">The expressions used to create the array elements.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayInit"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayInit(Type type, params Expression[] initializers)
        {
            return NewArrayInit(type, (IEnumerable<Expression>)initializers);
        }

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> of the specified type from the provided initializers.
        /// </summary>
        /// <param name="type">A Type that represents the element type of the array.</param>
        /// <param name="initializers">The expressions used to create the array elements.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayInit"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayInit(Type type, IEnumerable<Expression> initializers)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            TypeUtils.ValidateType(type, nameof(type));
            var initializerList = initializers.ToTrueReadOnly();

            Expression[] newList = null;
            for (int i = 0, n = initializerList.Count; i < n; i++)
            {
                var expr = initializerList[i];
                ExpressionUtils.RequiresCanRead(expr, nameof(initializers), i);

                if (!type.IsReferenceAssignableFromInternal(expr.Type))
                {
                    if (!TryQuote(type, ref expr))
                    {
                        throw Error.ExpressionTypeCannotInitializeArrayType(expr.Type, type);
                    }
                    if (newList == null)
                    {
                        newList = new Expression[initializerList.Count];
                        for (var j = 0; j < i; j++)
                        {
                            newList[j] = initializerList[j];
                        }
                    }
                }
                if (newList != null)
                {
                    newList[i] = expr;
                }
            }
            if (newList != null)
            {
                initializerList = new TrueReadOnlyCollection<Expression>(newList);
            }

            return NewArrayExpression.Make(ExpressionType.NewArrayInit, type.MakeArrayType(), initializerList);
        }

        #endregion NewArrayInit

        #region NewArrayBounds

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> that represents creating an array that has a specified rank.
        /// </summary>
        /// <param name="type">A <see cref="System.Type"/> that represents the element type of the array.</param>
        /// <param name="bounds">An array that contains Expression objects to use to populate the <see cref="NewArrayExpression.Expressions"/> collection.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayBounds"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayBounds(Type type, params Expression[] bounds)
        {
            return NewArrayBounds(type, (IEnumerable<Expression>)bounds);
        }

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> that represents creating an array that has a specified rank.
        /// </summary>
        /// <param name="type">A <see cref="System.Type"/> that represents the element type of the array.</param>
        /// <param name="bounds">An <see cref="IEnumerable{T}"/> that contains <see cref="Expression"/> objects to use to populate the <see cref="NewArrayExpression.Expressions"/> collection.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayBounds"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayBounds(Type type, IEnumerable<Expression> bounds)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(bounds, nameof(bounds));

            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            TypeUtils.ValidateType(type, nameof(type));

            var boundsList = bounds.ToTrueReadOnly();

            var dimensions = boundsList.Count;
            if (dimensions <= 0)
            {
                throw Error.BoundsCannotBeLessThanOne(nameof(bounds));
            }

            for (var i = 0; i < dimensions; i++)
            {
                var expr = boundsList[i];
                ExpressionUtils.RequiresCanRead(expr, nameof(bounds), i);
                if (!expr.Type.IsInteger())
                {
                    throw Error.ArgumentMustBeInteger(nameof(bounds), i);
                }
            }

            Type arrayType;
            if (dimensions == 1)
            {
                //To get a vector, need call Type.MakeArrayType().
                //Type.MakeArrayType(1) gives a non-vector array, which will cause type check error.
                arrayType = type.MakeArrayType();
            }
            else
            {
                arrayType = type.MakeArrayType(dimensions);
            }

            return NewArrayExpression.Make(ExpressionType.NewArrayBounds, arrayType, boundsList);
        }

        #endregion NewArrayBounds
    }

    /// <summary>
    /// Represents creating a new array and possibly initializing the elements of the new array.
    /// </summary>
    [DebuggerTypeProxy(typeof(NewArrayExpressionProxy))]
    public class NewArrayExpression : Expression
    {
        private readonly Expression[] _expressions;
        private readonly TrueReadOnlyCollection<Expression> _expressionsAsReadOnlyCollection;

        internal NewArrayExpression(Type type, Expression[] expressions)
        {
            _expressions = expressions;
            Type = type;
            _expressionsAsReadOnlyCollection = new TrueReadOnlyCollection<Expression>(_expressions);
        }

        /// <summary>
        /// Gets the bounds of the array if the value of the <see cref="ExpressionType"/> property is NewArrayBounds, or the values to initialize the elements of the new array if the value of the <see cref="Expression.NodeType"/> property is NewArrayInit.
        /// </summary>
        public ReadOnlyCollection<Expression> Expressions => _expressionsAsReadOnlyCollection;

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type { get; }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="expressions">The <see cref="Expressions"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public NewArrayExpression Update(IEnumerable<Expression> expressions)
        {
            // Explicit null check here as otherwise wrong parameter name will be used.
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));

            if (ExpressionUtils.SameElements(ref expressions, _expressions))
            {
                return this;
            }

            return NodeType == ExpressionType.NewArrayInit
                ? NewArrayInit(Type.GetElementType(), expressions)
                : NewArrayBounds(Type.GetElementType(), expressions);
        }

        internal static NewArrayExpression Make(ExpressionType nodeType, Type type, ReadOnlyCollection<Expression> expressions)
        {
            Debug.Assert(type.IsArray);
            var expressionsArray = Theraot.Collections.Extensions.AsArray(expressions);
            if (nodeType == ExpressionType.NewArrayInit)
            {
                return new NewArrayInitExpression(type, expressionsArray);
            }

            return new NewArrayBoundsExpression(type, expressionsArray);
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitNewArray(this);
        }
    }

    internal sealed class NewArrayBoundsExpression : NewArrayExpression
    {
        internal NewArrayBoundsExpression(Type type, Expression[] expressions)
            : base(type, expressions)
        {
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public override ExpressionType NodeType => ExpressionType.NewArrayBounds;
    }

    internal sealed class NewArrayInitExpression : NewArrayExpression
    {
        internal NewArrayInitExpression(Type type, Expression[] expressions)
            : base(type, expressions)
        {
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public override ExpressionType NodeType => ExpressionType.NewArrayInit;
    }
}

#endif