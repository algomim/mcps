# Security Policy

## Supported Versions

Security fixes are handled for the latest public release and the current `main` branch.

## Reporting a Vulnerability

Please do not open a public GitHub issue for security vulnerabilities.

Use GitHub private vulnerability reporting or open a private security advisory for this repository:

https://github.com/algomim/mcps/security/advisories/new

Include:

- affected host: Revit, AutoCAD, or shared MCP tooling
- affected version or commit
- reproduction steps
- expected impact
- any logs, screenshots, or proof-of-concept details with secrets removed

We will acknowledge valid reports as soon as practical, triage severity, and coordinate a fix before
public disclosure.

## Scope

In scope:

- arbitrary code execution beyond intended `script_execute` behavior
- local privilege escalation
- unsafe update or installer behavior
- unauthenticated access to non-loopback MCP endpoints
- disclosure of local project data, model data, credentials, tokens, or filesystem content
- release artifact integrity issues

Out of scope:

- reports requiring already-compromised maintainer machines
- denial-of-service reports without a concrete security impact
- social engineering or physical attacks
- vulnerabilities in Autodesk products, GitHub, Windows, or third-party services outside this repo

## Public Issues

If a report may expose a vulnerability, create a private report instead of a public issue. Public
security issues may be closed and moved to a private advisory.
