namespace Algomim.Revit.Mcp.Hosting;

/// <summary>Revit-specific MCP server identity and client-facing instructions.</summary>
internal static class RevitMcpServerProfile
{
    public const string ServerName = "revit-mcp";

    public const string ServerInstructions =
        "revit-mcp controls Autodesk Revit in-process through a broad typed Revit tool catalog plus two low-level primitives.\n" +
        "Prefer typed tools for common model queries and controlled operations. Typed tools use lower_snake_case " +
        "domain_action naming and return a JSON envelope: { ok, data, summary, warnings } or " +
        "{ ok:false, code, message, details, warnings }.\n" +
        "Typed tool domains include document, category, element, family, type, parameter, property, material, " +
        "geometry, view, sheet, schedule, model analysis, workset/worksharing, graphics, selection, modify, create, and export.\n" +
        "Use tools/list for the exact active schema; common examples include document_get_info, document_switch_context, " +
        "category_search, element_get_info, parameter_list, geometry_get_bounding_boxes, graphics_set_element_overrides, " +
        "element_move, create_sheets, create_schedule, create_tags, export_pdf, and export_cad.\n" +
        "Low-level primitives:\n" +
        "- script_execute (legacy alias: execute-script): run C# method body against the active document. " +
        "Args: { code, mode?, params? }. mode='write' wraps the run in a transaction. In scope: doc, uidoc, " +
        "activeView, uiApp, p (RevitParams). Do not add using-directives.\n" +
        "- api_discover (legacy alias: discover-api): introspect the live Revit API. Args: { query }. " +
        "Use it before scripts or advanced tool composition when unsure about signatures.";
}
