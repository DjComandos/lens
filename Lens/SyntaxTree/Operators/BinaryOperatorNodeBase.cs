﻿using System;
using System.Collections.Generic;
using Lens.Compiler;
using Lens.Translations;
using Lens.Utils;

namespace Lens.SyntaxTree.Operators
{
	/// <summary>
	/// The base for all binary operators.
	/// </summary>
	internal abstract class BinaryOperatorNodeBase : OperatorNodeBase
	{
		/// <summary>
		/// The operand to the left side.
		/// </summary>
		public NodeBase LeftOperand { get; set; }
		
		/// <summary>
		/// The operand to the right side.
		/// </summary>
		public NodeBase RightOperand { get; set; }

		/// <summary>
		/// Checks if numeric type casting checks should be applied to operands.
		/// </summary>
		protected virtual bool IsNumericOperator { get { return true; }}

		protected override Type resolveExpressionType(Context ctx, bool mustReturn = true)
		{
			var leftType = LeftOperand.GetExpressionType(ctx);
			var rightType = RightOperand.GetExpressionType(ctx);

			var result = resolveOperatorType(ctx, leftType, rightType);
			if (result != null)
				return result;

			if (IsNumericOperator)
			{
				if (leftType.IsNumericType() && rightType.IsNumericType())
					return resolveNumericType(ctx);
			}

			if (OverloadedMethodName != null)
			{
				try
				{
					m_OverloadedMethod = ctx.ResolveMethod(leftType, OverloadedMethodName, new[] { leftType, rightType });

					// cannot be generic
					if (m_OverloadedMethod != null)
						return m_OverloadedMethod.ReturnType;
				}
				catch { }
			}

			Error(this, CompilerMessages.OperatorBinaryTypesMismatch, OperatorRepresentation, leftType, rightType);
			return null;
		}

		protected override void compile(Context ctx, bool mustReturn)
		{
			var gen = ctx.CurrentILGenerator;

			GetExpressionType(ctx);

			if (m_OverloadedMethod == null)
			{
				compileOperator(ctx);
				return;
			}

			var ps = m_OverloadedMethod.ArgumentTypes;
			Expr.Cast(LeftOperand, ps[0]).Compile(ctx, true);
			Expr.Cast(RightOperand, ps[1]).Compile(ctx, true);
			gen.EmitCall(m_OverloadedMethod.MethodInfo);
		}

		protected virtual Type resolveOperatorType(Context ctx, Type leftType, Type rightType)
		{
			return null;
		}

		protected abstract void compileOperator(Context ctx);

		protected abstract dynamic unrollConstant(dynamic left, dynamic right);

		#region Helper methods

		/// <summary>
		/// Resolves a common numeric type.
		/// </summary>
		private Type resolveNumericType(Context ctx)
		{
			var left = LeftOperand.GetExpressionType(ctx);
			var right = RightOperand.GetExpressionType(ctx);

			var type = TypeExtensions.GetNumericOperationType(left, right);
			if(type == null)
				Error(CompilerMessages.OperatorTypesSignednessMismatch);

			return type;
		}

		/// <summary>
		/// Loads both arguments and converts them to the biggest common type.
		/// </summary>
		protected void loadAndConvertNumerics(Context ctx, Type type = null)
		{
			var gen = ctx.CurrentILGenerator;

			var left = LeftOperand.GetExpressionType(ctx);
			var right = RightOperand.GetExpressionType(ctx);

			if(type == null)
				type = TypeExtensions.GetNumericOperationType(left, right);

			LeftOperand.Compile(ctx, true);
			if (left != type)
				gen.EmitConvert(type);

			RightOperand.Compile(ctx, true);
			if (right != type)
				gen.EmitConvert(type);
		}

		#endregion

		#region NodeBase overrides

		public override bool IsConstant { get { return RightOperand.IsConstant && LeftOperand.IsConstant; } }
		public override object ConstantValue { get { return unrollConstant(LeftOperand.ConstantValue, RightOperand.ConstantValue); } }

		public override IEnumerable<NodeBase> GetChildNodes()
		{
			yield return LeftOperand;
			yield return RightOperand;
		}

		#endregion

		#region Equality members

		protected bool Equals(BinaryOperatorNodeBase other)
		{
			return Equals(LeftOperand, other.LeftOperand) && Equals(RightOperand, other.RightOperand);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((BinaryOperatorNodeBase)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((LeftOperand != null ? LeftOperand.GetHashCode() : 0) * 397) ^ (RightOperand != null ? RightOperand.GetHashCode() : 0);
			}
		}

		#endregion

		public override string ToString()
		{
			return string.Format("op{0}({1}, {2})", OperatorRepresentation, LeftOperand, RightOperand);
		}
	}
}
