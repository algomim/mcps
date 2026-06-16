# AutoCAD Host Adapter

AutoCAD MCP follows the same host-adapter shape as Revit: shared C# plugin code plus
version-specific projects that reference the installed Autodesk AutoCAD SDK assemblies.

Current shape:

```text
src/hosts/autocad/
  Algomim.AutoCad.Mcp.Shared/
  Algomim.AutoCad.Mcp.2025/   # net8.0-windows, included in solution
  Algomim.AutoCad.Mcp.2026/   # net8.0-windows, included in solution
  Algomim.AutoCad.Mcp.2027/   # net10.0-windows, prepared for SDK availability
tests/autocad/
installer/hosts/autocad/
```

The adapter depends on `src/common/Algomim.Aec.Mcp.*` and keeps AutoCAD SDK references out of
common packages.

Core rule: the AutoCAD MCP plugin is C#/.NET API first. Python, LISP, SCR files, and raw script
execution are not part of the foundation.

Initial catalog: 50 C# AutoCAD API tools across layer, geometry, entity, measurement, drawing,
block, dimension, annotation, document, and DXF export workflows.
