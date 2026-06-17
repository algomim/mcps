# Installer Layout

Each host product gets its own MSI. Future all-in-one installers should be bundles that compose
host MSIs instead of merging host-specific install logic into one file.

```text
installer/
  revit-mcp.wxs              # current Revit MSI
  rhino-mcp.wxs              # Rhino Yak-backed MSI
  legal/
    EULA.rtf                 # shared MSI license text
  hosts/
    revit/README.md
    autocad/README.md
    rhino/README.md
  bundles/
    README.md
```

Rules:

- One host, one MSI: `revit-mcp.msi`, `autocad-mcp.msi`, `rhino-mcp.msi`.
- Bundle installers may depend on host MSIs, but host MSIs must remain independently installable.
- Host installers own only host-specific folders, manifests, registry keys, and shortcuts.
- Common package versioning comes from the repository root.
- Host MSIs must use the shared WiX UI license screen with `installer/legal/EULA.rtf`.
