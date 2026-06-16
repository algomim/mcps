using System.IO;
using System.Linq;
using Algomim.Aec.Mcp.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>
/// Compiles a user C# method body (wrapped in a fixed template) with Roslyn and loads it into a
/// collectible <see cref="ScriptAssemblyLoadContext"/>. Pre-validation hints are prepended to any
/// compile errors so the agent can self-correct.
/// </summary>
public sealed class RoslynScriptCompiler : IScriptCompiler
{
    private const string CodeTemplate = @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;

public class DynamicScript
{
    public object Execute(Document doc, UIDocument uidoc,
        Autodesk.Revit.DB.View activeView, UIApplication uiApp,
        Algomim.Revit.Mcp.Scripting.RevitParams p)
    {
        {USER_CODE}
    }
}
";

    /// <summary>Lines preceding the user code in the template — used to adjust reported error lines.</summary>
    public static int PreambleLineCount { get; } = CodeTemplate
        .Substring(0, CodeTemplate.IndexOf("{USER_CODE}", StringComparison.Ordinal))
        .Split('\n').Length - 1;

    public ScriptCompilationResult Compile(string userCode)
    {
        var hints = RevitScriptPreValidator.Validate(userCode);
        var fullSource = CodeTemplate.Replace("{USER_CODE}", userCode);

        var syntaxTree = CSharpSyntaxTree.ParseText(fullSource, new CSharpParseOptions(LanguageVersion.Latest));
        var references = AssemblyReferenceCollector.Collect();

        var compilation = CSharpCompilation.Create(
            assemblyName: $"DynamicScript_{Guid.NewGuid():N}",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);

        if (!emit.Success)
        {
            var errors = emit.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d =>
                {
                    var line = d.Location.GetMappedLineSpan().StartLinePosition.Line - PreambleLineCount + 1;
                    return $"Line {Math.Max(1, line)}: {d.GetMessage()}";
                })
                .ToList();

            if (hints.Count > 0)
                errors.InsertRange(0, hints);

            return ScriptCompilationResult.Failed(errors);
        }

        ms.Seek(0, SeekOrigin.Begin);
        var loadContext = new ScriptAssemblyLoadContext();
        var assembly = loadContext.LoadFromStream(ms);
        return ScriptCompilationResult.Succeeded(assembly, loadContext);
    }
}
