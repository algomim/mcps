using System.Reflection;
using Autodesk.Revit.DB;
using Algomim.Revit.Mcp.Harness;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Revit.Mcp.Tools.Composition;
using Algomim.Revit.Mcp.Tools.Common;
using RevitView = Autodesk.Revit.DB.View;

namespace Algomim.Revit.Mcp.Tools.Analysis;

internal static class AnalysisToolSet
{
    public static IEnumerable<IMcpTool> Create(RevitToolServices services)
    {
        yield return Tool(services, "view_list_elements", "List element ids visible/listed in a view, sheet or schedule.", ToolCategory.View, Schema.From(new { type = "object", properties = new { viewId = new { type = "integer" } }, required = new[] { "viewId" } }), ViewListElements);
        yield return Tool(services, "sheet_get_contents", "Get viewports and schedules placed on sheets.", ToolCategory.Sheet, Schema.From(new { type = "object", properties = new { sheetIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "sheetIds" } }), SheetGetContents);
        yield return Tool(services, "schedule_get_info", "Get schedule metadata and columns.", ToolCategory.View, Schema.From(new { type = "object", properties = new { scheduleIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "scheduleIds" } }), ScheduleGetInfo);
        yield return Tool(services, "model_list_warnings", "List Revit model warnings.", ToolCategory.Analysis, Schema.From(new { type = "object", properties = new { } }), ModelListWarnings);
        yield return Tool(services, "document_get_units", "List project unit format settings where available.", ToolCategory.Document, Schema.From(new { type = "object", properties = new { } }), DocumentGetUnits);
        yield return Tool(services, "workset_list", "List all worksets in the document.", ToolCategory.Workset, Schema.From(new { type = "object", properties = new { } }), WorksetList);
        yield return Tool(services, "workset_get_for_elements", "Get workset assignments for element ids.", ToolCategory.Workset, Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), WorksetGetForElements);
        yield return Tool(services, "worksharing_get_info", "Get worksharing tooltip/status for element ids.", ToolCategory.Workset, Schema.From(new { type = "object", properties = new { elementIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "elementIds" } }), WorksharingGetInfo);
        yield return Tool(services, "family_get_file_sizes", "Return model file size and best-effort family size placeholders for family ids.", ToolCategory.Family, Schema.From(new { type = "object", properties = new { familyIds = new { type = "array", items = new { type = "integer" } } }, required = new[] { "familyIds" } }), FamilyGetFileSizes);
    }

    private static IMcpTool Tool(RevitToolServices services, string name, string description, ToolCategory category, System.Text.Json.JsonElement schema, Func<RevitToolContext, ArgumentReader, McpToolResult> execute)
        => new DelegateRevitTool(services.Dispatcher, services.DocumentContextStore, name, description, schema, new ToolMetadata(name, category, ToolMode.Read, ToolRisk.Low, description), (uiApp, args) =>
        {
            var context = RevitToolBase.TryCreateContext(uiApp, services.DocumentContextStore, out var error);
            return context is null ? error : execute(context, args);
        });

    private static McpToolResult ViewListElements(RevitToolContext context, ArgumentReader args)
    {
        var viewId = RevitIds.Id(args.RequireLong("viewId"));
        var view = context.Document.GetElement(viewId) as RevitView;
        if (view is null) return ToolResults.Error("VIEW_NOT_FOUND", "View, sheet or schedule not found.");
        if (view.IsTemplate) return ToolResults.Error("VIEW_TEMPLATE_UNSUPPORTED", "View templates do not expose visible elements.");

        var elementIds = new FilteredElementCollector(context.Document, view.Id)
            .WhereElementIsNotElementType()
            .ToElementIds()
            .Select(id => id.Value)
            .ToList();

        return ToolResults.Success(new { view = RevitElementSummary.FromElement(view), count = elementIds.Count, elementIds }, $"{elementIds.Count} element(s) found in view.");
    }

    private static McpToolResult SheetGetContents(RevitToolContext context, ArgumentReader args)
    {
        var sheetIds = args.RequireLongArray("sheetIds", 100);
        var scheduleInstances = new FilteredElementCollector(context.Document)
            .OfClass(typeof(ScheduleSheetInstance))
            .Cast<ScheduleSheetInstance>()
            .ToList();

        var sheets = sheetIds.Select(id =>
        {
            var sheet = context.Document.GetElement(RevitIds.Id(id)) as ViewSheet;
            var viewports = sheet?.GetAllViewports()
                .Select(viewportId => context.Document.GetElement(viewportId) as Viewport)
                .Where(viewport => viewport is not null)
                .Select(viewport => new
                {
                    id = viewport!.Id.Value,
                    viewId = viewport.ViewId.Value,
                    viewName = context.Document.GetElement(viewport.ViewId)?.Name,
                    boxCenter = RevitShapes.Xyz(viewport.GetBoxCenter()),
                })
                .ToList();

            var schedules = sheet is null
                ? []
                : scheduleInstances
                    .Where(instance => instance.OwnerViewId == sheet.Id)
                    .Select(instance => new
                    {
                        id = instance.Id.Value,
                        scheduleId = instance.ScheduleId.Value,
                        scheduleName = context.Document.GetElement(instance.ScheduleId)?.Name,
                        point = RevitShapes.Xyz(instance.Point),
                    })
                    .ToList();

            return new { sheetId = id, sheetName = sheet?.Name, viewports, schedules };
        }).ToList();

        return ToolResults.Success(new { count = sheets.Count, sheets }, $"{sheets.Count} sheet result(s).");
    }

    private static McpToolResult ScheduleGetInfo(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("scheduleIds", 50);
        var schedules = ids.Select(id =>
        {
            var schedule = context.Document.GetElement(RevitIds.Id(id)) as ViewSchedule;
            var fields = new List<object>();
            if (schedule is not null)
            {
                var definition = schedule.Definition;
                for (var i = 0; i < definition.GetFieldCount(); i++)
                {
                    var field = definition.GetField(i);
                    fields.Add(new
                    {
                        index = i,
                        fieldId = field.FieldId.IntegerValue,
                        parameterId = field.ParameterId.Value,
                        columnHeading = field.ColumnHeading,
                        fieldName = field.GetName(),
                        isHidden = field.IsHidden,
                    });
                }
            }

            return new { scheduleId = id, scheduleName = schedule?.Name, fieldCount = fields.Count, fields };
        }).ToList();

        return ToolResults.Success(new { count = schedules.Count, schedules }, $"{schedules.Count} schedule result(s).");
    }

    private static McpToolResult ModelListWarnings(RevitToolContext context, ArgumentReader args)
    {
        var warnings = context.Document.GetWarnings()
            .Select(warning => new
            {
                description = warning.GetDescriptionText(),
                severity = warning.GetSeverity().ToString(),
                failingElementIds = warning.GetFailingElements().Select(id => id.Value).ToList(),
                additionalElementIds = warning.GetAdditionalElements().Select(id => id.Value).ToList(),
            })
            .ToList();

        return ToolResults.Success(new { count = warnings.Count, warnings }, $"{warnings.Count} warning(s) found.");
    }

    private static McpToolResult DocumentGetUnits(RevitToolContext context, ArgumentReader args)
    {
        var units = context.Document.GetUnits();
        var results = new List<object>();
        var getSpecs = typeof(UnitUtils).Assembly.GetType("Autodesk.Revit.DB.UnitUtils")?.GetMethod("GetAllMeasurableSpecs", BindingFlags.Public | BindingFlags.Static);
        var specs = getSpecs?.Invoke(null, null) as System.Collections.IEnumerable;
        if (specs is not null)
        {
            foreach (var spec in specs)
            {
                try
                {
                    var formatOptions = units.GetType().GetMethod("GetFormatOptions", new[] { spec.GetType() })?.Invoke(units, new[] { spec });
                    var unitId = formatOptions?.GetType().GetMethod("GetUnitTypeId")?.Invoke(formatOptions, null);
                    results.Add(new { spec = spec.ToString(), unit = unitId?.ToString(), label = TryLabel(spec), unitLabel = TryLabel(unitId) });
                }
                catch
                {
                    // Some specs do not have document format options.
                }
            }
        }

        return ToolResults.Success(new { count = results.Count, units = results }, $"{results.Count} unit setting(s) found.");
    }

    private static McpToolResult WorksetList(RevitToolContext context, ArgumentReader args)
    {
        if (!context.Document.IsWorkshared)
            return ToolResults.Success(new { count = 0, worksets = Array.Empty<object>() }, "Document is not workshared.");

        var worksets = new FilteredWorksetCollector(context.Document)
            .OfKind(WorksetKind.UserWorkset)
            .Select(workset => new
            {
                id = workset.Id.IntegerValue,
                name = workset.Name,
                owner = workset.Owner,
                kind = workset.Kind.ToString(),
                isOpen = workset.IsOpen,
                isVisibleByDefault = workset.IsVisibleByDefault,
            })
            .ToList();

        return ToolResults.Success(new { count = worksets.Count, worksets }, $"{worksets.Count} workset(s) found.");
    }

    private static McpToolResult WorksetGetForElements(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 1000);
        var results = ids.Select(id =>
        {
            var element = context.Document.GetElement(RevitIds.Id(id));
            return new
            {
                elementId = id,
                worksetId = element?.WorksetId.IntegerValue,
                worksetName = element is null ? null : context.Document.GetWorksetTable().GetWorkset(element.WorksetId)?.Name,
            };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} workset assignment result(s).");
    }

    private static McpToolResult WorksharingGetInfo(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("elementIds", 100);
        var results = ids.Select(id =>
        {
            var elementId = RevitIds.Id(id);
            var info = context.Document.IsWorkshared ? WorksharingUtils.GetWorksharingTooltipInfo(context.Document, elementId) : null;
            return new
            {
                elementId = id,
                checkoutStatus = context.Document.IsWorkshared ? WorksharingUtils.GetCheckoutStatus(context.Document, elementId).ToString() : null,
                creator = info?.Creator,
                owner = info?.Owner,
                lastChangedBy = info?.LastChangedBy,
            };
        }).ToList();

        return ToolResults.Success(new { count = results.Count, elements = results }, $"{results.Count} worksharing result(s).");
    }

    private static McpToolResult FamilyGetFileSizes(RevitToolContext context, ArgumentReader args)
    {
        var ids = args.RequireLongArray("familyIds", 30);
        var modelSizeMb = System.IO.File.Exists(context.Document.PathName) ? new System.IO.FileInfo(context.Document.PathName).Length / 1024d / 1024d : (double?)null;
        var families = ids.Select(id =>
        {
            var family = context.Document.GetElement(RevitIds.Id(id)) as Autodesk.Revit.DB.Family;
            return new
            {
                familyId = id,
                familyName = family?.Name,
                sizeMb = (double?)null,
                note = "Revit API does not expose loaded family byte size directly without exporting/editing the family.",
            };
        }).ToList();

        return ToolResults.Success(new { modelSizeMb, count = families.Count, families }, $"{families.Count} family size result(s).", ["Family size is reported as null unless a future export-based analyzer is enabled."]);
    }

    private static string? TryLabel(object? forgeTypeId)
    {
        if (forgeTypeId is null) return null;
        try
        {
            var method = typeof(LabelUtils).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name.StartsWith("GetLabelFor") && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == forgeTypeId.GetType());
            return method?.Invoke(null, new[] { forgeTypeId }) as string;
        }
        catch
        {
            return null;
        }
    }
}
