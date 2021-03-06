// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Query.ExpressionVisitors;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Microsoft.Data.Entity.Relational.Query.ExpressionVisitors
{
    public class RelationalProjectionExpressionVisitor : ProjectionExpressionVisitor
    {
        private readonly IQuerySource _querySource;

        private readonly SqlTranslatingExpressionVisitor _sqlTranslatingExpressionVisitor;

        private bool _requiresClientEval;

        public RelationalProjectionExpressionVisitor(
            [NotNull] RelationalQueryModelVisitor queryModelVisitor,
            [NotNull] IQuerySource querySource)
            : base(Check.NotNull(queryModelVisitor, nameof(queryModelVisitor)))
        {
            _querySource = querySource;

            _sqlTranslatingExpressionVisitor
                = new SqlTranslatingExpressionVisitor(queryModelVisitor);
        }

        private new RelationalQueryModelVisitor QueryModelVisitor
            => (RelationalQueryModelVisitor)base.QueryModelVisitor;

        public virtual bool RequiresClientEval => _requiresClientEval;

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsGenericMethod)
            {
                var methodInfo = methodCallExpression.Method.GetGenericMethodDefinition();

                if (ReferenceEquals(methodInfo, QueryExtensions.PropertyMethodInfo)
                    || ReferenceEquals(methodInfo, QueryExtensions.ValueBufferPropertyMethodInfo))
                {
                    var newArg0 = Visit(methodCallExpression.Arguments[0]);

                    if (newArg0 != methodCallExpression.Arguments[0])
                    {
                        return Expression.Call(
                            methodCallExpression.Method,
                            newArg0,
                            methodCallExpression.Arguments[1]);
                    }

                    return methodCallExpression;
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        public override Expression Visit(Expression expression)
        {
            if (expression != null
                && !(expression is QuerySourceReferenceExpression)
                && !(expression is ConstantExpression))
            {
                var sqlExpression
                    = _sqlTranslatingExpressionVisitor.Visit(expression);

                if (sqlExpression == null)
                {
                    _requiresClientEval = true;
                }
                else
                {
                    var selectExpression
                        = QueryModelVisitor.TryGetQuery(_querySource);

                    Debug.Assert(selectExpression != null);

                    if (!(expression is NewExpression))
                    {
                        var columnExpression = sqlExpression.TryGetColumnExpression();

                        if (columnExpression != null)
                        {
                            selectExpression.AddToProjection(columnExpression);

                            return expression;
                        }

                        var index = selectExpression.AddToProjection(sqlExpression);

                        return
                            QueryModelVisitor.BindReadValueMethod(
                                expression.Type,
                                QuerySourceScope.GetResult(
                                    EntityQueryModelVisitor.QuerySourceScopeParameter,
                                    _querySource,
                                    typeof(ValueBuffer)),
                                index);
                    }
                }
            }

            return base.Visit(expression);
        }
    }
}
