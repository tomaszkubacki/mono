// 
// ControlFlowGraph.cs
// 
// Authors:
// 	Alexander Chebaturkin (chebaturkin@gmail.com)
// 
// Copyright (C) 2011 Alexander Chebaturkin
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//  
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.IO;
using Mono.CodeContracts.Static.AST;
using Mono.CodeContracts.Static.Analysis;
using Mono.CodeContracts.Static.ControlFlow.Subroutines;
using Mono.CodeContracts.Static.DataStructures;
using Mono.CodeContracts.Static.Providers;

namespace Mono.CodeContracts.Static.ControlFlow {
	class ControlFlowGraph : ICFG {
		private readonly object method_repository;
		private readonly Subroutine method_subroutine;

		public ControlFlowGraph (Subroutine subroutine, object methodRepository)
		{
			this.method_subroutine = subroutine;
			this.method_repository = methodRepository;
		}

		private CFGBlock EntryBlock
		{
			get { return this.method_subroutine.Entry; }
		}

		private CFGBlock ExitBlock
		{
			get { return this.method_subroutine.Exit; }
		}

		private CFGBlock ExceptionExitBlock
		{
			get { return this.method_subroutine.ExceptionExit; }
		}

		public Method CFGMethod
		{
			get
			{
				var methodInfo = this.method_subroutine as IMethodInfo;
				if (methodInfo != null)
					return methodInfo.Method;
				throw new InvalidOperationException ("CFG has bad subroutine that is not a method");
			}
		}

		#region ICFG Members
		public APC Entry
		{
			get { return new APC (EntryBlock, 0, null); }
		}

		public APC EntryAfterRequires
		{
			get { return new APC (this.method_subroutine.EntryAfterRequires, 0, null); }
		}

		public APC NormalExit
		{
			get { return new APC (ExitBlock, 0, null); }
		}

		public APC ExceptionExit
		{
			get { return new APC (ExceptionExitBlock, 0, null); }
		}

		public Subroutine Subroutine
		{
			get { return this.method_subroutine; }
		}

		public APC Next (APC pc)
		{
			APC next;

			if (HasSingleSuccessor (pc, out next))
				return next;

			return pc;
		}

		public bool HasSingleSuccessor (APC pc, out APC ifFound)
		{
			return pc.Block.Subroutine.HasSingleSuccessor (pc, out ifFound);
		}

		public IEnumerable<APC> Successors (APC pc)
		{
			return pc.Block.Subroutine.Successors (pc);
		}

		public bool HasSinglePredecessor (APC pc, out APC ifFound)
		{
			return pc.Block.Subroutine.HasSinglePredecessor (pc, out ifFound);
		}

		public IEnumerable<APC> Predecessors (APC pc)
		{
			return pc.Block.Subroutine.Predecessors (pc);
		}

		public bool IsJoinPoint (APC pc)
		{
			if (pc.Index != 0)
				return false;

			return IsJoinPoint (pc.Block);
		}

		public bool IsSplitPoint (APC pc)
		{
			if (pc.Index != pc.Block.Count)
				return false;

			return IsSplitPoint (pc.Block);
		}

		public bool IsBlockStart (APC pc)
		{
			return pc.Index == 0;
		}

		public bool IsBlockEnd (APC pc)
		{
			return pc.Index == pc.Block.Count;
		}

		public IILDecoder<APC, Dummy, Dummy, IMethodContextProvider, Dummy> GetDecoder (IMetaDataProvider metaDataProvider)
		{
			var methodRepository = this.method_repository as SubroutineFacade;
			return new APCDecoder (this, metaDataProvider, methodRepository);
		}

		public void Print (TextWriter tw, ILPrinter<APC> printer,
		                   Func<CFGBlock, IEnumerable<LispList<Edge<CFGBlock, EdgeTag>>>> contextLookup,
		                   LispList<Edge<CFGBlock, EdgeTag>> context)
		{
			var set = new HashSet<Pair<Subroutine, LispList<Edge<CFGBlock, EdgeTag>>>> ();
			this.method_subroutine.Print (tw, printer, contextLookup, context, set);
		}
		#endregion

		private bool IsJoinPoint (CFGBlock block)
		{
			return block.Subroutine.IsJoinPoint (block);
		}

		private bool IsSplitPoint (CFGBlock block)
		{
			return block.Subroutine.IsSplitPoint (block);
		}

		public IGraph<APC, Dummy> AsForwardGraph ()
		{
			return new GraphWrapper<APC, Dummy> (new APC[0], (pc) => SuccessorsEdges (pc));
		}

		private IEnumerable<Pair<Dummy, APC>> SuccessorsEdges (APC pc)
		{
			APC last = pc.LastInBlock ();
			foreach (APC succ in Successors (last))
				yield return new Pair<Dummy, APC> (Dummy.Value, succ);
		}
	}
}
