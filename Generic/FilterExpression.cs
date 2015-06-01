using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CCWOnline.Management.EntityFramework
{
    public class FilterExpression<T>
    {
        public Expression<Func<T, bool>> Expression;

        public FilterExpression() { }

        protected FilterExpression(Expression<Func<T, bool>> expression)
        {
            this.Expression = expression;
        }

        public Func<T, bool> ResultExpression
        {
            get { return this.Expression.Compile(); }
        }

        public FilterExpression<T> Start(Expression<Func<T, bool>> expression)
        {
            return Start(expression, true);
        }

        public FilterExpression<T> Start(Expression<Func<T, bool>> expression, bool condition)
        {
            this.Expression = condition ? expression : null;
            return new FilterExpression<T>(this.Expression);
        }

        public FilterExpression<T> And(Expression<Func<T, bool>> expression)
        {
            return And(expression, true);
        }

        public FilterExpression<T> And(Expression<Func<T, bool>> expression, bool condition)
        {
            ParameterExpression parameter = expression.Parameters.Select(NewParameter).First();

            CombineExp(expression, condition, parameter, System.Linq.Expressions.Expression.AndAlso);

            return this;
        }

        public FilterExpression<T> Or(Expression<Func<T, bool>> expression)
        {
            return Or(expression, true);
        }

        public FilterExpression<T> Or(Expression<Func<T, bool>> expression, bool condition)
        {
            ParameterExpression parameter = expression.Parameters.Select(NewParameter).First();

            CombineExp(expression, condition, parameter, System.Linq.Expressions.Expression.Or);

            return this;
        }

        private void CombineExp(Expression<Func<T, bool>> expression, bool condition, ParameterExpression parameter, Func<Expression, Expression, BinaryExpression> exp)
        {
            if (condition)
                if (this.Expression != null)
                {
                    Expression left = Rebuild(this.Expression.Body, parameter);
                    Expression right = Rebuild(expression.Body, parameter);
                    this.Expression = (Expression<Func<T, bool>>)System.Linq.Expressions.Expression.Lambda(exp(left, right), parameter);
                }
                else if (this.Expression == null)
                    this.Expression = expression;
        }

        private Expression Rebuild(Expression body, ParameterExpression parameters)
        {
            var callExp = body as MethodCallExpression;
            if (callExp != null)
            {
                var arguments = callExp.Arguments.Select(expression => Rebuild(expression, parameters));
                return System.Linq.Expressions.Expression.Call(Rebuild(callExp.Object, parameters), callExp.Method, arguments);
            }

            var memberExp = body as MemberExpression;
            if (memberExp != null)
                return System.Linq.Expressions.Expression.Property(parameters, (PropertyInfo)memberExp.Member);

            var binExp = body as BinaryExpression;
            if (binExp != null)
                return System.Linq.Expressions.Expression.MakeBinary(binExp.NodeType,
                                             Rebuild(binExp.Left, parameters),
                                             Rebuild(binExp.Right, parameters));

            return body;
        }

        private ParameterExpression NewParameter(ParameterExpression parameterExpression)
        {
            return System.Linq.Expressions.Expression.Variable(parameterExpression.Type, parameterExpression.Name);
        }
    }
}