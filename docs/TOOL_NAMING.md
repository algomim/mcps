# Tool Naming

MCP tool names are public API. The canonical form is:

```text
domain_action_object
```

Rules:

- lowercase `snake_case`
- stable domain prefix
- singular domain names
- no dashes
- no host name inside the tool name unless the operation is genuinely host-specific

Canonical examples:

```text
document_get_info
element_move
view_capture_snapshot
sheet_create
level_create
grid_create
schedule_create
tag_create
export_pdf
script_execute
api_discover
```

Legacy/action-first names can remain as aliases while clients migrate:

```text
create_levels -> level_create
create_grids -> grid_create
create_sheets -> sheet_create
create_schedule -> schedule_create
create_tags -> tag_create
```

The pure naming policy lives in `src/common/Algomim.Aec.Mcp.Core/Naming/ToolNamePolicy.cs` and is
covered by unit tests.
