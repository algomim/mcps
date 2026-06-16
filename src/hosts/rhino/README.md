# Rhino Host Adapter

Reserved host boundary for a future `Algomim.Rhino.Mcp.*` adapter and its own MSI package.

Expected shape:

```text
src/hosts/rhino/
  Algomim.Rhino.Mcp.Shared/
  Algomim.Rhino.Mcp.<Version>/
tests/rhino/
installer/hosts/rhino/
```

The adapter should depend on `src/common/Algomim.Aec.Mcp.*` and keep Rhino SDK references out of common packages.
