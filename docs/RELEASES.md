# Release Process

Releases are versioned with `vX.Y.Z` tags. Pushing a tag creates a draft GitHub Release after
cloud-safe checks pass. The draft becomes public only after both host MSI assets are present and
manual Autodesk smoke testing is confirmed.

## Rules

- Do not reuse a tag after publishing a public release.
- Do not upload ad-hoc release assets.
- Do not publish source archives or installers from an unreviewed local tree.
- Keep release tags aligned with `Directory.Build.props` and installer metadata.
- Publish Revit and AutoCAD installers as separate MSI assets.
- Do not publish a draft release until all required host assets and checksums are attached.

## Version Bump

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/version.ps1 -Version X.Y.Z
dotnet test tests/revit/Algomim.Revit.Mcp.Tests/Algomim.Revit.Mcp.Tests.csproj -c Release
dotnet test tests/autocad/Algomim.AutoCad.Mcp.Tests/Algomim.AutoCad.Mcp.Tests.csproj -c Release
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

The release workflow checks that the tag name matches synced metadata, builds the cloud-safe Revit
MSI, and creates a draft release. It does not publish the release automatically.

## Draft And Publish Flow

1. Push `vX.Y.Z`.
2. Wait for the `release` workflow to create a draft release with:

```text
revit-mcp-X.Y.Z.msi
revit-mcp-X.Y.Z.msi.sha256
```

3. Build the AutoCAD MSI on an approved Autodesk workstation.
4. Upload these files to the same draft release:

```text
autocad-mcp-X.Y.Z.msi
autocad-mcp-X.Y.Z.msi.sha256
```

5. Smoke test the Revit and AutoCAD installers.
6. Run the `publish-release` workflow, enter `X.Y.Z`, confirm smoke tests, and type `PUBLISH`.

The `publish-release` workflow verifies that all four required assets exist and that each checksum
matches before making the draft release public.

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
- Revit installer asset was built by the release workflow.
- AutoCAD installer asset was built by an approved Autodesk workstation.
- Checksums are present.
- Release notes do not mention private paths, customer data, or internal-only context.
- Smoke testing in real Revit/AutoCAD has been completed when the change affects host runtime.
