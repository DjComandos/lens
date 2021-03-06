﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lens.Compiler;
using Lens.SyntaxTree.ControlFlow;
using Lens.SyntaxTree.Expressions;
using Lens.SyntaxTree.Literals;
using Lens.SyntaxTree.Operators;

namespace Lens.SyntaxTree
{
	internal static class Expr
	{
		#region Constants
		
		public static IntNode Int(int value = 0)
		{
			return new IntNode {Value = value};
		}

		public static DoubleNode Double(double value = 0)
		{
			return new DoubleNode(value);
		}

		public static BooleanNode True()
		{
			return Bool(true);
		}

		public static BooleanNode False()
		{
			return Bool(false);
		}

		public static BooleanNode Bool(bool value)
		{
			return new BooleanNode(value);
		}

		public static StringNode Str(string value = null)
		{
			return new StringNode(value ?? string.Empty);
		}

		public static NullNode Null()
		{
			return new NullNode();
		}

		public static UnitNode Unit()
		{
			return new UnitNode();
		}

		public static ThisNode This()
		{
			return new ThisNode();
		}

		#endregion

		#region Operators

		public static AddOperatorNode Add(NodeBase left, NodeBase right)
		{
			return Op<AddOperatorNode>(left, right);
		}

		public static SubtractOperatorNode Sub(NodeBase left, NodeBase right)
		{
			return Op<SubtractOperatorNode>(left, right);
		}

		public static MultiplyOperatorNode Mult(NodeBase left, NodeBase right)
		{
			return Op<MultiplyOperatorNode>(left, right);
		}

		public static DivideOperatorNode Div(NodeBase left, NodeBase right)
		{
			return Op<DivideOperatorNode>(left, right);
		}

		public static RemainderOperatorNode Mod(NodeBase left, NodeBase right)
		{
			return Op<RemainderOperatorNode>(left, right);
		}

		public static PowOperatorNode Pow(NodeBase left, NodeBase right)
		{
			return Op<PowOperatorNode>(left, right);
		}

		public static ShiftOperatorNode ShiftLeft(NodeBase left, NodeBase right)
		{
			return new ShiftOperatorNode {LeftOperand = left, RightOperand = right, IsLeft = true};
		}

		public static ShiftOperatorNode ShiftRight(NodeBase left, NodeBase right)
		{
			return new ShiftOperatorNode { LeftOperand = left, RightOperand = right, IsLeft = false };
		}

		public static InversionOperatorNode Not(NodeBase node)
		{
			return new InversionOperatorNode {Operand = node};
		}

		public static NegationOperatorNode Negate(NodeBase node)
		{
			return new NegationOperatorNode {Operand = node};
		}

		public static DefaultOperatorNode Default(TypeSignature type)
		{
			return new DefaultOperatorNode {TypeSignature = type};
		}

		public static DefaultOperatorNode Default(Type type)
		{
			return new DefaultOperatorNode { Type = type };
		}

		public static TypeofOperatorNode Typeof(TypeSignature type)
		{
			return new TypeofOperatorNode {TypeSignature = type};
		}

		public static TypeofOperatorNode Typeof(Type type)
		{
			return new TypeofOperatorNode { Type = type };
		}

		public static CastOperatorNode Cast(NodeBase node, TypeSignature type)
		{
			return new CastOperatorNode {Expression = node, TypeSignature = type};
		}

		public static CastOperatorNode Cast(NodeBase node, Type type)
		{
			return new CastOperatorNode { Expression = node, Type = type };
		}

		public static IsOperatorNode Is(NodeBase node, TypeSignature type)
		{
			return new IsOperatorNode { Expression = node, TypeSignature = type };
		}

		public static IsOperatorNode Is(NodeBase node, Type type)
		{
			return new IsOperatorNode { Expression = node, Type = type };
		}

		public static BinaryOperatorNodeBase Binary(LogicalOperatorKind kind, NodeBase left, NodeBase right)
		{
			return new BooleanOperatorNode { Kind = kind, LeftOperand = left, RightOperand = right };
		}

		public static BinaryOperatorNodeBase And(NodeBase left, NodeBase right)
		{
			return new BooleanOperatorNode {LeftOperand = left, RightOperand = right};
		}

		public static BinaryOperatorNodeBase Or(NodeBase left, NodeBase right)
		{
			return new BooleanOperatorNode { Kind = LogicalOperatorKind.Or, LeftOperand = left, RightOperand = right };
		}

		public static BinaryOperatorNodeBase Xor(NodeBase left, NodeBase right)
		{
			return new BooleanOperatorNode { Kind = LogicalOperatorKind.Xor, LeftOperand = left, RightOperand = right };
		}

		public static ComparisonOperatorNode Compare(ComparisonOperatorKind kind, NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode { Kind = kind, LeftOperand = left, RightOperand = right };
		}

		public static ComparisonOperatorNode Equal(NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode {LeftOperand = left, RightOperand = right};
		}

		public static ComparisonOperatorNode NotEqual(NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode { Kind = ComparisonOperatorKind.NotEquals, LeftOperand = left, RightOperand = right };
		}

		public static ComparisonOperatorNode Less(NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode { Kind = ComparisonOperatorKind.Less, LeftOperand = left, RightOperand = right };
		}

		public static ComparisonOperatorNode LessEqual(NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode { Kind = ComparisonOperatorKind.LessEquals, LeftOperand = left, RightOperand = right };
		}

		public static ComparisonOperatorNode Greater(NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode { Kind = ComparisonOperatorKind.Greater, LeftOperand = left, RightOperand = right };
		}

		public static ComparisonOperatorNode GreaterEqual(NodeBase left, NodeBase right)
		{
			return new ComparisonOperatorNode { Kind = ComparisonOperatorKind.GreaterEquals, LeftOperand = left, RightOperand = right };
		}

		private static T Op<T>(NodeBase left, NodeBase right) where T : BinaryOperatorNodeBase, new()
		{
			return new T {LeftOperand = left, RightOperand = right};
		}

		#endregion

		#region Initializers

		public static NewArrayNode Array(params NodeBase[] items)
		{
			return new NewArrayNode {Expressions = items.ToList()};
		}

		public static NewTupleNode Tuple(params NodeBase[] items)
		{
			return new NewTupleNode { Expressions = items.ToList() };
		}

		public static NewListNode List(params NodeBase[] items)
		{
			return new NewListNode { Expressions = items.ToList() };
		}

		public static NewObjectNode New(TypeSignature type, params NodeBase[] args)
		{
			return new NewObjectNode
			{
				TypeSignature = type,
				Arguments = args.Length == 0 ? new List<NodeBase> { new UnitNode() } : args.ToList()
			};
		}

		public static NewObjectNode New(Type type, params NodeBase[] args)
		{
			return new NewObjectNode
			{
				Type = type,
				Arguments = args.Length == 0 ? new List<NodeBase> { new UnitNode() } : args.ToList()
			};
		}

		#endregion

		#region Expressions

		public static GetIndexNode GetIdx(NodeBase expr, NodeBase index)
		{
			return new GetIndexNode {Expression = expr, Index = index};
		}

		public static SetIndexNode SetIdx(NodeBase expr, NodeBase index, NodeBase value)
		{
			return new SetIndexNode { Expression = expr, Index = index, Value = value};
		}

		public static GetIdentifierNode Get(string name)
		{
			return new GetIdentifierNode { Identifier = name };
		}

		public static SetIdentifierNode Set(string name, NodeBase value)
		{
			return new SetIdentifierNode { Identifier = name, Value = value };
		}

		public static GetIdentifierNode Get(LocalName name)
		{
			return new GetIdentifierNode { LocalName = name };
		}

		public static SetIdentifierNode Set(LocalName name, NodeBase value)
		{
			return new SetIdentifierNode { LocalName = name, Value = value };
		}

		public static GetMemberNode GetMember(NodeBase expr, string name, params TypeSignature[] hints)
		{
			return new GetMemberNode { Expression = expr, MemberName = name, TypeHints = hints.ToList() };
		}

		public static GetMemberNode GetMember(TypeSignature type, string name, params TypeSignature[] hints)
		{
			return new GetMemberNode { StaticType = type, MemberName = name, TypeHints = hints.ToList() };
		}

		public static SetMemberNode SetMember(NodeBase expr, string name, NodeBase value)
		{
			return new SetMemberNode { Expression = expr, MemberName = name, Value = value };
		}

		public static SetMemberNode SetMember(TypeSignature type, string name, NodeBase value)
		{
			return new SetMemberNode { StaticType = type, MemberName = name, Value = value };
		}

		public static GetArgumentNode GetArg(int id)
		{
			return new GetArgumentNode {ArgumentId = id};
		}

		public static InvocationNode Invoke(TypeSignature type, string name, params NodeBase[] args)
		{
			return Invoke(GetMember(type, name), args);
		}

		public static InvocationNode Invoke(NodeBase expr, string name, params NodeBase[] args)
		{
			return Invoke(GetMember(expr, name), args);
		}

		public static InvocationNode Invoke(NodeBase expr, params NodeBase[] args)
		{
			return invoke(expr, args);
		}

		public static InvocationNode Invoke(string name, params NodeBase[] args)
		{
			return invoke(Get(name), args);
		}

		public static ShiftOperatorNode Compose(params NodeBase[] args)
		{
			if(args.Length < 2)
				throw new ArgumentException("At least 2 functions are required for composition!");

			var node = ShiftRight(args[0], args[1]);
			for (var idx = 2; idx < args.Length; idx++)
				node = ShiftRight(node, args[idx]);

			return node;
		}

		private static InvocationNode invoke(NodeBase expr, params NodeBase[] args)
		{
			return new InvocationNode
			{
				Expression = expr,
				Arguments = args.Length == 0 ? new List<NodeBase> { Unit() } : args.ToList()
			};
		}

		public static T Ref<T>(T expr) where T: NodeBase, IPointerProvider
		{
			expr.RefArgumentRequired = true;
			return expr;
		}

		#endregion

		#region Shortcuts

		public static SetIdentifierNode Inc(string var, int value = 1)
		{
			return Set(var, Add(Get(var), Int(value)));
		}

		public static SetMemberNode Inc(NodeBase expr, string mbr, int value = 1)
		{
			return SetMember(expr, mbr, Add(GetMember(expr, mbr), Int(value)));
		}

		public static SetMemberNode Inc(string type, string mbr, int value = 1)
		{
			return SetMember(type, mbr, Add(GetMember(type, mbr), Int(value)));
		}

		public static SetIdentifierNode Dec(string var, int value = 1)
		{
			return Set(var, Sub(Get(var), Int(value)));
		}

		public static SetMemberNode Dec(NodeBase expr, string mbr, int value = 1)
		{
			return SetMember(expr, mbr, Sub(GetMember(expr, mbr), Int(value)));
		}

		public static SetMemberNode Dec(string type, string mbr, int value = 1)
		{
			return SetMember(type, mbr, Sub(GetMember(type, mbr), Int(value)));
		}

		#endregion

		#region Control Structures

		public static CodeBlockNode Block(params NodeBase[] stmts)
		{
			return new CodeBlockNode {Statements = stmts.ToList()};
		}

		public static VarNode Var(string name, NodeBase expr)
		{
			return new VarNode(name) {Value = expr};
		}

		public static VarNode Var(string name, TypeSignature type)
		{
			return new VarNode(name) { Type = type };
		}

		public static VarNode Var(LocalName name, NodeBase expr)
		{
			return new VarNode { LocalName = name, Value = expr };
		}

		public static LetNode Let(string name, NodeBase expr)
		{
			return new LetNode(name) { Value = expr };
		}

		public static LetNode Let(LocalName name, NodeBase expr)
		{
			return new LetNode { LocalName = name, Value = expr };
		}

		public static WhileNode While(NodeBase condition, CodeBlockNode body)
		{
			return new WhileNode {Condition = condition, Body = body};
		}

		public static IfNode If(NodeBase condition, CodeBlockNode ifTrue, CodeBlockNode ifFalse = null)
		{
			return new IfNode {Condition = condition, TrueAction = ifTrue, FalseAction = ifFalse};
		}

		public static ForeachNode For(string name, NodeBase seq, CodeBlockNode body)
		{
			return new ForeachNode {VariableName = name, IterableExpression = seq, Body = body};
		}

		public static ForeachNode For(string name, NodeBase from, NodeBase to, CodeBlockNode body)
		{
			return new ForeachNode { VariableName = name, RangeStart = from, RangeEnd = to, Body = body };
		}

		public static ThrowNode Throw()
		{
			return new ThrowNode();
		}

		public static ThrowNode Throw(NodeBase expr)
		{
			return new ThrowNode {Expression = expr};
		}

		public static ThrowNode Throw(TypeSignature type, params NodeBase[] args)
		{
			return new ThrowNode { Expression = New(type, args) };
		}

		public static TryNode Try(CodeBlockNode body, params CatchNode[] catches)
		{
			return new TryNode {Code = body, CatchClauses = catches.ToList()};
		}

		public static TryNode Try(CodeBlockNode body, CodeBlockNode final, params CatchNode[] catches)
		{
			return new TryNode { Code = body, CatchClauses = catches.ToList(), Finally = final };
		}

		public static CatchNode Catch(TypeSignature excType, string varName, CodeBlockNode body)
		{
			return new CatchNode {Code = body, ExceptionType = excType, ExceptionVariable = varName};
		}

		public static CatchNode Catch(TypeSignature excType, CodeBlockNode body)
		{
			return new CatchNode { Code = body, ExceptionType = excType };
		}

		public static CatchNode CatchAll(params NodeBase[] stmts)
		{
			return new CatchNode { Code = Block(stmts) };
		}

		#endregion

		#region Entities

		public static RecordDefinitionNode Record(string name, params RecordField[] fields)
		{
			var node = new RecordDefinitionNode {Name = name};
			foreach(var curr in fields)
				node.Entries.Add(curr);
			return node;
		}

		public static RecordField Field(string name, TypeSignature type)
		{
			return new RecordField {Name = name, Type = type};
		}

		public static TypeDefinitionNode Type(string name, params TypeLabel[] labels)
		{
			var node = new TypeDefinitionNode {Name = name};
			foreach(var curr in labels)
				node.Entries.Add(curr);
			return node;
		}

		public static TypeLabel Label(string name, TypeSignature tag = null)
		{
			return new TypeLabel {Name = name, TagType = tag};
		}

		public static FunctionNode Fun(string name, params NodeBase[] body)
		{
			return Fun(name, null, false, new FunctionArgument[0], body);
		}

		public static FunctionNode Fun(string name, FunctionArgument[] args, params NodeBase[] body)
		{
			return Fun(name, null, false, args, body);
		}

		public static FunctionNode Fun(string name, TypeSignature type, params NodeBase[] body)
		{
			return Fun(name, type, false, new FunctionArgument[0], body);
		}

		public static FunctionNode Fun(string name, TypeSignature type, FunctionArgument[] args, params NodeBase[] body)
		{
			return Fun(name, type, false, args, body);
		}

		public static FunctionNode Fun(string name, TypeSignature type, bool isPure, params NodeBase[] body)
		{
			return Fun(name, type, isPure, new FunctionArgument[0], body);
		}

		public static FunctionNode Fun(string name, TypeSignature type, bool isPure, FunctionArgument[] args, params NodeBase[] body)
		{
			return new FunctionNode
			{
				Name = name,
				Arguments = args.ToList(),
				ReturnTypeSignature = type,
				Body = Block(body),
				IsPure = isPure
			};
		}

		public static FunctionArgument Arg(string name, TypeSignature type, bool isRef = false)
		{
			return new FunctionArgument {Name = name, TypeSignature = type, IsRefArgument = isRef};
		}

		public static FunctionArgument Arg<T>(string name, bool isRef = false)
		{
			return new FunctionArgument { Name = name, TypeSignature = typeof(T).FullName, IsRefArgument = isRef };
		}

		public static LambdaNode Lambda(FunctionArgument[] args, params NodeBase[] body)
		{
			return new LambdaNode {Body = Block(body), Arguments = args.ToList()};
		}

		public static LambdaNode Lambda(params NodeBase[] body)
		{
			return new LambdaNode { Body = Block(body) };
		}

		#endregion
	}
}
