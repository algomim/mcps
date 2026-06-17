# Release Process

Releases are versioned with `vX.Y.Z` tags and publish host-specific MSI assets.

## Rules

- Do not reuse a tag after publishing a public release.
- Do not upload ad-hoc release assets.
- Do not publish source archives or installers from an unreviewed local tree.
- Keep release tags aligned with `Directory.Build.props` and installer metadata.
- Publish Revit and AutoCAD installers as separate MSI assets.

## Version Bump

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/version.ps1 -Version X.Y.Z
dotnet test Algomim.Aec.Mcp.slnx --no-restore
```

The version script updates:

- `Directory.Build.props` `Version`
- Revit and AutoCAD WiX package versions
- AutoCAD `PackageContents.xml` `AppVersion`

## Tagging

After the version bump is committed:

```powershell
git tag vX.Y.Z
git push origin main
git push origin vX.Y.Z
```

The release workflow checks that the tag name matches synced metadata before building installers.

## Artifacts

Release assets use this naming:

```text
revit-mcp-X.Y.Z.msi
revit-mcp-X.Y.Z.msi.sha256
autocad-mcp-X.Y.Z.msi
autocad-mcp-X.Y.Z.msi.sha256
```

The in-product update check searches GitHub Releases for host-specific MSI assets with
`revit-mcp-` or `autocad-mcp-` prefixes.

## Public Release Checklist

Before publishing:

- `main` is clean and pushed.
- CI is green.
- No old tags or stale draft releases exist for the same version.
- Installer assets were built by the release workflow or approved release runner.
- Checksums are present.
- Release notes do not mention private paths, customer data, or internal-only context.
- Smoke testing in real Revit/AutoCAD has been completed when the change affects host runtime.
