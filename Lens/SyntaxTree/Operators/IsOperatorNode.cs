﻿using System;
using Lens.Compiler;

namespace Lens.SyntaxTree.Operators
{
	/// <summary>
	/// Checks if the object is of given type.
	/// </summary>
	internal class IsOperatorNode : TypeCheckOperatorNodeBase
	{
		protected override Type resolveExpressionType(Context ctx, bool mustReturn = true)
		{
			return typeof (bool);
		}

		protected override void compile(Context ctx, bool mustReturn)
		{
			var gen = ctx.CurrentILGenerator;

			var exprType = Expression.GetExpressionType(ctx);
			var desiredType = ctx.ResolveType(TypeSignature);

			SafeModeCheckType(ctx, desiredType);

			// types are identical
			if (exprType == desiredType)
			{
				gen.EmitConstant(true);
				return;
			}

			// valuetype can only be cast to object
			if (exprType.IsValueType)
			{
				gen.EmitConstant(desiredType == typeof(object));
				return;
			}

			Expression.Compile(ctx, true);

			// check if not null
			if (desiredType == typeof (object))
			{
				gen.EmitNull();
				gen.EmitCompareEqual();
				gen.EmitConstant(false);
				gen.EmitCompareEqual();
			}
			else
			{
				gen.EmitCast(desiredType, false);
				gen.EmitNull();
				gen.EmitCompareGreater(false);
			}
		}
	}
}
