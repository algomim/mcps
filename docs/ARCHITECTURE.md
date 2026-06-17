# Algomim AEC MCP - Architecture & Build Plan

## What it is

An AEC MCP monorepo. Autodesk Revit and AutoCAD are the first release-supported host adapters;
Rhino has an initial separate host skeleton. The Revit adapter is hosted **inside Revit** and exposes:

- typed Revit tools for common read, write, create, graphics, and export operations;
- `script_execute` for advanced C# execution against the live Revit API;
- `api_discover` for version-correct API reflection.

Legacy aliases `execute-script` and `discover-api` remain available for backward compatibility.
The AutoCAD adapter is hosted **inside AutoCAD** and exposes a C#/.NET API-first typed tool catalog.
Python, LISP, SCR files, and raw script execution are deliberately outside the AutoCAD foundation.
The Rhino adapter skeleton is hosted **inside Rhino** and wires lifecycle commands plus an empty MCP
catalog before typed RhinoCommon tools are added.

## Runtime topology

```text
   Agent (opencode-algomim / Codex / Claude)
        |  MCP over HTTP   (http://localhost:<port>/mcp)
        v
   Host add-in  -- UI thread + transaction/document lock -->  Autodesk API
   (hosts the MCP server in-process)
```

- Each host add-in hosts an **MCP-over-HTTP server on a per-instance port**.
- Single Autodesk host instance, any agent -> connect to the one URL. Works in opencode, Codex,
  Claude. No broker.
- Automatic multi-instance discovery -> announcement files under the local MCP runtime directory.
- Host ribbon actions follow a shared shape: Connect/Disconnect, Status, and Update. Status shows
  the exact `http://127.0.0.1:<port>/mcp` URL for external MCP clients.

## Tool model

Typed tools use `lower_snake_case` names with a domain prefix. Current domains:

- `document_*`
- `category_*`
- `element_*`
- `family_*`
- `type_*`
- `parameter_*`
- `property_*`
- `material_*`
- `geometry_*`
- `view_*`
- `sheet_*`
- `schedule_*`
- `model_*`
- `workset_*` / `worksharing_*`
- `graphics_*`
- `selection_*`
- create operations under their target domain, such as `grid_create`, `sheet_create`, and `view_create_*`
- `export_*`

Public ordering and registration live in each host adapter's tool catalog.
Revit domain module ordering lives in
`src/hosts/revit/Algomim.Revit.Mcp.Shared/Tools/Composition/RevitToolModuleRegistry.cs`.
Adding a new typed tool should not require transport, dispatcher, or app startup changes.

Create tools use domain-first names such as `level_create`, `grid_create`, `sheet_create`,
`schedule_create`, `tag_create`, and `view_create_plans`.

Target capability coverage is represented as explicit typed tools with stable schemas.

Typed tools return:

```json
{ "ok": true, "data": {}, "summary": "...", "warnings": [] }
```

Typed tool errors return:

```json
{ "ok": false, "code": "...", "message": "...", "details": {}, "warnings": [] }
```

### `script_execute`

- **Input:** `{ code: string, mode?: "read" | "write", params?: object }`
- Wraps user C# in a template exposing `(doc, uidoc, activeView, uiApp, p)`.
- Compiles with Roslyn, runs on the **Revit UI thread**.
- `write` mode runs inside a `Transaction`.
- Legacy alias: `execute-script`.

### `api_discover`

- **Input:** `{ query: string }`
- Reflects the live, currently loaded Revit API so code is version-correct.
- Returns curated members/signatures rather than a raw reflection dump.
- Legacy alias: `discover-api`.

## Safety harness

1. **UI-thread dispatcher** (`ExternalEvent`) - all Revit API runs on the UI thread.
2. **Pre-validation** - active document/view checks; Roslyn diagnostics; script pre-validator hints.
3. **Transaction + rollback** - writes run inside a transaction; reads do not.
4. **Exception containment** - catchable errors become structured results.
5. **Modal-dialog suppression** - warnings and task dialogs are auto-resolved where possible.
6. **Collectible `AssemblyLoadContext` + LRU compile cache** - bounded memory over long sessions.
7. **Tool metadata** - typed tools carry category, read/write mode, and risk for future UI/approval policies.

## Project structure

```text
aec-skills/revit-mcp/
|-- Algomim.Aec.Mcp.slnx
|-- src/
|   |-- common/
|   |   |-- Algomim.Aec.Mcp.Abstractions/
|   |   |     Host-neutral MCP contracts, protocol models, tool envelopes, metadata.
|   |   |-- Algomim.Aec.Mcp.Core/
|   |   |     Pure naming, validation, geometry DTOs, operation plan primitives.
|   |   `-- Algomim.Aec.Mcp.Tooling/
|   |         Host-neutral module and catalog validation primitives.
|   `-- hosts/
|       |-- revit/
|       |   |-- Algomim.Revit.Mcp.Shared/
|       |   |     Harness, host, discovery, scripting, typed tools, RevitMcpApp.
|       |   |-- Algomim.Revit.Mcp.2025/
|       |   |-- Algomim.Revit.Mcp.2026/
|       |   `-- Algomim.Revit.Mcp.2027/
|       |-- autocad/
|       `-- rhino/
|           |-- Algomim.Rhino.Mcp.Shared/
|           `-- Algomim.Rhino.Mcp.8/
|-- tests/revit/Algomim.Revit.Mcp.Tests/
|-- tests/rhino/Algomim.Rhino.Mcp.Tests/
|-- installer/
|-- docs/
`-- revit-mcp.slnx
```

Common package rule: `Algomim.Aec.Mcp.*` must not reference host SDKs.
Host adapters depend inward on common packages.

Typed tool folders live under `src/hosts/revit/Algomim.Revit.Mcp.Shared/Tools/` by domain:

```text
Analysis/
Api/
Category/
Common/
Create/
Document/
Element/
Export/
Family/
Geometry/
Graphics/
Modify/
Parameter/
Selection/
View/
```

Small related tools may use `*ToolSet` descriptor files. Larger write/create/export operations should
move into feature folders with this shape:

```text
Tools/<Domain>/<Feature>/
  <Domain><Feature>Tool.cs
  <Domain><Feature>Args.cs
  <Domain><Feature>Plan.cs
  <Domain><Feature>Planner.cs
  Revit<Domain><Feature>Executor.cs
```

This keeps argument parsing, pure planning, and Revit side effects separated. `grid_create` is the
first migrated example of this pattern.

Tool modules are composed through small host-specific service bags and catalog builders such as
`RevitToolServices`/`RevitToolCatalog` and `AutoCadToolServices`/`AutoCadToolCatalog`. This keeps
the adapters aligned without forcing a full DI container into Autodesk add-ins.

## Transport + multi-instance readiness

- **In-process MCP-over-HTTP server** using `HttpListener`.
- **Per-instance port** from a pool starting at `48884`.
- **Announcement** written to `%LOCALAPPDATA%\Temp\mcp-runtime\announcements\<host>-{pid}-{port}.json`.
- **Ribbon** standard actions: Connect/Disconnect starts or stops the HTTP server and writes/removes
  the announcement; Status shows the MCP URL and health URL; Update checks for a newer host MSI.

## Versions, packaging, CI

- Revit 2025 / 2026: `net8.0-windows`.
- Revit 2027: `net10.0-windows`.
- Host-specific MSI via WiX. Revit and AutoCAD are current release-supported packages; Rhino has a
  reserved MSI boundary and host skeleton.
- CI builds against Nice3point Revit API reference packages, so Revit does not need to be installed to build.
- Release-supported host smoke tests are still required before public release.

## Roadmap

| Phase | Deliverable |
|---|---|
| 1 | Core host, safety harness, `script_execute`, `api_discover`, first typed read tools. Done. |
| 2 | Query/view/selection/analysis tools. Done. |
| 3 | Controlled write tools: parameters, graphics, move/copy/rotate/delete. Done. |
| 4 | Create/export tools: levels, grids, sheets, views, schedules, tags, PDF/CAD. Done. |
| 5 | Real Revit smoke tests, stronger edge-case handling, optional shared broker. |

## What is deliberately different

- No separate named-pipe connector process.
- No opaque copied tool catalog.
- No unstructured string-only outputs for typed tools.
- No fixed single port.
- No Revit 2024 / net48 target in this runtime family.
