﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lens.Compiler;
using Lens.Translations;
using Lens.Utils;

namespace Lens.SyntaxTree
{
	/// <summary>
	/// The base class for all syntax tree nodes.
	/// </summary>
	internal abstract class NodeBase : LocationEntity
	{
		/// <summary>
		/// Checks if the current node is a constant.
		/// </summary>
		public virtual bool IsConstant { get { return false; } }

		public virtual dynamic ConstantValue { get { throw new InvalidOperationException("Not a constant!"); } }

		/// <summary>
		/// The cached expression type.
		/// </summary>
		private Type m_ExpressionType;

		/// <summary>
		/// Calculates the type of expression represented by current node.
		/// </summary>
		[DebuggerStepThrough]
		public Type GetExpressionType(Context ctx, bool mustReturn = true)
		{
			if (m_ExpressionType == null)
			{
				m_ExpressionType = resolveExpressionType(ctx, mustReturn);
				SafeModeCheckType(ctx, m_ExpressionType);
			}

			return m_ExpressionType;
		}

		protected virtual Type resolveExpressionType(Context ctx, bool mustReturn = true)
		{
			return typeof (Unit);
		}

		/// <summary>
		/// Generates the IL for this node.
		/// </summary>
		/// <param name="ctx">Pointer to current context.</param>
		/// <param name="mustReturn">Flag indicating the node should return a value.</param>
		[DebuggerStepThrough]
		public void Compile(Context ctx, bool mustReturn)
		{
			GetExpressionType(ctx, mustReturn);

			if (IsConstant && ctx.Options.UnrollConstants)
			{
				if(mustReturn)
					emitConstant(ctx);
			}
			else
			{
				compile(ctx, mustReturn);
			}
		}

		protected abstract void compile(Context ctx, bool mustReturn);

		/// <summary>
		/// Emit the value of current node as a constant.
		/// </summary>
		private void emitConstant(Context ctx)
		{
			var gen = ctx.CurrentILGenerator;
			var value = ConstantValue;

			if (value is bool)
				gen.EmitConstant((bool)value);
			else if (value is int)
				gen.EmitConstant((int)value);
			else if (value is long)
				gen.EmitConstant((long)value);
			else if (value is double)
				gen.EmitConstant((double)value);
			else if (value is string)
				gen.EmitConstant((string)value);
			else
				compile(ctx, true);
		}

		/// <summary>
		/// Gets the list of child nodes.
		/// </summary>
		public virtual IEnumerable<NodeBase> GetChildNodes()
		{
			return new NodeBase[0];
		}

		/// <summary>
		/// Processes closures.
		/// </summary>
		public virtual void ProcessClosures(Context ctx)
		{
			foreach(var child in GetChildNodes())
				if(child != null)
					child.ProcessClosures(ctx);
		}

		/// <summary>
		/// Reports an error to the compiler.
		/// </summary>
		/// <param name="message">Error message.</param>
		/// <param name="args">Optional error arguments.</param>
		[ContractAnnotation("=> halt")]
		[DebuggerStepThrough]
		public void Error(string message, params object[] args)
		{
			Error(this, message, args);
		}

		/// <summary>
		/// Reports an error to the compiler.
		/// </summary>
		/// <param name="message">Error message.</param>
		/// <param name="args">Optional error arguments.</param>
		[ContractAnnotation("=> halt")]
		[DebuggerStepThrough]
		public void Error(LocationEntity entity, string message, params object[] args)
		{
			var msg = string.Format(message, args);
			throw new LensCompilerException(msg, entity);
		}

		/// <summary>
		/// Throws an error that the current type is not alowed in safe mode.
		/// </summary>
		protected void SafeModeCheckType(Context ctx, Type type)
		{
			if(!ctx.IsTypeAllowed(type))
				Error(CompilerMessages.SafeModeIllegalType, type.FullName);
		}
	}
}
