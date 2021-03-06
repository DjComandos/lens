﻿using System;
using System.Reflection.Emit;
using Lens.Compiler;
using Lens.SyntaxTree.Literals;
using Lens.Translations;
using Lens.Utils;

namespace Lens.SyntaxTree.Operators
{
	/// <summary>
	/// A node representing object comparison operations.
	/// </summary>
	internal class ComparisonOperatorNode : BinaryOperatorNodeBase
	{
		public ComparisonOperatorNode(ComparisonOperatorKind kind = default(ComparisonOperatorKind))
		{
			Kind = kind;
		}

		/// <summary>
		/// The kind of equality operator.
		/// </summary>
		public ComparisonOperatorKind Kind { get; set; }

		public override string OperatorRepresentation
		{
			get
			{
				switch (Kind)
				{
					case ComparisonOperatorKind.Equals:			return "==";
					case ComparisonOperatorKind.NotEquals:		return "<>";
					case ComparisonOperatorKind.Less:			return "<";
					case ComparisonOperatorKind.LessEquals:		return "<=";
					case ComparisonOperatorKind.Greater:		return ">";
					case ComparisonOperatorKind.GreaterEquals:	return ">=";

					default: throw new ArgumentException("Comparison operator kind is invalid!");
				}
			}
		}

		public override string  OverloadedMethodName
		{
			get
			{
				switch (Kind)
				{
					case ComparisonOperatorKind.Equals: return "op_Equality";
					case ComparisonOperatorKind.NotEquals: return "op_Inequality";
					case ComparisonOperatorKind.Less: return "op_LessThan";
					case ComparisonOperatorKind.LessEquals: return "op_LessThanOrEqual";
					case ComparisonOperatorKind.Greater: return "op_GreaterThan";
					case ComparisonOperatorKind.GreaterEquals: return "op_GreaterThanOrEqual";

					default: throw new ArgumentException("Comparison operator kind is invalid!");
				}
			}
		}

		protected override Type  resolveOperatorType(Context ctx, Type leftType, Type rightType)
		{
			var isEquality = Kind == ComparisonOperatorKind.Equals || Kind == ComparisonOperatorKind.NotEquals;
			return canCompare(leftType, rightType, isEquality) ? typeof (bool) : null;
		}

		protected override void compileOperator(Context ctx)
		{
			var leftType = LeftOperand.GetExpressionType(ctx);
			var rightType = RightOperand.GetExpressionType(ctx);

			var isEquality = Kind == ComparisonOperatorKind.Equals || Kind == ComparisonOperatorKind.NotEquals;

			if(!canCompare(leftType, rightType, isEquality))
				Error(CompilerMessages.TypesIncomparable, leftType, rightType);

			if (isEquality)
				compileEquality(ctx, leftType, rightType);
			else
				compileRelation(ctx, leftType, rightType);
		}

		/// <summary>
		/// Checks if two types can be compared.
		/// </summary>
		private bool canCompare(Type left, Type right, bool equalityOnly)
		{
			// there's an overridden method
			if (m_OverloadedMethod != null)
				return true;

			// string .. string
			if (left == typeof(string) && right == left)
				return true;

			// numeric .. numeric
			if (left.IsNumericType() && right.IsNumericType())
				return left.IsUnsignedIntegerType() == right.IsUnsignedIntegerType();

			if (equalityOnly)
			{
				// Nullable<T> .. (Nullable<T> | T | null)
				if (left.IsNullableType())
					return left == right || Nullable.GetUnderlyingType(left) == right || right == typeof (NullType);

				if (right.IsNullableType())
					return Nullable.GetUnderlyingType(right) == left || left == typeof (NullType);

				// ref type .. null
				if ((right == typeof (NullType) && !left.IsValueType) || (left == typeof (NullType) && !right.IsValueType))
					return true;

				if (left is TypeBuilder && left == right)
					return true;

			}

			return false;
		}

		/// <summary>
		/// Emits code for equality and inequality comparison.
		/// </summary>
		private void compileEquality(Context ctx, Type left, Type right)
		{
			var gen = ctx.CurrentILGenerator;

			// compare two strings
			if (left == typeof (string) && right == typeof (string))
			{
				LeftOperand.Compile(ctx, true);
				RightOperand.Compile(ctx, true);

				var method = typeof (string).GetMethod("Equals", new[] {typeof (string), typeof (string)});
				gen.EmitCall(method);

				if (Kind == ComparisonOperatorKind.NotEquals)
					emitInversion(gen);

				return;
			}

			// compare two numerics
			if (left.IsNumericType() && right.IsNumericType())
			{
				loadAndConvertNumerics(ctx);
				gen.EmitCompareEqual();

				if(Kind == ComparisonOperatorKind.NotEquals)
					emitInversion(gen);

				return;
			}

			// compare nullable against another nullable, it's base type or null
			if (left.IsNullableType())
			{
				if(left == right || Nullable.GetUnderlyingType(left) == right)
					compileNullable(ctx, LeftOperand, RightOperand);
				else if(right == typeof(NullType))
					compileHasValue(ctx, LeftOperand);

				return;
			}

			if (right.IsNullableType())
			{
				if (Nullable.GetUnderlyingType(right) == left)
					compileNullable(ctx, RightOperand, LeftOperand);
				else if (left == typeof(NullType))
					compileHasValue(ctx, RightOperand);

				return;
			}

			// compare a reftype against a null
			if (left == typeof(NullType) || right == typeof(NullType))
			{
				LeftOperand.Compile(ctx, true);
				RightOperand.Compile(ctx, true);
				gen.EmitCompareEqual();

				if (Kind == ComparisonOperatorKind.NotEquals)
					emitInversion(gen);

				return;
			}

			if (left is TypeBuilder && left == right)
			{
				var equals = ctx.ResolveMethod(left, "Equals", new [] { typeof (object) });

				LeftOperand.Compile(ctx, true);
				RightOperand.Compile(ctx, true);

				gen.EmitCall(equals.MethodInfo);

				if (Kind == ComparisonOperatorKind.NotEquals)
					emitInversion(gen);

				return;
			}
		}

		/// <summary>
		/// Emits code for comparing a nullable 
		/// </summary>
		private void compileNullable(Context ctx, NodeBase nullValue, NodeBase otherValue)
		{
			var gen = ctx.CurrentILGenerator;

			var nullType = nullValue.GetExpressionType(ctx);
			var otherType = otherValue.GetExpressionType(ctx);
			var otherNull = otherType.IsNullableType();

			var getValOrDefault = nullType.GetMethod("GetValueOrDefault", Type.EmptyTypes);
			var hasValueGetter = nullType.GetProperty("HasValue").GetGetMethod();

			var falseLabel = gen.DefineLabel();
			var endLabel = gen.DefineLabel();

			LocalName nullVar, otherVar = null;
			nullVar = ctx.CurrentScope.DeclareImplicitName(ctx, nullType, true);
			if (otherNull)
				otherVar = ctx.CurrentScope.DeclareImplicitName(ctx, otherType, true);
//			if (otherNull)
//			{
//				otherVar = ctx.CurrentScope.DeclareImplicitName(ctx, otherType, true);
//
//				var code = Expr.Block(
//					Expr.Let(nullVar, nullValue),
//					Expr.Let(otherVar, otherValue),
//					Expr.Binary(
//						Kind == ComparisonOperatorKind.Equals ? BooleanOperatorKind.And : BooleanOperatorKind.Or,
//						Expr.Compare(
//							Kind,
//							Expr.Invoke(Expr.GetIdentifier(nullVar), "GetValueOrDefault"),
//							Expr.Invoke(Expr.GetIdentifier(otherVar), "GetValueOrDefault")
//						),
//						Expr.Compare(
//							Kind,
//							Expr.Invoke(Expr.GetIdentifier(nullVar), "get_HasValue"),
//							Expr.Invoke(Expr.GetIdentifier(otherVar), "get_HasValue")
//						)
//					)
//				);
//
//				code.Compile(ctx, true);
//			}
//			else
//			{
//				var code = Expr.Block(
//					Expr.Let(nullVar, nullValue),
//					Expr.Binary(
//						Kind == ComparisonOperatorKind.Equals ? BooleanOperatorKind.And : BooleanOperatorKind.Or,
//						Expr.Compare(
//							Kind,
//							Expr.Invoke(Expr.GetIdentifier(nullVar), "GetValueOrDefault"),
//							Expr.Cast(otherValue, Nullable.GetUnderlyingType(nullType))
//						),
//						Expr.Invoke(Expr.GetIdentifier(nullVar), "get_HasValue")
//					)
//				);
//
//				code.Compile(ctx, true);
//			}
				

			// $tmp = nullValue
			nullValue.Compile(ctx, true);
			gen.EmitSaveLocal(nullVar);

			if (otherNull)
			{
				// $tmp2 = otherValue
				otherValue.Compile(ctx, true);
				gen.EmitSaveLocal(otherVar);
			}

			// $tmp == $tmp2
			gen.EmitLoadLocal(nullVar, true);
			gen.EmitCall(getValOrDefault);

			if (otherNull)
			{
				gen.EmitLoadLocal(otherVar, true);
				gen.EmitCall(getValOrDefault);
			}
			else
			{
				otherValue.Compile(ctx, true);
			}

			gen.EmitBranchNotEquals(falseLabel);

			// otherwise, compare HasValues
			gen.EmitLoadLocal(nullVar, true);
			gen.EmitCall(hasValueGetter);

			if (otherNull)
			{
				gen.EmitLoadLocal(otherVar, true);
				gen.EmitCall(hasValueGetter);

				gen.EmitCompareEqual();
			}

			if(Kind == ComparisonOperatorKind.NotEquals)
				emitInversion(gen);

			gen.EmitJump(endLabel);

			gen.MarkLabel(falseLabel);
			gen.EmitConstant(false);

			gen.MarkLabel(endLabel);
			gen.EmitNop();
		}

		/// <summary>
		/// Checks if the nullable expression is null.
		/// </summary>
		private void compileHasValue(Context ctx, NodeBase nullValue)
		{
			var gen = ctx.CurrentILGenerator;
			var nullType = nullValue.GetExpressionType(ctx);
			var nullVar = ctx.CurrentScope.DeclareImplicitName(ctx, nullType, true);
			var hasValueGetter = nullType.GetProperty("HasValue").GetGetMethod();

			nullValue.Compile(ctx, true);
			gen.EmitSaveLocal(nullVar);

			gen.EmitLoadLocal(nullVar, true);
			gen.EmitCall(hasValueGetter);

			// sic! get_HasValue == true when value != null
			if(Kind == ComparisonOperatorKind.Equals)
				emitInversion(gen);
		}

		/// <summary>
		/// Emits code for inverting the relation.
		/// </summary>
		private void emitInversion(ILGenerator gen)
		{
			gen.EmitConstant(false);
			gen.EmitCompareEqual();
		}

		/// <summary>
		/// Emits code for relation comparison: greater, less, etc.
		/// </summary>
		private void compileRelation(Context ctx, Type left, Type right)
		{
			var gen = ctx.CurrentILGenerator;

			// string comparisons
			if (left == typeof (string))
			{
				LeftOperand.Compile(ctx, true);
				RightOperand.Compile(ctx, true);

				var method = typeof (string).GetMethod("Compare", new[] {typeof (string), typeof (string)});
				gen.EmitCall(method);

				if (Kind.IsAnyOf(ComparisonOperatorKind.Less, ComparisonOperatorKind.GreaterEquals))
				{
					gen.EmitConstant(-1);
					gen.EmitCompareEqual();
					if (Kind == ComparisonOperatorKind.GreaterEquals)
						emitInversion(gen);
				}
				else
				{
					gen.EmitConstant(1);
					gen.EmitCompareEqual();
					if (Kind == ComparisonOperatorKind.LessEquals)
						emitInversion(gen);
				}
			}

			// numeric comparison
			loadAndConvertNumerics(ctx);
			if (Kind.IsAnyOf(ComparisonOperatorKind.Less, ComparisonOperatorKind.GreaterEquals))
			{
				gen.EmitCompareLess();
				if (Kind == ComparisonOperatorKind.GreaterEquals)
					emitInversion(gen);
			}
			else
			{
				gen.EmitCompareGreater();
				if (Kind == ComparisonOperatorKind.LessEquals)
					emitInversion(gen);
			}
		}

		protected override dynamic unrollConstant(dynamic left, dynamic right)
		{
			switch (Kind)
			{
				case ComparisonOperatorKind.Equals: return left == right;
				case ComparisonOperatorKind.NotEquals: return left != right;
				case ComparisonOperatorKind.Less: return left < right;
				case ComparisonOperatorKind.LessEquals: return left <= right;
				case ComparisonOperatorKind.Greater: return left > right;
				case ComparisonOperatorKind.GreaterEquals: return left >= right;
			}

			return null;
		}

		#region Equality members

		protected bool Equals(ComparisonOperatorNode other)
		{
			return base.Equals(other) && Kind == other.Kind;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ComparisonOperatorNode)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ (int)Kind;
			}
		}

		#endregion
	}

	/// <summary>
	/// The kind of comparison operators.
	/// </summary>
	public enum ComparisonOperatorKind
	{
		Equals,
		NotEquals,
		Less,
		LessEquals,
		Greater,
		GreaterEquals
	}
}
