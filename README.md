# Algomim AEC MCP

An AEC-focused [Model Context Protocol](https://modelcontextprotocol.io) monorepo. Autodesk Revit
and AutoCAD are the first host adapters; Rhino is planned as a separate host package and installer.
The project is licensed under Apache-2.0 and developed in public through issue-first pull requests.

`revit-mcp` exposes Revit to AI agents through a typed tool catalog plus two low-level primitives:

| Tool family | Purpose |
|---|---|
| Typed tools | Stable `lower_snake_case` Revit operations for document/category/element/family/type/parameter/geometry/view/sheet/graphics/modify/create/export workflows. |
| `script_execute` | Compile and run C# against the live Revit API, inside Revit, on the UI thread, wrapped in a safety harness. Legacy alias: `execute-script`. |
| `api_discover` | Introspect the currently running Revit version's API surface so the agent writes version-correct code. Legacy alias: `discover-api`. |

Common Revit and AutoCAD operations live as first-class typed tools with consistent names, schemas,
response envelopes, and safety policies. The Revit script/API primitives remain available for
advanced cases and for capabilities that have not yet graduated into typed tools.

Common contracts live in `src/common/Algomim.Aec.Mcp.*`; host adapters live under
`src/hosts/<host>/`. The public Revit tool list is assembled through module registration in
`src/hosts/revit/Algomim.Revit.Mcp.Shared/Tools/Composition/RevitToolModuleRegistry.cs`.
AutoCAD follows the same host-adapter shape with C#/.NET API-first tools and no Python/LISP/SCR
foundation.

Current visible Revit MCP names use one canonical `domain_action_object` style, plus 2
backward-compatible low-level aliases.

Representative typed tools:

```text
document_get_info, document_switch_context
category_search, category_list
element_get_info, element_list_by_category
family_list, family_list_types, family_list_elements
parameter_list, parameter_get_values, parameter_set_values
property_list, property_get_values, property_set_values
material_get_layers
geometry_get_locations, geometry_get_bounding_boxes, geometry_get_host_ids, geometry_get_boundary_lines
view_get_active, view_list_elements, view_isolate_elements
sheet_get_contents, sheet_set_revisions, sheet_place_views
schedule_get_info, schedule_create
model_list_warnings, document_get_units, workset_list, workset_get_for_elements
graphics_get_element_overrides, graphics_get_view_filters, graphics_set_element_overrides
selection_get, selection_set
element_move, element_rotate, element_copy, element_delete
grid_create, level_create, sheet_create, tag_create
view_create_plans, view_create_3ds, view_create_sections, view_create_text_notes
export_pdf, export_cad
```

Current AutoCAD MCP catalog: 50 C# AutoCAD API tools across layer, geometry, entity, measurement,
drawing, block, dimension, annotation, document, and DXF export workflows.

## How it works

```text
OpenCode / Codex / Claude  --MCP (localhost)-->  revit-mcp (in Revit)  -->  Revit API
        (agent host)                              connect/disconnect       (UI thread + transaction)
```

The Revit add-in hosts the MCP endpoint in-process. A single ribbon button toggles it: **click to connect, click again to disconnect**. There is no separate connector executable; the installer ships the add-in only.

## Supported Revit versions

| Revit | Runtime |
|---|---|
| 2025 / 2026 | .NET 8 (`net8.0-windows`) |
| 2027 | .NET 10 (`net10.0-windows`) |

## Typed tool response shape

Typed tools return a standard JSON envelope:

```json
{ "ok": true, "data": {}, "summary": "...", "warnings": [] }
```

Errors use the same shape:

```json
{ "ok": false, "code": "...", "message": "...", "details": {}, "warnings": [] }
```

## Install

Ships as host-specific **MSI** packages (built with WiX). Revit and AutoCAD have independent MSIs;
future Rhino packaging should follow the same shape.

Public installer artifacts are published from GitHub Releases when available. Revit and AutoCAD
should be closed before running an installer or update MSI.

## License

Apache-2.0. See [LICENSE](LICENSE).

## Security & Contributing

Please report vulnerabilities privately; see [SECURITY.md](SECURITY.md). Contributions follow an
issue-first workflow; see [CONTRIBUTING.md](CONTRIBUTING.md).

## Documentation

See [docs/README.md](docs/README.md) for architecture, packaging, release, naming, and public
repository guardrail docs.

## Design principles

SOLID; functional core / imperative shell; ports/adapters; modular tool modules; domain-first tool
naming; host-specific installers.

## Status

Early development. The typed Revit foundation, AutoCAD host foundation, host-specific MSI packaging,
and target capability map are implemented; broader real-project smoke testing is still required
before release.
