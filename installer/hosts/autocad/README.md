# AutoCAD MSI

`autocad-mcp.msi` installs the AutoCAD host adapter as an Autodesk ApplicationPlugins bundle.

The installer should ship AutoCAD bundles under:

```text
%ProgramData%\Autodesk\ApplicationPlugins\Algomim.AutoCad.Mcp.bundle\
```

Expected bundle layout:

```text
Algomim.AutoCad.Mcp.bundle/
  PackageContents.xml
  Contents/
    2025/
      Algomim.AutoCad.Mcp.2025.dll
    2026/
      Algomim.AutoCad.Mcp.2026.dll
    2027/
      Algomim.AutoCad.Mcp.2027.dll
```

The MSI harvests 2025, 2026, and 2027 outputs. AutoCAD 2027 requires the approved local build
environment to have AutoCAD 2027 SDK/runtime assemblies available before packaging.

This installer owns only AutoCAD-specific manifests, registration, and binaries.
