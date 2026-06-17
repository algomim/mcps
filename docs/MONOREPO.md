# Monorepo Shape

Current package boundary:

```text
src/
  common/
    Algomim.Aec.Mcp.Abstractions/
    Algomim.Aec.Mcp.Core/
    Algomim.Aec.Mcp.Tooling/
  hosts/
    revit/
      Algomim.Revit.Mcp.Shared/
      Algomim.Revit.Mcp.2025/
      Algomim.Revit.Mcp.2026/
      Algomim.Revit.Mcp.2027/
    autocad/
      Algomim.AutoCad.Mcp.Shared/
      Algomim.AutoCad.Mcp.2025/
      Algomim.AutoCad.Mcp.2026/
      Algomim.AutoCad.Mcp.2027/
    rhino/
      Algomim.Rhino.Mcp.Shared/
      Algomim.Rhino.Mcp.8/
      README.md
tests/
  revit/
    Algomim.Revit.Mcp.Tests/
  autocad/
    Algomim.AutoCad.Mcp.Tests/
  rhino/
    Algomim.Rhino.Mcp.Tests/
```

Future host adapters should follow the same boundary:

```text
src/hosts/autocad/Algomim.AutoCad.Mcp.*
src/hosts/rhino/Algomim.Rhino.Mcp.*
```

Dependency direction:

```text
Host Adapter -> AEC Tooling -> AEC Core -> AEC Abstractions
```

Common AEC packages must not reference host SDK assemblies. Host-specific installers stay under
`installer/hosts/<host>/`, and each host can ship an independent MSI.
