using Algomim.Revit.Mcp.Scripting;
using Xunit;

namespace Algomim.Revit.Mcp.Tests;

public class ScriptPreValidatorTests
{
    [Fact]
    public void Clean_code_yields_no_hints()
        => Assert.Empty(RevitScriptPreValidator.Validate("return doc.Title;"));

    [Fact]
    public void Using_statement_is_flagged()
        => Assert.Contains(RevitScriptPreValidator.Validate("using System;\nreturn 1;"), h => h.Contains("Remove 'using'"));

    [Fact]
    public void IntegerValue_is_flagged_as_deprecated()
        => Assert.Contains(RevitScriptPreValidator.Validate("var x = id.IntegerValue;"), h => h.Contains("ElementId.Value"));

    [Fact]
    public void ScaleElement_hallucination_is_flagged()
        => Assert.Contains(RevitScriptPreValidator.Validate("ElementTransformUtils.ScaleElement(doc, id, 2.0);"), h => h.Contains("ScaleElement"));

    [Fact]
    public void Parameter_subscript_is_flagged()
        => Assert.Contains(RevitScriptPreValidator.Validate("var v = p[\"key\"];"), h => h.Contains("subscript"));

    [Fact]
    public void View_modification_requires_write_mode()
        => Assert.Contains(RevitScriptPreValidator.Validate("view.IsolateElementsTemporary(ids);"), h => h.Contains("mode='write'"));
}
