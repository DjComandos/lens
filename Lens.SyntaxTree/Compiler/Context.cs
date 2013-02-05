﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Lens.SyntaxTree.SyntaxTree;
using Lens.SyntaxTree.SyntaxTree.ControlFlow;
using Lens.SyntaxTree.Utils;

namespace Lens.SyntaxTree.Compiler
{
	/// <summary>
	/// The main context class that stores information about currently compiled Assembly.
	/// </summary>
	public partial class Context
	{
		#region Constants

		/// <summary>
		/// The name of the main type in the assembly.
		/// </summary>
		private const string RootTypeName = "<ScriptRootType>";

		/// <summary>
		/// The name of the entry point of the assembly.
		/// </summary>
		private const string RootMethodName = "<ScriptBody>";

		/// <summary>
		/// The name for the application domain to execute script in.
		/// </summary>
		private const string AppDomainName = "LensScriptAppDomain";

		#endregion

		private Context()
		{
			var an = new AssemblyName(getAssemblyName());

			Domain = AppDomain.CreateDomain(AppDomainName);
			MainAssembly = Domain.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
			MainModule = MainAssembly.DefineDynamicModule(an.Name, an.Name + ".dll");

			_TypeResolver = new TypeResolver();
			_DefinedTypes = new Dictionary<string, TypeEntity>();

			declareRootType();
		}

		/// <summary>
		/// Creates the context from a stream of nodes.
		/// </summary>
		/// <param name="nodes">Stream of nodes generated by the parser.</param>
		public static Context CreateFromNodes(IEnumerable<NodeBase> nodes)
		{
			var ctx = new Context();
			foreach (var currNode in nodes)
			{
				if (currNode is TypeDefinitionNode)
					ctx.DeclareType(currNode as TypeDefinitionNode);
				else if (currNode is RecordDefinitionNode)
					ctx.DeclareRecord(currNode as RecordDefinitionNode);
				else if (currNode is FunctionNode)
					ctx.DeclareFunction(currNode as FunctionNode);
				else if(currNode is UsingNode)
					ctx.DeclareOpenNamespace(currNode as UsingNode);
				else
					ctx.DeclareScriptNode(currNode);
			}

			return ctx;
		}

		/// <summary>
		/// Compiles the assembly.
		/// </summary>
		public void Compile()
		{
			// todo
			prepareEntities();
			processClosures();
			prepareEntities();
			compileInternal();
			finalizeAssembly();

			IsCompiled = true;
		}

		/// <summary>
		/// Execute the script and get it's return value.
		/// </summary>
		public object Execute()
		{
			if (!IsCompiled)
				Compile();

			var method = ResolveMethod(RootTypeName, RootMethodName);
			return method.Invoke(null, new object[0]);
		}

		/// <summary>
		/// Throws a new error.
		/// </summary>
		[ContractAnnotation("=> halt")]
		public void Error(string msg, params object[] args)
		{
			throw new LensCompilerException(string.Format(msg, args));
		}

		#region Properties

		/// <summary>
		/// The application domain for current script.
		/// </summary>
		public AppDomain Domain { get; private set; }

		/// <summary>
		/// The assembly that's being currently built.
		/// </summary>
		public AssemblyBuilder MainAssembly { get; private set; }

		/// <summary>
		/// The main module of the current assembly.
		/// </summary>
		public ModuleBuilder MainModule { get; private set; }

		/// <summary>
		/// Main type of the assembly.
		/// </summary>
		public TypeBuilder MainType
		{
			get
			{
				if (_MainType == null)
					_MainType = ResolveType(RootTypeName) as TypeBuilder;
				return _MainType;
			}
		}
		private TypeBuilder _MainType;

		/// <summary>
		/// Checks if the source in this context has been compiled.
		/// </summary>
		public bool IsCompiled { get; private set; }

		/// <summary>
		/// Type that is currently processed.
		/// </summary>
		internal TypeEntity CurrentType { get; set; }

		/// <summary>
		/// Method that is currently processed.
		/// </summary>
		internal MethodEntityBase CurrentMethod { get; set; }

		/// <summary>
		/// The lexical scope of the current scope.
		/// </summary>
		internal Scope CurrentScope { get { return CurrentMethod == null ? null : CurrentMethod.Scope; } }

		/// <summary>
		/// An ID for closure types.
		/// </summary>
		internal int ClosureId;

		#endregion

		#region Fields

		/// <summary>
		/// The counter that allows multiple assemblies.
		/// </summary>
		private static int _AssemblyId;

		/// <summary>
		/// A helper that resolves built-in .NET types by their string signatures.
		/// </summary>
		private readonly TypeResolver _TypeResolver;

		/// <summary>
		/// The root of type lookup.
		/// </summary>
		private readonly Dictionary<string, TypeEntity> _DefinedTypes;

		/// <summary>
		/// The function that is the body of the script.
		/// </summary>
		private MethodEntity _ScriptBody;

		#endregion
	}
}
