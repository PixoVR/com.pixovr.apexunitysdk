using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PixoVR.StyleCheck
{
	/// <summary>
	/// Checks the package's C# against the company style in AGENTS.md: tabs, braces on their own
	/// lines and around every conditional and loop, a blank line after each conditional or loop
	/// block, and the naming rules. Usage: StyleCheck [packageRoot [file...]]; with no files it checks
	/// every .cs file in the package. Exits 1 when a rule is broken.
	/// </summary>
	public static class Program
	{
		private static readonly string[] kCheckedDirectories = { "Runtime", "Editor", "Tests", "Samples~", "Tools~" };

		public static int Main(string[] commandLineArguments)
		{
			string packageRoot = Path.GetFullPath(commandLineArguments.Length > 0 ? commandLineArguments[0] : ".");
			List<string> sourcePaths = new List<string>();
			for (int argumentIndex = 1; argumentIndex < commandLineArguments.Length; argumentIndex++)
			{
				sourcePaths.Add(Path.GetFullPath(commandLineArguments[argumentIndex], packageRoot));
			}

			string[] checkedDirectories = sourcePaths.Count > 0 ? Array.Empty<string>() : kCheckedDirectories;
			foreach (string checkedDirectory in checkedDirectories)
			{
				string directoryPath = Path.Combine(packageRoot, checkedDirectory);
				if (Directory.Exists(directoryPath))
				{
					IEnumerable<string> directorySources = Directory
						.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
						.Where(sourcePath => !IsBuildOutput(sourcePath));
					sourcePaths.AddRange(directorySources);
				}
			}

			sourcePaths.Sort(StringComparer.Ordinal);
			int problemCount = 0;
			foreach (string sourcePath in sourcePaths)
			{
				string relativePath = Path.GetRelativePath(packageRoot, sourcePath).Replace('\\', '/');
				foreach (string problem in SourceFileChecker.Check(File.ReadAllText(sourcePath)))
				{
					Console.WriteLine(relativePath + ":" + problem);
					problemCount++;
				}
			}

			if (problemCount > 0)
			{
				Console.WriteLine(problemCount + " style problem(s) in " + sourcePaths.Count + " files");
				return 1;
			}

			Console.WriteLine("No style problems in " + sourcePaths.Count + " files");
			return 0;
		}

		private static bool IsBuildOutput(string sourcePath)
		{
			string normalizedPath = sourcePath.Replace('\\', '/');
			return normalizedPath.Contains("/bin/") || normalizedPath.Contains("/obj/");
		}
	}

	/// <summary>Checks one file. Code inside #if blocks is checked under every symbol the file tests.</summary>
	public static class SourceFileChecker
	{
		private static readonly Regex kSymbolPattern = new Regex(@"^\s*#\s*(?:if|elif)\b(.*)$", RegexOptions.Multiline);
		private static readonly Regex kIdentifierPattern = new Regex(@"[A-Za-z_][A-Za-z0-9_]*");
		private const int kMaximumLineWidth = 120;

		public static IEnumerable<string> Check(string sourceText)
		{
			SortedSet<(int, string)> problems = new SortedSet<(int, string)>();
			foreach (string[] symbolSet in BuildSymbolSets(sourceText))
			{
				CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp9,
					preprocessorSymbols: symbolSet);
				SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceText, parseOptions);
				StyleWalker styleWalker = new StyleWalker(syntaxTree.GetText());
				styleWalker.Visit(syntaxTree.GetRoot());
				foreach ((int, string) problem in styleWalker.GetProblems())
				{
					problems.Add(problem);
				}
			}

			foreach ((int, string) problem in CheckIndentation(sourceText))
			{
				problems.Add(problem);
			}

			return problems.Select(problem => problem.Item1 + ": " + problem.Item2);
		}

		private static List<string[]> BuildSymbolSets(string sourceText)
		{
			SortedSet<string> testedSymbols = new SortedSet<string>(StringComparer.Ordinal);
			foreach (Match directiveMatch in kSymbolPattern.Matches(sourceText))
			{
				foreach (Match identifierMatch in kIdentifierPattern.Matches(directiveMatch.Groups[1].Value))
				{
					if (identifierMatch.Value != "true" && identifierMatch.Value != "false")
					{
						testedSymbols.Add(identifierMatch.Value);
					}
				}
			}

			List<string[]> symbolSets = new List<string[]> { Array.Empty<string>(), testedSymbols.ToArray() };
			foreach (string testedSymbol in testedSymbols)
			{
				symbolSets.Add(new[] { testedSymbol });
			}

			return symbolSets;
		}

		private static IEnumerable<(int, string)> CheckIndentation(string sourceText)
		{
			string[] sourceLines = sourceText.Replace("\r\n", "\n").Split('\n');
			bool insideBlockComment = false;
			for (int lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++)
			{
				string sourceLine = sourceLines[lineIndex];
				string trimmedLine = sourceLine.TrimStart(' ', '\t');
				if (!insideBlockComment && sourceLine.StartsWith(" ") && trimmedLine.Length > 0)
				{
					yield return (lineIndex + 1, "indent with tabs, not spaces");
				}

				int lineWidth = sourceLine.Replace("\t", "    ").Length;
				if (lineWidth > kMaximumLineWidth)
				{
					yield return (lineIndex + 1, "keep lines within " + kMaximumLineWidth
						+ " columns (tabs count as 4)");
				}

				bool closesBlock = trimmedLine.StartsWith("}");
				bool previousLineBlank = lineIndex > 0 && sourceLines[lineIndex - 1].Trim().Length == 0;
				if (closesBlock && previousLineBlank)
				{
					yield return (lineIndex + 1, "no blank line before a closing brace");
				}

				if (trimmedLine.Contains("/*"))
				{
					insideBlockComment = true;
				}

				if (trimmedLine.Contains("*/"))
				{
					insideBlockComment = false;
				}
			}
		}
	}

	/// <summary>Walks one syntax tree and records each broken rule with its line number.</summary>
	public sealed class StyleWalker : CSharpSyntaxWalker
	{
		private const string kSuppressionComment = "StyleCheck: external name";

		private static readonly Regex kPascalCase = new Regex("^[A-Z][A-Za-z0-9]*$");
		private static readonly Regex kInterfaceCase = new Regex("^I[A-Z][A-Za-z0-9]*$");
		private static readonly Regex kEnumCase = new Regex("^E[A-Z][A-Za-z0-9]*$");
		private static readonly Regex kUpperCase = new Regex("^[A-Z][A-Z0-9_]*$");
		private static readonly Regex kMemberCase = new Regex("^m_[a-z][A-Za-z0-9]*$");
		private static readonly Regex kConstantCase = new Regex("^k[A-Z][A-Za-z0-9]*$");
		private static readonly Regex kCamelCase = new Regex("^[a-z][A-Za-z0-9]*$");
		private static readonly Regex kTypeParameterCase = new Regex("^T([A-Z][A-Za-z0-9]*)?$");
		private const int kMinimumNameLength = 3;

		private readonly SourceText m_sourceText;
		private readonly List<(int, string)> m_problems = new List<(int, string)>();

		public StyleWalker(SourceText sourceText) : base(SyntaxWalkerDepth.Token)
		{
			m_sourceText = sourceText;
		}

		public IReadOnlyList<(int, string)> GetProblems()
		{
			return m_problems;
		}

		// ---------------------------------------------------------------- Braces

		public override void VisitToken(SyntaxToken token)
		{
			if (token.IsKind(SyntaxKind.OpenBraceToken) || token.IsKind(SyntaxKind.CloseBraceToken))
			{
				CheckBracePlacement(token);
			}

			base.VisitToken(token);
		}

		private void CheckBracePlacement(SyntaxToken braceToken)
		{
			SyntaxNode braceOwner = braceToken.Parent;
			bool isStructuralBrace = braceOwner is BlockSyntax
				|| braceOwner is BaseNamespaceDeclarationSyntax
				|| braceOwner is BaseTypeDeclarationSyntax
				|| braceOwner is SwitchStatementSyntax
				|| (braceOwner is AccessorListSyntax accessorList
					&& accessorList.Accessors.Any(accessor => accessor.Body != null
						|| accessor.ExpressionBody != null));
			if (!isStructuralBrace)
			{
				return;
			}

			int braceLine = GetLine(braceToken);
			SyntaxToken previousToken = braceToken.GetPreviousToken();
			if (previousToken != default && GetLine(previousToken) == braceLine)
			{
				Report(braceToken, "put '" + braceToken.Text + "' on its own line");
				return;
			}

			SyntaxToken nextToken = braceToken.GetNextToken();
			bool nextTokenOnSameLine = nextToken != default && GetLine(nextToken) == braceLine;
			if (!nextTokenOnSameLine)
			{
				return;
			}

			bool closesStatement = braceToken.IsKind(SyntaxKind.CloseBraceToken)
				&& (nextToken.IsKind(SyntaxKind.SemicolonToken) || nextToken.IsKind(SyntaxKind.CloseParenToken)
					|| nextToken.IsKind(SyntaxKind.CommaToken));
			if (!closesStatement)
			{
				Report(braceToken, "put '" + braceToken.Text + "' on its own line");
			}
		}

		public override void VisitIfStatement(IfStatementSyntax ifStatement)
		{
			RequireBlock(ifStatement.Statement, "if");
			if (ifStatement.Else != null && !(ifStatement.Else.Statement is IfStatementSyntax))
			{
				RequireBlock(ifStatement.Else.Statement, "else");
			}

			base.VisitIfStatement(ifStatement);
		}

		public override void VisitForStatement(ForStatementSyntax forStatement)
		{
			RequireBlock(forStatement.Statement, "for");
			base.VisitForStatement(forStatement);
		}

		public override void VisitForEachStatement(ForEachStatementSyntax forEachStatement)
		{
			RequireBlock(forEachStatement.Statement, "foreach");
			CheckLocalName(forEachStatement.Identifier, "loop variables");
			base.VisitForEachStatement(forEachStatement);
		}

		public override void VisitWhileStatement(WhileStatementSyntax whileStatement)
		{
			RequireBlock(whileStatement.Statement, "while");
			base.VisitWhileStatement(whileStatement);
		}

		public override void VisitDoStatement(DoStatementSyntax doStatement)
		{
			RequireBlock(doStatement.Statement, "do");
			base.VisitDoStatement(doStatement);
		}

		private void RequireBlock(StatementSyntax bodyStatement, string keyword)
		{
			if (!(bodyStatement is BlockSyntax))
			{
				Report(bodyStatement.GetFirstToken(), "put braces around the body of '" + keyword + "'");
			}
		}

		// ---------------------------------------------------------------- Blank line after blocks

		public override void VisitBlock(BlockSyntax block)
		{
			CheckBlankLinesAfterControlBlocks(block.Statements);
			base.VisitBlock(block);
		}

		public override void VisitSwitchSection(SwitchSectionSyntax switchSection)
		{
			CheckBlankLinesAfterControlBlocks(switchSection.Statements);
			base.VisitSwitchSection(switchSection);
		}

		private void CheckBlankLinesAfterControlBlocks(SyntaxList<StatementSyntax> statements)
		{
			for (int statementIndex = 0; statementIndex + 1 < statements.Count; statementIndex++)
			{
				StatementSyntax statement = statements[statementIndex];
				bool isControlBlock = statement is IfStatementSyntax
					|| statement is ForStatementSyntax
					|| statement is ForEachStatementSyntax
					|| statement is WhileStatementSyntax
					|| statement is DoStatementSyntax
					|| statement is SwitchStatementSyntax
					|| statement is TryStatementSyntax;
				if (!isControlBlock)
				{
					continue;
				}

				int closingLine = GetLine(statement.GetLastToken());
				int lineAfterBlock = closingLine + 1;
				string textAfterBlock = lineAfterBlock < m_sourceText.Lines.Count
					? m_sourceText.Lines[lineAfterBlock].ToString().Trim()
					: string.Empty;
				// A preprocessor line (#endif, #else) may follow directly.
				if (textAfterBlock.Length > 0 && !textAfterBlock.StartsWith("#"))
				{
					Report(statement.GetLastToken(),
						"leave a blank line after the closing brace of a conditional or loop");
				}
			}
		}

		// ---------------------------------------------------------------- Type and member names

		public override void VisitClassDeclaration(ClassDeclarationSyntax classDeclaration)
		{
			CheckName(classDeclaration.Identifier, kPascalCase, "class names are PascalCase");
			base.VisitClassDeclaration(classDeclaration);
		}

		public override void VisitStructDeclaration(StructDeclarationSyntax structDeclaration)
		{
			CheckName(structDeclaration.Identifier, kPascalCase, "struct names are PascalCase");
			base.VisitStructDeclaration(structDeclaration);
		}

		public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax interfaceDeclaration)
		{
			CheckName(interfaceDeclaration.Identifier, kInterfaceCase, "interface names are I + PascalCase");
			base.VisitInterfaceDeclaration(interfaceDeclaration);
		}

		public override void VisitEnumDeclaration(EnumDeclarationSyntax enumDeclaration)
		{
			CheckName(enumDeclaration.Identifier, kEnumCase, "enum names are E + PascalCase");
			base.VisitEnumDeclaration(enumDeclaration);
		}

		public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax enumMember)
		{
			CheckName(enumMember.Identifier, kUpperCase, "enum values are UPPER_CASE");
			base.VisitEnumMemberDeclaration(enumMember);
		}

		public override void VisitDelegateDeclaration(DelegateDeclarationSyntax delegateDeclaration)
		{
			CheckName(delegateDeclaration.Identifier, kPascalCase, "delegate names are PascalCase");
			base.VisitDelegateDeclaration(delegateDeclaration);
		}

		public override void VisitMethodDeclaration(MethodDeclarationSyntax methodDeclaration)
		{
			CheckName(methodDeclaration.Identifier, kPascalCase, "method names are PascalCase");
			base.VisitMethodDeclaration(methodDeclaration);
		}

		public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax localFunction)
		{
			CheckName(localFunction.Identifier, kPascalCase, "function names are PascalCase");
			base.VisitLocalFunctionStatement(localFunction);
		}

		public override void VisitPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
		{
			CheckName(propertyDeclaration.Identifier, kPascalCase, "property names are PascalCase");
			base.VisitPropertyDeclaration(propertyDeclaration);
		}

		public override void VisitEventDeclaration(EventDeclarationSyntax eventDeclaration)
		{
			CheckName(eventDeclaration.Identifier, kPascalCase, "event names are PascalCase");
			base.VisitEventDeclaration(eventDeclaration);
		}

		public override void VisitTypeParameter(TypeParameterSyntax typeParameter)
		{
			CheckName(typeParameter.Identifier, kTypeParameterCase, "type parameters are T or T + PascalCase");
			base.VisitTypeParameter(typeParameter);
		}

		public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax eventField)
		{
			foreach (VariableDeclaratorSyntax eventVariable in eventField.Declaration.Variables)
			{
				CheckName(eventVariable.Identifier, kPascalCase, "event names are PascalCase");
			}

			base.VisitEventFieldDeclaration(eventField);
		}

		public override void VisitFieldDeclaration(FieldDeclarationSyntax fieldDeclaration)
		{
			// Static read-only fields hold fixed values (patterns, tables, delegates) and are named like constants.
			bool isConstant = fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword)
				|| (fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
					&& fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword));
			bool isPublic = fieldDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword);
			bool isInteropField = IsInsideInteropStruct(fieldDeclaration);
			foreach (VariableDeclaratorSyntax fieldVariable in fieldDeclaration.Declaration.Variables)
			{
				if (isConstant)
				{
					CheckName(fieldVariable.Identifier, kConstantCase,
						"constants and static read-only fields are k + PascalCase");
				}
				else if (isInteropField)
				{
					CheckName(fieldVariable.Identifier, kPascalCase,
						"fields of interop structs are PascalCase, like the C header");
				}
				else if (isPublic)
				{
					if (!HasSuppression(fieldVariable.Identifier))
					{
						Report(fieldVariable.Identifier, "make field '" + fieldVariable.Identifier.Text
							+ "' private (m_) and expose it through a property");
					}
				}
				else
				{
					CheckName(fieldVariable.Identifier, kMemberCase, "member variables are m_ + camelCase");
				}
			}

			base.VisitFieldDeclaration(fieldDeclaration);
		}

		private static bool IsInsideInteropStruct(FieldDeclarationSyntax fieldDeclaration)
		{
			return fieldDeclaration.Parent is StructDeclarationSyntax structDeclaration
				&& structDeclaration.AttributeLists
					.SelectMany(attributeList => attributeList.Attributes)
					.Any(attribute => attribute.Name.ToString().Contains("StructLayout"));
		}

		// ---------------------------------------------------------------- Parameters and locals

		public override void VisitParameter(ParameterSyntax parameter)
		{
			bool isDiscard = parameter.Identifier.Text == "_";
			if (!isDiscard && parameter.Identifier.Text.Length > 0)
			{
				CheckLocalName(parameter.Identifier, "parameters");
			}

			base.VisitParameter(parameter);
		}

		public override void VisitVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
		{
			if (!(variableDeclarator.Parent?.Parent is FieldDeclarationSyntax)
				&& !(variableDeclarator.Parent?.Parent is EventFieldDeclarationSyntax))
			{
				CheckLocalName(variableDeclarator.Identifier, "local variables");
			}

			base.VisitVariableDeclarator(variableDeclarator);
		}

		public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax variableDesignation)
		{
			CheckLocalName(variableDesignation.Identifier, "local variables");
			base.VisitSingleVariableDesignation(variableDesignation);
		}

		public override void VisitCatchDeclaration(CatchDeclarationSyntax catchDeclaration)
		{
			if (catchDeclaration.Identifier.Text.Length > 0)
			{
				CheckLocalName(catchDeclaration.Identifier, "exception variables");
			}

			base.VisitCatchDeclaration(catchDeclaration);
		}

		private void CheckLocalName(SyntaxToken identifier, string description)
		{
			if (HasSuppression(identifier))
			{
				return;
			}

			if (!kCamelCase.IsMatch(identifier.Text))
			{
				Report(identifier, description + " are camelCase: '" + identifier.Text + "'");
			}
			else if (identifier.Text.Length < kMinimumNameLength)
			{
				Report(identifier, "use a descriptive name instead of '" + identifier.Text + "'");
			}
		}

		// ---------------------------------------------------------------- Helpers

		private void CheckName(SyntaxToken identifier, Regex namePattern, string rule)
		{
			if (!namePattern.IsMatch(identifier.Text) && !HasSuppression(identifier))
			{
				Report(identifier, rule + ": '" + identifier.Text + "'");
			}
		}

		private bool HasSuppression(SyntaxToken identifier)
		{
			int identifierLine = GetLine(identifier);
			return m_sourceText.Lines[identifierLine].ToString().Contains(kSuppressionComment);
		}

		private int GetLine(SyntaxToken token)
		{
			return m_sourceText.Lines.GetLineFromPosition(token.SpanStart).LineNumber;
		}

		private void Report(SyntaxToken token, string message)
		{
			m_problems.Add((GetLine(token) + 1, message));
		}
	}
}
