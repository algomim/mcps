# Packaging

Each release-supported host ships as its own MSI. Rhino also publishes a Yak package for the
package-manager path:

```text
revit-mcp.msi
autocad-mcp.msi
algomim-rhino-mcp-X.Y.Z-rh8_0-win.yak
rhino-mcp.msi
```

Host MSIs must remain independently installable. Future bundle installers should compose host MSIs
instead of mixing host-specific registration rules into one setup.

All host MSIs must show the shared Algomim MCP EULA from `installer/legal/EULA.rtf` through the
standard WiX UI license flow.

Packaging unit:

```text
One MSI per host, not one MSI per product year.

revit-mcp.msi   -> Revit 2025, 2026, 2027 payloads/manifests
autocad-mcp.msi -> AutoCAD 2025, 2026, 2027 payloads/manifests
algomim-rhino-mcp-*.yak -> Rhino Package Manager / marketplace path
rhino-mcp.msi           -> wrapper that installs the bundled Yak package
```

Year-specific binaries stay inside the host MSI. This keeps installation simple for users while
still allowing each Autodesk version to load its matching runtime assembly.

Installer edge-case behavior:

- If none of the supported versions for a host is installed, the MSI stops with a clear message.
- If only one supported version is installed, only that year's add-in payload is installed.
- Missing years are not populated under Revit `Addins/<year>` folders.
- AutoCAD still uses Autodesk AppLoader `RuntimeRequirements`; the MSI also skips payload folders
  for unsupported/missing AutoCAD years.

GitHub releases should publish current release-supported host MSI assets:

```text
revit-mcp-X.Y.Z.msi
autocad-mcp-X.Y.Z.msi
rhino-mcp-X.Y.Z.msi
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak
revit-mcp-X.Y.Z.msi.sha256
autocad-mcp-X.Y.Z.msi.sha256
rhino-mcp-X.Y.Z.msi.sha256
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak.sha256
```

Version metadata is release-gated. Before creating a tag, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/version.ps1 -Version X.Y.Z
```

The script keeps these values aligned:

- `Directory.Build.props` `Version`
- Revit, AutoCAD, and Rhino WiX `Package` versions (`X.Y.Z.0`)
- AutoCAD `PackageContents.xml` `AppVersion`

CI checks the current repository version metadata. The release workflow also checks that the tag
name (`vX.Y.Z`) matches the synced metadata before building installers.

## Update discovery

Installed host add-ins check GitHub Releases for newer host-specific MSI assets:

```text
revit-mcp-X.Y.Z.msi
autocad-mcp-X.Y.Z.msi
algomim-rhino-mcp-X.Y.Z-*.yak
rhino-mcp-X.Y.Z.msi
```

Revit, AutoCAD, and Rhino share the common release-checking code. On startup, each host
checks the latest GitHub Release in the background and notifies the user only when a newer
host-specific release artifact is available. The Update button keeps a manual path: it checks again
and can open the release/download page, but it does not download or install artifacts automatically.

Release tags run on GitHub-hosted Windows and create a draft release after cloud-safe checks pass.
The workflow builds the Revit MSI because Revit uses NuGet reference assemblies. AutoCAD and Rhino
installer artifacts must be built on approved host workstations because they depend on installed
host SDK/application packaging tools. MSIs must be built as x64 packages.

Current Revit targets:

```text
Revit 2025 -> net8.0-windows
Revit 2026 -> net8.0-windows
Revit 2027 -> net10.0-windows
```

Current AutoCAD targets:

```text
AutoCAD 2025 -> net8.0-windows
AutoCAD 2026 -> net8.0-windows
AutoCAD 2027 -> net10.0-windows
```

Installer layout:

```text
installer/
  revit-mcp.wxs
  hosts/
    revit/
    autocad/
    rhino/
  bundles/
```

See [Release Process](RELEASES.md) for the public release checklist.
