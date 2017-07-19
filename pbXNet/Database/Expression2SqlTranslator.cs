﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace pbXNet.Database
{
	public class Expression2SqlTranslator : IExpressionTranslator
	{
		protected static readonly MethodInfo _methodStartsWith = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), new[] { typeof(string) });
		protected static readonly MethodInfo _methodEndsWith = typeof(string).GetRuntimeMethod(nameof(string.EndsWith), new[] { typeof(string) });

		public List<(string name, object value)> Parameters => _parameters.Value;

		Lazy<List<(string name, object value)>> _parameters = new Lazy<List<(string, object)>>(() => new List<(string, object)>(), true);

		protected Type _typeForWhichMemberNamesWillBeEmitted;

		protected SqlBuilder _sqlBuilder;

		protected class Result
		{
			public string expr;
			public object value;
			public string name;

#if DEBUG
			public override string ToString()
			{
				return $"[{expr}],[{value}],[{name}]";
			}
#endif
		}

		public Expression2SqlTranslator(SqlBuilder sqlBuilder, Type typeForWhichMemberNamesWillBeEmitted = null)
		{
			Check.Null(sqlBuilder, nameof(sqlBuilder));

			_sqlBuilder = sqlBuilder;
			_typeForWhichMemberNamesWillBeEmitted = typeForWhichMemberNamesWillBeEmitted;
		}

		public IExpressionTranslator New(Type typeForWhichMemberNamesWillBeEmitted = null)
		{
			return new Expression2SqlTranslator(_sqlBuilder.New(), typeForWhichMemberNamesWillBeEmitted);
		}

		protected virtual Result TranslateExpr(ConstantExpression expr)
		{
			if (expr == null)
				return null;

			Result r = new Result
			{
				value = expr.Value,
			};

			if (r.value != null)
			{
				r.name = $"_{Parameters.Count + 1}";
				r.expr = $"{_sqlBuilder.ParameterPrefix}{r.name}";

				Parameters.Add((r.name, r.value));
			}

			return r;
		}

		protected virtual Result TranslateExpr(UnaryExpression expr)
		{
			if (expr == null)
				return null;

			if (expr.NodeType == ExpressionType.Not)
			{
				Result operand = TranslateExpr(expr.Operand);
				if (operand != null && operand.expr != null)
				{
					return new Result
					{
						expr = _sqlBuilder.Expr().Not.Text(operand.expr),
					};
				}
			}

			return null;
		}

		protected virtual Result TranslateExpr(MemberExpression expr)
		{
			if (expr == null)
				return null;

			if (expr.NodeType == ExpressionType.MemberAccess && expr.Member is MemberInfo mi)
			{
				if (mi.DeclaringType == _typeForWhichMemberNamesWillBeEmitted)
				{
					return new Result
					{
						expr = mi.Name,
						name = mi.Name,
					};
				}

				if (expr.Expression is ConstantExpression ce)
				{
					object value = null;
					if (mi is PropertyInfo pi)
						value = pi.GetValue(ce.Value);
					else if (mi is FieldInfo fi)
						value = fi.GetValue(ce.Value);
					else
						return null;

					Result r = new Result
					{
						value = value,
					};

					if (value != null)
					{
						r.name = mi.Name;
						r.expr = $"{_sqlBuilder.ParameterPrefix}{r.name}";

						Parameters.Add((r.name, r.value));
					}

					return r;
				}
			}

			return TranslateExpr(expr.Expression);
		}

		protected virtual Result TranslateExpr(MethodCallExpression expr)
		{
			if (expr == null)
				return null;

			if (Equals(expr.Method, _methodStartsWith) || Equals(expr.Method, _methodEndsWith))
			{
				Result left = TranslateExpr(expr.Object);
				if (left == null)
					return null;

				Result right = TranslateExpr(expr.Arguments[0]);
				if (right == null)
					return null;

				SqlBuilder _expr = _sqlBuilder.Expr().Ob().Text(left.expr).Like.Ob();

				if (Equals(expr.Method, _methodStartsWith))
					_expr.E(right.expr).Concat.Text("'%'"); // TODO: % do SqlBuilder
				else
					_expr.Text("'%'").Concat.E(right.expr);

				return new Result
				{
					expr = _expr.Cb().Cb(),
				};
			}

			return null;
		}

		protected virtual Result TranslateExpr(BinaryExpression expr)
		{
			if (expr == null)
				return null;

			Result left = TranslateExpr(expr.Left);
			if (left == null)
				return null;

			Result right = TranslateExpr(expr.Right);
			if (right == null)
				return null;

			bool rightCanBeNull = false;
			string oper = null;

			switch (expr.NodeType)
			{
				case ExpressionType.AndAlso:
					oper = _sqlBuilder.Expr().And();
					break;

				case ExpressionType.OrElse:
					oper = _sqlBuilder.Expr().Or();
					break;


				case ExpressionType.Equal:
					if (left.value == null && left.expr == null)
					{
						// handle: null == [expr]
						oper = _sqlBuilder.Expr().Is.Null();
						Tools.Swap(ref right, ref left);
						rightCanBeNull = true;
					}
					else if (right.value == null && right.expr == null)
					{
						// handle: [expr] == null
						oper = _sqlBuilder.Expr().Is.Null();
						rightCanBeNull = true;
					}
					else
						oper = _sqlBuilder.Expr().Eq;
					break;

				case ExpressionType.NotEqual:
					if (left.value == null && left.expr == null)
					{
						// handle: null != [expr]
						oper = _sqlBuilder.Expr().Is.NotNull();
						Tools.Swap(ref right, ref left);
						rightCanBeNull = true;
					}
					else if (right.value == null && right.expr == null)
					{
						// handle: [expr] != null
						oper = _sqlBuilder.Expr().Is.NotNull();
						rightCanBeNull = true;
					}
					else
						oper = _sqlBuilder.Expr().Neq;
					break;

				case ExpressionType.GreaterThan:
					oper = _sqlBuilder.Expr().Gt;
					break;

				case ExpressionType.GreaterThanOrEqual:
					oper = _sqlBuilder.Expr().GtEq;
					break;

				case ExpressionType.LessThan:
					oper = _sqlBuilder.Expr().Lt;
					break;

				case ExpressionType.LessThanOrEqual:
					oper = _sqlBuilder.Expr().LtEq;
					break;


				case ExpressionType.And:
					oper = _sqlBuilder.Expr().BitwiseAnd;
					break;

				case ExpressionType.Or:
					oper = _sqlBuilder.Expr().BitwiseOr;
					break;

				case ExpressionType.Add:
					if (expr.Left.Type == typeof(string))
						oper = _sqlBuilder.Expr().Concat;
					else
						oper = _sqlBuilder.Expr().Plus;
					break;

				case ExpressionType.Subtract:
					oper = _sqlBuilder.Expr().Minus;
					break;

				case ExpressionType.Multiply:
					oper = _sqlBuilder.Expr().Multiply;
					break;

				case ExpressionType.Divide:
					oper = _sqlBuilder.Expr().Divide;
					break;
			}

			if (oper != null && left.expr != null && (rightCanBeNull || right.expr != null))
			{
				return new Result
				{
					expr = _sqlBuilder.Expr().Ob().Text(left.expr).Text(oper).Text(right.expr).Cb(),
				};
			}

			return null;
		}

		protected virtual Result TranslateExpr(Expression expr)
		{
			if (expr.NodeType == ExpressionType.Lambda && expr is LambdaExpression e)
				return TranslateExpr(e.Body);
			else if (expr is BinaryExpression be)
				return TranslateExpr(be);
			else if (expr is MemberExpression me)
				return TranslateExpr(me);
			else if (expr is ConstantExpression ce)
				return TranslateExpr(ce);
			else if (expr is UnaryExpression ue)
				return TranslateExpr(ue);
			else if (expr is MethodCallExpression mce)
				return TranslateExpr(mce);
			return null;
		}

		public string Translate(Expression expr)
		{
			Check.Null(expr, nameof(expr));

			Result r = TranslateExpr(expr);
			return (r != null && r.expr != null) ? r.expr : null;
		}
	}
}
