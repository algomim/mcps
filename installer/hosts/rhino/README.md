# Rhino Packaging

Primary package-manager artifact: `.yak`, built with `scripts/build-rhino-yak.ps1`.

Local skeleton MSI: `installer/rhino-mcp.wxs`, built with `scripts/build-rhino-msi.ps1`. The MSI
embeds the `.yak` artifact and calls Rhino 8's `Yak.exe install <package.yak>` during installation.

The host skeleton exists under `src/hosts/rhino`, but Rhino is not release-supported yet. Do not add
Rhino artifacts to public publish requirements until package install behavior and smoke testing are
complete.

The MSI currently owns only Rhino-specific plug-in payload binaries for local testing. The Yak
package is the intended path for Rhino Package Manager and future marketplace distribution.
