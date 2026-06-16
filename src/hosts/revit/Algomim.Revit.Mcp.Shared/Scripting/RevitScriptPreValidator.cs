using System.Text.RegularExpressions;

namespace Algomim.Revit.Mcp.Scripting;

/// <summary>Pre-validates Revit script bodies for common API mistakes before Roslyn compilation.</summary>
public static class RevitScriptPreValidator
{
    private sealed class ValidationRule
    {
        public required Func<string, bool> Matches { get; init; }
        public required string Hint { get; init; }
    }

    private static readonly ValidationRule[] Rules = BuildRules();

    private static ValidationRule[] BuildRules()
    {
        var rules = new List<ValidationRule>();
        rules.AddRange(TemplateViolationRules());
        rules.AddRange(DeprecatedApiRules());
        rules.AddRange(HallucinationRules());
        rules.AddRange(TransactionRequirementRules());
        return rules.ToArray();
    }

    private static IEnumerable<ValidationRule> TemplateViolationRules()
    {
        yield return new()
        {
            Matches = code => code.Split('\n').Any(line => Regex.IsMatch(line.TrimStart(), @"^using\s+(System|Autodesk)\b")),
            Hint = "HINT: Remove 'using' statements - all namespaces (System, System.Linq, Autodesk.Revit.DB.*, etc.) are already pre-imported in the script template."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bvar\s+(doc|uidoc|activeView|uiApp|p)\s*=") ||
                              Regex.IsMatch(code, @"\bthis\.(doc|uidoc|activeView|uiApp|p)\b"),
            Hint = "HINT: Don't redeclare doc, uidoc, activeView, uiApp, or p - they are already in scope. Use them directly."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bp\s*\["),
            Hint = "HINT: Don't use p[\"key\"] subscript syntax. Use p.GetString(\"key\"), p.GetInt(\"key\"), p.GetDouble(\"key\"), p.GetLong(\"key\"), p.GetBool(\"key\"), etc."
        };
    }

    private static IEnumerable<ValidationRule> DeprecatedApiRules()
    {
        yield return new()
        {
            Matches = code => code.Contains("DisplayUnitType"),
            Hint = "HINT: DisplayUnitType is deprecated since Revit 2022. Use UnitTypeId instead (e.g. UnitTypeId.Millimeters)."
        };
        yield return new()
        {
            Matches = code => code.Contains("IntegerValue"),
            Hint = "HINT: ElementId.IntegerValue is removed in Revit 2025+. Use ElementId.Value (returns long) instead."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bParameterType\b") && !code.Contains("ForgeTypeId"),
            Hint = "HINT: ParameterType enum is removed in Revit 2025+. Use ForgeTypeId with SpecTypeId constants instead."
        };
        yield return new()
        {
            Matches = code => code.Contains("BuiltInParameterGroup"),
            Hint = "HINT: BuiltInParameterGroup is removed in Revit 2025+. Use GroupTypeId constants instead."
        };
    }

    private static IEnumerable<ValidationRule> HallucinationRules()
    {
        yield return new()
        {
            Matches = code => code.Contains("MaterialFunctionAssignment.ThermalOrAir") ||
                              code.Contains("MaterialFunctionAssignment.Air") ||
                              code.Contains("MaterialFunctionAssignment.Thermal"),
            Hint = "HINT: MaterialFunctionAssignment has no ThermalOrAir/Air/Thermal. Valid: None, Structure, Substrate, Insulation, Finish1, Finish2, Membrane, StructuralDeck."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\.GetBoundarySegments\s*\(") && Regex.IsMatch(code, @"\bFloor\b"),
            Hint = "HINT: Floor has no GetBoundarySegments(). Use: var sketch = doc.GetElement(floor.SketchId) as Sketch; then sketch.Profile for boundary curves."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bwall\b.*\.Structural\b", RegexOptions.IgnoreCase) && !code.Contains("StructuralUsage"),
            Hint = "HINT: Wall has no 'Structural' property. Use wall.StructuralUsage (StructuralWallUsage) or WallType.Function."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"ScaleElement|ElementTransformUtils\s*\.\s*Scale"),
            Hint = "HINT: ElementTransformUtils.ScaleElement does NOT exist. Revit elements cannot be arbitrarily scaled; explain this and ask what the user wants to achieve."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"SpecTypeId\s*\.\s*Text\b") && !code.Contains("SpecTypeId.String.Text"),
            Hint = "HINT: SpecTypeId.Text does NOT exist. Text uses a nested class: SpecTypeId.String.Text (also SpecTypeId.Int.Integer, SpecTypeId.Boolean.YesNo)."
        };
        yield return new()
        {
            Matches = code => code.Contains("SetProjectionFillColor"),
            Hint = "HINT: OverrideGraphicSettings.SetProjectionFillColor() does NOT exist. Use SetSurfaceForegroundPatternColor() and SetProjectionLineColor()."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"FillPatternElement\b[^.]*\.Target\b") && !code.Contains("GetFillPattern().Target"),
            Hint = "HINT: FillPatternElement has no direct 'Target'. Use fp.GetFillPattern().Target."
        };
        yield return new()
        {
            Matches = code => code.Contains("WorksetTable") && code.Contains("GetWorksets"),
            Hint = "HINT: WorksetTable.GetWorksets() does NOT exist. Use new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset)."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bnew\s+WorksetCollector\b"),
            Hint = "HINT: 'WorksetCollector' does not exist. Use 'FilteredWorksetCollector': new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset)."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bOST_Detail(Lines|Curves|Items)\b"),
            Hint = "HINT: OST_DetailLines/DetailCurves/DetailItems don't exist. Use OST_DetailComponents, or OST_Lines for line-based elements."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bOST_Stairs(Riser|Treads|Landings)\b"),
            Hint = "HINT: OST_StairsRiser/StairsTreads/StairsLandings don't exist. Use OST_Stairs; access sub-elements via Stairs.GetStairsRuns()/GetStairsLandings()."
        };
        yield return new()
        {
            Matches = code => code.Contains("doc.GetCategory"),
            Hint = "HINT: Document.GetCategory() doesn't exist. Use doc.Settings.Categories.get_Item(BuiltInCategory.OST_Xxx)."
        };
        yield return new()
        {
            Matches = code => code.Contains("GetCategoryVisibility"),
            Hint = "HINT: View.GetCategoryVisibility() doesn't exist. Use view.GetCategoryHidden(categoryId)."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"\bViewportType\b"),
            Hint = "HINT: ViewportType class doesn't exist. Viewport types are ElementType with FamilyName='Viewport'."
        };
        yield return new()
        {
            Matches = code => Regex.IsMatch(code, @"view\.GetSectionBox\b"),
            Hint = "HINT: ViewSection.GetSectionBox() doesn't exist. Use view.CropBox for the section's bounding box with transform."
        };
    }

    private static IEnumerable<ValidationRule> TransactionRequirementRules()
    {
        yield return new()
        {
            Matches = code => Regex.IsMatch(code,
                @"\.(IsolateElementsTemporary|IsolateCategoriesTemporary|HideElements|HideCategories|" +
                @"SetElementOverrides|SetCategoryOverrides|DisableTemporaryViewMode|EnableRevealHiddenMode|" +
                @"ApplyViewTemplateParameters|SetFilterOverrides)\s*\("),
            Hint = "HINT: This code calls methods that REQUIRE an active transaction. Call script_execute with mode='write'."
        };
    }

    public static List<string> Validate(string userCode)
        => Rules.Where(rule => rule.Matches(userCode)).Select(rule => rule.Hint).ToList();
}
