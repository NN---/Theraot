#if NET20 || NET30

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;
using System.Threading;
using Theraot.Collections.ThreadSafe;
using Theraot.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a block that contains a sequence of expressions where variables can be defined.
    /// </summary>
    [DebuggerTypeProxy(typeof(BlockExpressionProxy))]
    public class BlockExpression : Expression
    {
        internal BlockExpression()
        {
        }

        /// <summary>
        /// Gets the expressions in this block.
        /// </summary>
        public ReadOnlyCollection<Expression> Expressions => GetOrMakeExpressions();

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Block;

        /// <summary>
        /// Gets the last expression in this block.
        /// </summary>
        public Expression Result => GetExpression(ExpressionCount - 1);

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public override Type Type => GetExpression(ExpressionCount - 1).Type;

        /// <summary>
        /// Gets the variables defined in this block.
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Variables => GetOrMakeVariables();

        internal virtual int ExpressionCount => throw ContractUtils.Unreachable;

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="variables">The <see cref="Variables"/> property of the result.</param>
        /// <param name="expressions">The <see cref="Expressions"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public BlockExpression Update(IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
        {
            if (expressions != null)
            {
                // Ensure variables is safe to enumerate twice.
                // (If this means a second call to ToTrueReadOnly it will return quickly).
                ICollection<ParameterExpression> vars;
                if (variables == null)
                {
                    vars = null;
                }
                else
                {
                    vars = variables as ICollection<ParameterExpression>;
                    if (vars == null)
                    {
                        variables = vars = variables.ToTrueReadOnly();
                    }
                }

                if (SameVariables(vars))
                {
                    // Ensure expressions is safe to enumerate twice.
                    // (If this means a second call to ToTrueReadOnly it will return quickly).
                    if (!(expressions is ICollection<Expression> expressionsAsCollection))
                    {
                        expressions = expressionsAsCollection = expressions.ToTrueReadOnly();
                    }
                    if (SameExpressions(expressionsAsCollection))
                    {
                        return this;
                    }
                }
            }

            return Block(Type, variables, expressions);
        }

        /// <summary>
        /// Helper used for ensuring we only return 1 instance of a ReadOnlyCollection of T.
        ///
        /// This is similar to the ReturnReadOnly which only takes a single argument. This version
        /// supports nodes which hold onto 5 Expressions and puts all of the arguments into the
        /// ReadOnlyCollection.
        ///
        /// Ultimately this means if we create the read-only collection we will be slightly more wasteful as we'll
        /// have a read-only collection + some fields in the type.  The DLR internally avoids accessing anything
        /// which would force the read-only collection to be created.
        ///
        /// This is used by BlockExpression5 and MethodCallExpression5.
        /// </summary>
        internal static ReadOnlyCollection<Expression> ReturnReadOnlyExpressions(BlockExpression provider, ref object collection)
        {
            if (collection is Expression tObj)
            {
                // otherwise make sure only one read-only collection ever gets exposed
                Interlocked.CompareExchange(
                    ref collection,
                    new ReadOnlyCollection<Expression>(new BlockExpressionList(provider, tObj)),
                    tObj
                );
            }

            // and return what is not guaranteed to be a read-only collection
            return (ReadOnlyCollection<Expression>)collection;
        }

        internal virtual Expression GetExpression(int index)
        {
            throw ContractUtils.Unreachable;
        }

        internal virtual ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            throw ContractUtils.Unreachable;
        }

        internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
        {
            return EmptyReadOnlyCollection<ParameterExpression>.Instance;
        }

        /// <summary>
        /// Makes a copy of this node replacing the parameters/args with the provided values.  The
        /// shape of the parameters/args needs to match the shape of the current block - in other
        /// words there should be the same # of parameters and args.
        ///
        /// parameters can be null in which case the existing parameters are used.
        ///
        /// This helper is provided to allow re-writing of nodes to not depend on the specific optimized
        /// subclass of BlockExpression which is being used.
        /// </summary>
        internal virtual BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            throw ContractUtils.Unreachable;
        }

        internal virtual bool SameExpressions(ICollection<Expression> expressions)
        {
            throw ContractUtils.Unreachable;
        }

        internal virtual bool SameVariables(ICollection<ParameterExpression> variables) =>
                    variables == null || variables.Count == 0;

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitBlock(this);
        }
    }

    #region Specialized Subclasses

    internal sealed class Block2 : BlockExpression
    {
        private readonly Expression _arg1;
        private object _arg0;                   // storage for the 1st argument or a read-only collection.  See IArgumentProvider
                                                // storage for the 2nd argument.

        internal Block2(Expression arg0, Expression arg1)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        internal override int ExpressionCount => 2;

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ExpressionUtils.ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 2);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block2(args[0], args[1]);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions)
        {
            Debug.Assert(expressions != null);
            if (expressions.Count == 2)
            {
                if (_arg0 is Expression[] alreadyArray)
                {
                    return ExpressionUtils.SameElements(expressions, alreadyArray);
                }

                using (var en = expressions.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        return en.Current == _arg1;
                    }
                }
            }

            return false;
        }
    }

    internal sealed class Block3 : BlockExpression
    {
        private readonly Expression _arg1, _arg2;
        private object _arg0;                       // storage for the 1st argument or a read-only collection.  See IArgumentProvider
                                                    // storage for the 2nd and 3rd arguments.

        internal Block3(Expression arg0, Expression arg1, Expression arg2)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        internal override int ExpressionCount => 3;

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ExpressionUtils.ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 3);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block3(args[0], args[1], args[2]);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions)
        {
            Debug.Assert(expressions != null);
            if (expressions.Count == 3)
            {
                if (_arg0 is Expression[] alreadyArray)
                {
                    return ExpressionUtils.SameElements(expressions, alreadyArray);
                }

                using (var en = expressions.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            return en.Current == _arg2;
                        }
                    }
                }
            }

            return false;
        }
    }

    internal sealed class Block4 : BlockExpression
    {
        private readonly Expression _arg1, _arg2, _arg3;
        private object _arg0;                               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
                                                            // storage for the 2nd, 3rd, and 4th arguments.

        internal Block4(Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        internal override int ExpressionCount => 4;

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ExpressionUtils.ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                case 3: return _arg3;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 4);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block4(args[0], args[1], args[2], args[3]);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions)
        {
            Debug.Assert(expressions != null);
            if (expressions.Count == 4)
            {
                if (_arg0 is Expression[] alreadyArray)
                {
                    return ExpressionUtils.SameElements(expressions, alreadyArray);
                }

                using (var en = expressions.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            if (en.Current == _arg2)
                            {
                                en.MoveNext();
                                return en.Current == _arg3;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }

    internal sealed class Block5 : BlockExpression
    {
        private readonly Expression _arg1, _arg2, _arg3, _arg4;
        private object _arg0;                                       // storage for the 1st argument or a read-only collection.  See IArgumentProvider
                                                                    // storage for the 2nd - 5th args.

        internal Block5(Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        internal override int ExpressionCount => 5;

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ExpressionUtils.ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                case 3: return _arg3;
                case 4: return _arg4;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 5);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block5(args[0], args[1], args[2], args[3], args[4]);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions)
        {
            Debug.Assert(expressions != null);
            if (expressions.Count == 5)
            {
                if (_arg0 is Expression[] alreadyArray)
                {
                    return ExpressionUtils.SameElements(expressions, alreadyArray);
                }

                using (var en = expressions.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            if (en.Current == _arg2)
                            {
                                en.MoveNext();
                                if (en.Current == _arg3)
                                {
                                    en.MoveNext();
                                    return en.Current == _arg4;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }

    internal class BlockN : BlockExpression
    {
        private readonly Expression[] _expressions;
        private readonly TrueReadOnlyCollection<Expression> _expressionsAsReadOnlyCollection;

        internal BlockN(Expression[] expressions)
        {
            Debug.Assert(expressions.Length != 0);

            _expressions = expressions;
            _expressionsAsReadOnlyCollection = new TrueReadOnlyCollection<Expression>(_expressions);
        }

        internal override int ExpressionCount => _expressions.Length;

        internal override Expression GetExpression(int index)
        {
            Debug.Assert(index >= 0 && index < _expressions.Length);

            return _expressions[index];
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return _expressionsAsReadOnlyCollection;
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(variables == null || variables.Count == 0);
            Debug.Assert(args != null);

            return new BlockN(args);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions) =>
                                            ExpressionUtils.SameElements(expressions, _expressions);
    }

    internal sealed class Scope1 : ScopeExpression
    {
        private object _body;

        internal Scope1(ParameterExpression[] variables, Expression body)
            : this(variables, (object)body)
        {
        }

        private Scope1(ParameterExpression[] variables, object body)
            : base(variables)
        {
            _body = body;
        }

        internal override int ExpressionCount => 1;

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ExpressionUtils.ReturnObject<Expression>(_body);
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _body);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            var array = Theraot.Collections.Extensions.AsArray(variables);
            if (args == null)
            {
                Debug.Assert(variables.Count == Variables.Count);
                ValidateVariables(array, nameof(variables));
                return new Scope1(array, _body);
            }
            Debug.Assert(args.Length == 1);
            Debug.Assert(variables == null || variables.Count == Variables.Count);

            return new Scope1(ReuseOrValidateVariables(array), args[0]);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions)
        {
            Debug.Assert(expressions != null);
            if (expressions.Count == 1)
            {
                if (_body is Expression[] alreadyArray)
                {
                    return ExpressionUtils.SameElements(expressions, alreadyArray);
                }

                using (var en = expressions.GetEnumerator())
                {
                    en.MoveNext();
                    return ExpressionUtils.ReturnObject<Expression>(_body) == en.Current;
                }
            }

            return false;
        }
    }

    internal class ScopeExpression : BlockExpression
    {
        private readonly ParameterExpression[] _variables;      // list of variables or ReadOnlyCollection if the user has accessed the read-only collection
        private readonly TrueReadOnlyCollection<ParameterExpression> _variablesAsReadOnlyCollection;

        internal ScopeExpression(ParameterExpression[] variables)
        {
            _variables = variables;
            _variablesAsReadOnlyCollection = new TrueReadOnlyCollection<ParameterExpression>(_variables);
        }

        protected IReadOnlyList<ParameterExpression> VariablesList => _variablesAsReadOnlyCollection;

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
        {
            return _variablesAsReadOnlyCollection;
        }

        // Used for rewrite of the nodes to either reuse existing set of variables if not rewritten.
        internal ParameterExpression[] ReuseOrValidateVariables(ParameterExpression[] variables)
        {
            if (variables != null && variables != _variables)
            {
                // Need to validate the new variables (uniqueness, not byref)
                ValidateVariables(variables, nameof(variables));
                return variables;
            }

            return _variables;
        }

        internal override bool SameVariables(ICollection<ParameterExpression> variables) =>
                                    ExpressionUtils.SameElements(variables, _variables);
    }

    internal class ScopeN : ScopeExpression
    {
        private readonly Expression[] _body;
        private readonly TrueReadOnlyCollection<Expression> _bodyAsReadOnlyCollection;

        internal ScopeN(ParameterExpression[] variables, Expression[] body)
            : base(variables)
        {
            _body = body;
            _bodyAsReadOnlyCollection = new TrueReadOnlyCollection<Expression>(_body);
        }

        internal override int ExpressionCount => _body.Length;

        protected IReadOnlyList<Expression> Body => _bodyAsReadOnlyCollection;

        internal override Expression GetExpression(int index) => _body[index];

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return _bodyAsReadOnlyCollection;
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            var array = Theraot.Collections.Extensions.AsArray(variables);
            if (args == null)
            {
                Debug.Assert(variables.Count == Variables.Count);
                ValidateVariables(array, nameof(variables));
                return new ScopeN(array, _body);
            }
            Debug.Assert(args.Length == ExpressionCount);
            Debug.Assert(variables == null || variables.Count == Variables.Count);

            return new ScopeN(ReuseOrValidateVariables(array), args);
        }

        internal override bool SameExpressions(ICollection<Expression> expressions) =>
                                                    ExpressionUtils.SameElements(expressions, _body);
    }

    internal sealed class ScopeWithType : ScopeN
    {
        internal ScopeWithType(ParameterExpression[] variables, Expression[] expressions, Type type)
            : base(variables, expressions)
        {
            Type = type;
        }

        public override Type Type { get; }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            var array = Theraot.Collections.Extensions.AsArray(variables);
            if (args == null)
            {
                Debug.Assert(variables.Count == Variables.Count);
                ValidateVariables(array, nameof(variables));
                return new ScopeWithType(array, Theraot.Collections.Extensions.AsArray(Body), Type);
            }
            Debug.Assert(args.Length == ExpressionCount);
            Debug.Assert(variables == null || variables.Count == Variables.Count);

            return new ScopeWithType(ReuseOrValidateVariables(array), args, Type);
        }
    }

    #endregion Specialized Subclasses

    #region Block List Classes

    /// <summary>
    /// Provides a wrapper around an IArgumentProvider which exposes the argument providers
    /// members out as an IList of Expression.  This is used to avoid allocating an array
    /// which needs to be stored inside of a ReadOnlyCollection.  Instead this type has
    /// the same amount of overhead as an array without duplicating the storage of the
    /// elements.  This ensures that internally we can avoid creating and copying arrays
    /// while users of the Expression trees also don't pay a size penalty for this internal
    /// optimization.  See IArgumentProvider for more general information on the Expression
    /// tree optimizations being used here.
    /// </summary>
    internal class BlockExpressionList : IList<Expression>
    {
        private readonly Expression _arg0;
        private readonly BlockExpression _block;

        internal BlockExpressionList(BlockExpression provider, Expression arg0)
        {
            _block = provider;
            _arg0 = arg0;
        }

        #region IList<Expression> Members

        public Expression this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return _arg0;
                }

                return _block.GetExpression(index);
            }
            set => throw ContractUtils.Unreachable;
        }

        public int IndexOf(Expression item)
        {
            if (_arg0 == item)
            {
                return 0;
            }

            for (var i = 1; i < _block.ExpressionCount; i++)
            {
                if (_block.GetExpression(i) == item)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, Expression item)
        {
            throw ContractUtils.Unreachable;
        }

        public void RemoveAt(int index)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion IList<Expression> Members

        #region ICollection<Expression> Members

        public int Count => _block.ExpressionCount;

        public bool IsReadOnly => throw ContractUtils.Unreachable;

        public void Add(Expression item)
        {
            throw ContractUtils.Unreachable;
        }

        public void Clear()
        {
            throw ContractUtils.Unreachable;
        }

        public bool Contains(Expression item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(Expression[] array, int index)
        {
            ContractUtils.RequiresNotNull(array, nameof(array));
            if (index < 0)
            {
                throw Error.ArgumentOutOfRange(nameof(index));
            }

            var n = _block.ExpressionCount;
            Debug.Assert(n > 0);
            if (index + n > array.Length)
            {
                throw new ArgumentException();
            }

            array[index++] = _arg0;
            for (var i = 1; i < n; i++)
            {
                array[index++] = _block.GetExpression(i);
            }
        }

        public bool Remove(Expression item)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion ICollection<Expression> Members

        #region IEnumerable<Expression> Members

        public IEnumerator<Expression> GetEnumerator()
        {
            yield return _arg0;

            for (var i = 1; i < _block.ExpressionCount; i++)
            {
                yield return _block.GetExpression(i);
            }
        }

        #endregion IEnumerable<Expression> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable Members
    }

    #endregion Block List Classes

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains two expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1)
        {
            ExpressionUtils.RequiresCanRead(arg0, nameof(arg0));
            ExpressionUtils.RequiresCanRead(arg1, nameof(arg1));

            return new Block2(arg0, arg1);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains three expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <param name="arg2">The third expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2)
        {
            ExpressionUtils.RequiresCanRead(arg0, nameof(arg0));
            ExpressionUtils.RequiresCanRead(arg1, nameof(arg1));
            ExpressionUtils.RequiresCanRead(arg2, nameof(arg2));
            return new Block3(arg0, arg1, arg2);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains four expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <param name="arg2">The third expression in the block.</param>
        /// <param name="arg3">The fourth expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            ExpressionUtils.RequiresCanRead(arg0, nameof(arg0));
            ExpressionUtils.RequiresCanRead(arg1, nameof(arg1));
            ExpressionUtils.RequiresCanRead(arg2, nameof(arg2));
            ExpressionUtils.RequiresCanRead(arg3, nameof(arg3));
            return new Block4(arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains five expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <param name="arg2">The third expression in the block.</param>
        /// <param name="arg3">The fourth expression in the block.</param>
        /// <param name="arg4">The fifth expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            ExpressionUtils.RequiresCanRead(arg0, nameof(arg0));
            ExpressionUtils.RequiresCanRead(arg1, nameof(arg1));
            ExpressionUtils.RequiresCanRead(arg2, nameof(arg2));
            ExpressionUtils.RequiresCanRead(arg3, nameof(arg3));
            ExpressionUtils.RequiresCanRead(arg4, nameof(arg4));

            return new Block5(arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions and has no variables.
        /// </summary>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(params Expression[] expressions)
        {
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));
            RequiresCanRead(expressions, nameof(expressions));

            return GetOptimizedBlockExpression(expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions and has no variables.
        /// </summary>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(IEnumerable<Expression> expressions)
        {
            return Block(EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions, has no variables and has specific result type.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, params Expression[] expressions)
        {
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));
            return Block(type, (IEnumerable<Expression>)expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions, has no variables and has specific result type.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, IEnumerable<Expression> expressions)
        {
            return Block(type, EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(IEnumerable<ParameterExpression> variables, params Expression[] expressions)
        {
            return Block(variables, (IEnumerable<Expression>)expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, params Expression[] expressions)
        {
            return Block(type, variables, (IEnumerable<Expression>)expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
        {
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));
            var variableList = variables.ToTrueReadOnly();

            var expressionList = Theraot.Collections.Extensions.AsArray(expressions);
            RequiresCanRead(expressionList, nameof(expressions));
            if (variableList.Count == 0)
            {
                return GetOptimizedBlockExpression(expressionList);
            }

            return BlockCore(null, Theraot.Collections.Extensions.AsArray(variableList), expressionList);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));

            var expressionList = Theraot.Collections.Extensions.AsArray(expressions);
            RequiresCanRead(expressionList, nameof(expressions));

            var variableList = Theraot.Collections.Extensions.AsArray(variables);

            if (variableList.Length == 0 && expressionList.Length != 0)
            {
                var expressionCount = expressionList.Length;

                if (expressionCount != 0)
                {
                    var lastExpression = expressionList[expressionCount - 1];

                    if (lastExpression.Type == type)
                    {
                        return GetOptimizedBlockExpression(expressionList);
                    }
                }
            }

            return BlockCore(type, variableList, expressionList);
        }

        // Checks that all variables are non-null, not byref, and unique.
        internal static void ValidateVariables(ParameterExpression[] varList, string collectionName)
        {
            var count = varList.Length;
            if (count != 0)
            {
                var set = new HashSet<ParameterExpression>();
                for (var i = 0; i < count; i++)
                {
                    var v = varList[i];
                    ContractUtils.RequiresNotNull(v, collectionName, i);
                    if (v.IsByRef)
                    {
                        throw Error.VariableMustNotBeByRef(v, v.Type, collectionName, i);
                    }
                    if (!set.Add(v))
                    {
                        throw Error.DuplicateVariable(v, collectionName, i);
                    }
                }
            }
        }

        private static BlockExpression BlockCore(Type type, ParameterExpression[] variables, Expression[] expressions)
        {
            ValidateVariables(variables, nameof(variables));

            if (type != null)
            {
                if (expressions.Length == 0)
                {
                    if (type != typeof(void))
                    {
                        throw Error.ArgumentTypesMustMatch();
                    }

                    return new ScopeWithType(variables, expressions, type);
                }
                var last = expressions.Last();
                if (type != typeof(void))
                {
                    if (!type.IsReferenceAssignableFromInternal(last.Type))
                    {
                        throw Error.ArgumentTypesMustMatch();
                    }
                }
                if (type != last.Type)
                {
                    return new ScopeWithType(variables, expressions, type);
                }
            }

            switch (expressions.Length)
            {
                case 0:
                    return new ScopeWithType(variables, expressions, typeof(void));

                case 1:
                    return new Scope1(variables, expressions[0]);

                default:
                    return new ScopeN(variables, expressions);
            }
        }

        private static BlockExpression GetOptimizedBlockExpression(Expression[] expressions)
        {
            switch (expressions.Length)
            {
                case 0: return BlockCore(typeof(void), ArrayReservoir<ParameterExpression>.EmptyArray, ArrayReservoir<Expression>.EmptyArray);
                case 2: return new Block2(expressions[0], expressions[1]);
                case 3: return new Block3(expressions[0], expressions[1], expressions[2]);
                case 4: return new Block4(expressions[0], expressions[1], expressions[2], expressions[3]);
                case 5: return new Block5(expressions[0], expressions[1], expressions[2], expressions[3], expressions[4]);
                default:
                    return new BlockN(expressions);
            }
        }
    }
}

#endif