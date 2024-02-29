﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarAnalyzer.ShimLayer.AnalysisContext;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace SonarAnalyzer.Test.Wrappers;

[TestClass]
public class RegisterSymbolStartActionWrapperTest
{
#pragma warning disable RS1001 // Missing diagnostic analyzer attribute
#pragma warning disable RS1025 // Configure generated code analysis
#pragma warning disable RS1026 // Enable concurrent execution
    public class TestDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public TestDiagnosticAnalyzer(Action<ShimLayer.AnalysisContext.SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            Action = action;
            SymbolKind = symbolKind;
        }
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(new DiagnosticDescriptor("TEST", "Test", "Test", "Test", DiagnosticSeverity.Warning, true));

        public Action<ShimLayer.AnalysisContext.SymbolStartAnalysisContext> Action { get; }
        public SymbolKind SymbolKind { get; }

        public override void Initialize(Microsoft.CodeAnalysis.Diagnostics.AnalysisContext context) =>
            context.RegisterCompilationStartAction(start =>
                CompilationStartAnalysisContextExtensions.RegisterSymbolStartAction(start, Action, SymbolKind));
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterCodeBlockAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M() => ToString();
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visitedCodeBlocks = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterCodeBlockAction(block =>
                {
                    var node = block.CodeBlock.ToString();
                    visitedCodeBlocks.Add(node);
                });
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visitedCodeBlocks.Should().BeEquivalentTo("int i = 0;", "public void M() => ToString();");
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterCodeBlockStartAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M() => ToString();
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visited = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterCodeBlockStartAction<CS.SyntaxKind>(blockStart =>
                {
                    var node = blockStart.CodeBlock.ToString();
                    visited.Add(node);
                    blockStart.RegisterSyntaxNodeAction(nodeContext => visited.Add(nodeContext.Node.ToString()), CS.SyntaxKind.InvocationExpression);
                });
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visited.Should().BeEquivalentTo("int i = 0;", "public void M() => ToString();", "ToString()");
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterOperationAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M()
                {
                    ToString();
                }
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visited = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterOperationAction(operationContext =>
                {
                    var operation = operationContext.Operation.Syntax.ToString();
                    visited.Add(operation);
                }, ImmutableArray.Create(OperationKind.Invocation));
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visited.Should().BeEquivalentTo("ToString()");
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterOperationBlockAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M() => ToString();
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visited = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterOperationBlockAction(operationBlockContext =>
                {
                    var operation = operationBlockContext.OperationBlocks.First().Syntax.ToString();
                    visited.Add(operation);
                });
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visited.Should().BeEquivalentTo("= 0", "=> ToString()");
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterOperationBlockStartAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M() => ToString();
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visited = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterOperationBlockStartAction(operationBlockStartContext =>
                {
                    var operation = operationBlockStartContext.OperationBlocks.First().Syntax.ToString();
                    visited.Add(operation);
                    operationBlockStartContext.RegisterOperationAction(operationContext => visited.Add(operationContext.Operation.Syntax.ToString()), OperationKind.Invocation);
                });
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visited.Should().BeEquivalentTo("= 0", "=> ToString()", "ToString()");
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterRegisterSymbolEndAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M() => ToString();
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visited = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterSymbolEndAction(symbolContext =>
                {
                    var symbolName = symbolContext.Symbol.Name;
                    visited.Add(symbolName);
                });
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visited.Should().BeEquivalentTo("C");
    }

    [TestMethod]
    public async Task RegisterSymbolStartAction_RegisterSyntaxNodeAction()
    {
        var code = """
            public class C
            {
                int i = 0;
                public void M() => ToString();
            }
            """;
        var snippet = new SnippetCompiler(code);
        var visited = new List<string>();
        var compilation = snippet.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
            new TestDiagnosticAnalyzer(symbolStart =>
            {
                symbolStart.RegisterSyntaxNodeAction(syntaxNodeContext =>
                {
                    var nodeName = syntaxNodeContext.Node.ToString();
                    visited.Add(nodeName);
                }, CS.SyntaxKind.InvocationExpression, CS.SyntaxKind.EqualsValueClause);
            }, SymbolKind.NamedType)));
        await compilation.GetAnalyzerDiagnosticsAsync();
        visited.Should().BeEquivalentTo("= 0", "ToString()");
    }
}
