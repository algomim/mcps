# Rhino Packaging

Primary package-manager artifact: `.yak`, built with `scripts/build-rhino-yak.ps1`.

Release MSI: `installer/rhino-mcp.wxs`, built with `scripts/build-rhino-msi.ps1`. The MSI embeds
the `.yak` artifact and calls Rhino 8's `Yak.exe install <package.yak>` during installation.

Rhino is release-supported through a Yak-backed MSI. Public releases must include both:

```text
rhino-mcp-X.Y.Z.msi
rhino-mcp-X.Y.Z.msi.sha256
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak
algomim-rhino-mcp-X.Y.Z-rh8_*-win.yak.sha256
```

The Yak package remains the intended path for Rhino Package Manager and future marketplace
distribution.
