# Contributing to Algomim AEC MCP

## Issue-first policy

Open or link an issue before sending a pull request. Use `Fixes #123`, `Closes #123`, or
`Refs #123` in the PR description so maintainers can understand the context and avoid duplicate
work. Small typo/documentation fixes can use a brief issue.

Large tool, UX, installer, release, or public API changes should start as a design discussion before
implementation.

Do not report security vulnerabilities in public issues or pull requests. Follow
[SECURITY.md](SECURITY.md) instead.

Public repository rules and maintainer guardrails are documented in
[docs/PUBLIC_REPO.md](docs/PUBLIC_REPO.md).

## Design contract

This codebase favors a **small, clean, extensible core** with a typed Revit tool catalog. Every change is held to:

- **SOLID** - single responsibility per type; depend on interfaces, not concretions.
- **DI composition root** - app startup wires the runtime and delegates public tool ordering to `RevitToolCatalog`. Everything else receives dependencies via constructor injection.
- **Functional core, imperative shell** - pure host-free logic lives in `Algomim.Aec.Mcp.*` and is unit-tested without host SDKs. Side effects (host API, transport I/O, assembly loading, transactions, MSI registration) are pushed to host adapters.
- **Ports/adapters** - common contracts use `Algomim.Aec.Mcp.*`; Revit adapter code uses `Algomim.Revit.Mcp.*`. Future hosts get their own adapter namespace and installer.
- **Modular tool modules** - domain groups implement the module pattern through `RevitToolServices`, `IRevitToolModule`, and `RevitToolModuleRegistry`.
- **Typed tools for common Revit operations** - prefer clear `lower_snake_case` tools with stable schemas and standard response envelopes. Keep `script_execute` for advanced or not-yet-modeled operations.
- **Small functions** - <= 30 lines, <= 4 parameters (use a config/context object beyond that), <= 3 nesting levels (prefer early returns), cognitive complexity <= 15.

## Naming

| Kind | Rule |
|---|---|
| MCP tools | domain_action_object in `lower_snake_case` - `document_get_info`, `element_list_by_category`, `level_create` |
| Methods | verb + noun - `CompileScript`, `DiscoverApi`, `ExecuteInTransaction` |
| Booleans | `is` / `has` / `can` / `should` - `IsValidObject`, `HasStarted` |
| Interfaces | `I` prefix - `IMcpTool`, `IScriptCompiler`, `ITransport` |
| Factories | `Create...` |
| Types / public members / constants | PascalCase |
| Private fields | `_camelCase` |
| Files | one public type per file; small related descriptor tools may be grouped temporarily; larger write/create/export tools move into feature folders |

Naming and style are enforced by `.editorconfig`. Public types and methods carry XML `/// <summary>` docs.

## Namespaces

- Common packages: `Algomim.Aec.Mcp.<Area>`.
- Revit adapter: `Algomim.Revit.Mcp.<Area>`.
- Future hosts: `Algomim.AutoCad.Mcp.<Area>`, `Algomim.Rhino.Mcp.<Area>`.

Use one namespace per folder and file-scoped declarations.

## Adding a typed tool

Common Revit capabilities should be typed MCP tools when they are broadly useful, reusable, and can be expressed with a stable schema:

1. Choose the domain module in `src/hosts/revit/Algomim.Revit.Mcp.Shared/Tools/Composition/RevitToolModuleRegistry.cs`. If the domain does not exist, add a module first.
2. Implement `IMcpTool`, inherit from `RevitToolBase`, or add a descriptor to a domain `*ToolSet` when the tool is small and follows an existing pattern.
3. Use `ToolResponse.Success` / `ToolResponse.Failure` for the response envelope.
4. Keep Revit access on the UI thread via the dispatcher.
5. Wrap writes in `TransactionRunner`; destructive writes need explicit limits and clear error handling.
6. Register the tool through the domain module, not by changing transport or HTTP code.

For write/create/export tools, prefer this feature-folder shape:

```text
Tools/<Domain>/<Feature>/
  <Domain><Feature>Tool.cs
  <Domain><Feature>Args.cs
  <Domain><Feature>Plan.cs
  <Domain><Feature>Planner.cs
  Revit<Domain><Feature>Executor.cs
```

The transport and JSON-RPC dispatcher should not need to change when adding a tool.

## Commits & releases

[Conventional Commits](https://www.conventionalcommits.org) are the release-note and version-bump
standard. `feat:` means minor, `fix:` means patch, and `feat!:`/`fix!:` means major.

Before tagging a release, sync version metadata from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/version.ps1 -Version X.Y.Z
```

This updates `Directory.Build.props`, WiX MSI product versions, and AutoCAD `PackageContents.xml`
`AppVersion` values together. CI runs the same script in check mode, and release tags must match the
synced version using `vX.Y.Z` format.

See [docs/RELEASES.md](docs/RELEASES.md) for the public release checklist and artifact naming.

## Before you push

- `dotnet build -c Release` - zero warnings expected.
- `dotnet test` - pure-core unit tests green.
- A compile is **not** proof of runtime. Smoke-test in real Revit before any release.
- Do not include secrets, proprietary code, customer data, or decompiled source.
