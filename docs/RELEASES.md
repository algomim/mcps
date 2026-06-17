# Release Process

Releases are versioned with `vX.Y.Z` tags. Pushing a tag creates a draft GitHub Release after
cloud-safe checks pass. The draft becomes public only after every release-supported host MSI asset
and checksum is present, and host-specific smoke testing is confirmed.

## Rules

- Do not reuse a tag after publishing a public release.
- Do not upload ad-hoc release assets.
- Do not publish source archives or installers from an unreviewed local tree.
- Keep release tags aligned with `Directory.Build.props` and installer metadata.
- Publish every release-supported host installer as a separate MSI asset.
- Do not publish a draft release until all required host assets and checksums are attached.

## Version Bump

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/version.ps1 -Version X.Y.Z
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/test-host-neutral.ps1 -Configuration Release
```

The test script discovers `*.Tests.csproj` projects under `tests/`, so new host MCP test suites do
not require CI or release workflow edits.

The version script updates:

- `Directory.Build.props` `Version`
- Revit, AutoCAD, and Rhino WiX package versions
- AutoCAD `PackageContents.xml` `AppVersion`

## Tagging

After the version bump is committed:

```powershell
git tag vX.Y.Z
git push origin main
git push origin vX.Y.Z
```

The release workflow checks that the tag name matches synced metadata, builds cloud-safe MSI assets,
and creates a draft release. It does not publish the release automatically.

## Draft And Publish Flow

1. Push `vX.Y.Z`.
2. Wait for the `release` workflow to create a draft release with cloud-built host assets. Currently:

```text
revit-mcp-X.Y.Z.msi
revit-mcp-X.Y.Z.msi.sha256
```

3. Build every remaining release-supported host artifact on the required approved workstation or
   build environment for that host.
4. Upload the remaining host files and checksums to the same draft release. Required patterns live
   in `release/hosts.json`. Currently:

```text
autocad-mcp-X.Y.Z.msi
autocad-mcp-X.Y.Z.msi.sha256
rhino-mcp-X.Y.Z.msi
rhino-mcp-X.Y.Z.msi.sha256
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak.sha256
```

5. Smoke test every release-supported host installer.
6. Run the `publish-release` workflow, enter `X.Y.Z`, confirm smoke tests, and type `PUBLISH`.

The `publish-release` workflow verifies that all required host assets exist and that each checksum
matches before making the draft release public.

## Artifacts

Release assets use this naming:

```text
revit-mcp-X.Y.Z.msi
revit-mcp-X.Y.Z.msi.sha256
autocad-mcp-X.Y.Z.msi
autocad-mcp-X.Y.Z.msi.sha256
rhino-mcp-X.Y.Z.msi
rhino-mcp-X.Y.Z.msi.sha256
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak.sha256
```

Future release-supported hosts must follow the same `<host>-mcp-X.Y.Z.msi` and
`<host>-mcp-X.Y.Z.msi.sha256` naming pattern and be added to `release/hosts.json`.

The in-product update check searches GitHub Releases for host-specific MSI assets with
`revit-mcp-`, `autocad-mcp-`, or `rhino-mcp-` prefixes.

## Public Release Checklist

Before publishing:

- `main` is clean and pushed.
- CI is green.
- No old tags or stale draft releases exist for the same version.
- Every release-supported host installer asset was built by its approved build path.
- Checksums are present for every release-supported host installer.
- Every MSI includes the shared Algomim MCP EULA from `installer/legal/EULA.rtf`.
- Release notes do not mention private paths, customer data, or internal-only context.
- Smoke testing in the real host application has been completed when the change affects host
  runtime.
