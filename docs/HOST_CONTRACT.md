# Host Contract

This document defines the minimum contract for a public `src/hosts/<host>` adapter. It keeps new
hosts aligned without forcing Revit, AutoCAD, Rhino, or future adapters into the same SDK-specific
implementation.

## MCP Tool Contract

Host adapters expose agent-facing capabilities as MCP tools, not as host UI commands.

Every public MCP tool must:

- implement the shared `IMcpTool` shape: name, description, JSON input schema, and executor;
- appear through `tools/list` and execute through `tools/call`;
- use the standard typed response envelope: `ok`, `data`, `summary`, `warnings`, and structured
  failure fields;
- follow `domain_action_object` `lower_snake_case` naming unless it is an explicitly documented
  legacy alias;
- keep host SDK references inside `src/hosts/<host>` and out of `src/common/Algomim.Aec.Mcp.*`.

## Host Lifecycle Contract

Each installed host add-in must expose the same user-facing lifecycle shape:

- **Connect/Disconnect** starts or stops the in-process MCP HTTP server for the current host
  instance.
- **Status** shows the exact client connection addresses:

```text
MCP URL: http://127.0.0.1:<port>/mcp
Health: http://127.0.0.1:<port>/health
```

- **Update** checks GitHub Releases for a newer host-specific MSI using the host asset prefix.

These lifecycle actions are not MCP tools. They are host UI or host command entry points that help a
user connect an external MCP client such as Codex, Claude, or OpenCode.

## Runtime Discovery Contract

When connected, each host instance must write a runtime announcement with:

- host owner, such as `revit`, `autocad`, or `rhino`;
- process id;
- selected port;
- MCP URL;
- health URL;
- display name and current document name when available.

Disconnect and host shutdown must remove the announcement for that process so stale instances do not
remain discoverable.

## Update And Release Contract

Each release-supported host must use a stable release asset prefix:

```text
<host>-mcp-X.Y.Z.msi
<host>-mcp-X.Y.Z.msi.sha256
```

The host update checker must look for its own `<host>-mcp-` prefix. Public release publishing must
verify every required asset and checksum for every release-supported host, then rely on human
maintainer confirmation that host-specific smoke tests are complete.

## Packaging Contract

Each host owns its own installer. Host MSIs must remain independently installable even if future
bundle installers compose multiple host MSIs.

Host installer inputs own only host-specific folders, manifests, registry keys, and shortcuts.
Release assets must not be committed to the repository.

## Current Host Status

| Host | MCP tools | Lifecycle UI | Installer | Public release asset |
|---|---|---|---|---|
| Revit | Implemented | Implemented | Implemented | `revit-mcp-X.Y.Z.msi` |
| AutoCAD | Implemented | Implemented | Implemented | `autocad-mcp-X.Y.Z.msi` |
| Rhino | Implemented | Implemented | Yak-backed MSI + Yak | `rhino-mcp-X.Y.Z.msi`, `algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak` |
