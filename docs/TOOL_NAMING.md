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
view_create_plans
view_create_text_notes
export_pdf
script_execute
api_discover
```

Do not expose action-first create names in the public catalog:

```text
create_levels
create_grids
create_sheets
create_schedule
create_tags
```

The pure naming policy lives in `src/common/Algomim.Aec.Mcp.Core/Naming/ToolNamePolicy.cs` and is
covered by unit tests.
