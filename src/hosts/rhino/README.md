# Rhino Host Adapter

Initial host skeleton for `rhino-mcp`. It wires the Rhino plugin boundary, lifecycle commands,
runtime announcements, update prefix, and empty MCP tool catalog. Typed Rhino tools are added after
the host can load and connect cleanly.

Current shape:

```text
src/hosts/rhino/
  Algomim.Rhino.Mcp.Shared/
  Algomim.Rhino.Mcp.8/
tests/rhino/
installer/hosts/rhino/
```

The adapter should depend on `src/common/Algomim.Aec.Mcp.*` and keep Rhino SDK references out of common packages.
It follows `docs/HOST_CONTRACT.md`: MCP tools through the shared tool contract,
Connect/Disconnect/Status/Update lifecycle commands, runtime announcements, and the future
`rhino-mcp-X.Y.Z.msi` release asset naming pattern.

Initial Rhino command:

- `Algomim`

`Algomim` shows state-aware options: `Connect`, `Disconnect`, `Status`, and `Update`.
