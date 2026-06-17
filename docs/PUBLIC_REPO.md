# Public Repository Guardrails

This repository is public and accepts issue-driven contributions.

## Contributor Flow

1. Search existing issues.
2. Open a bug, feature, or question issue using the templates.
3. Discuss larger API, UX, installer, release, or host-adapter changes before implementation.
4. Open a small, focused pull request linked to the issue with `Fixes #123`, `Closes #123`, or
   `Refs #123`.
5. Keep PR descriptions short and include verification steps.

Blank issues are disabled. Security reports must use private vulnerability reporting instead of
public issues.

## Pull Request Rules

- External PRs must reference an existing issue.
- Maintainer and Dependabot PRs are exempt from issue-first enforcement.
- `main` requires pull requests, code-owner review, and resolved conversations.
- Force pushes and branch deletion are disabled for `main`.
- Public API changes must update docs and tests.
- PRs must not include secrets, proprietary code, customer data, or decompiled source.

## Security Features

Enabled repository safeguards:

- `SECURITY.md` with private reporting instructions
- Dependabot version updates for NuGet and GitHub Actions
- Dependabot security updates
- Secret scanning
- Secret scanning push protection

Security-sensitive reports should be moved out of public issues and into a private advisory.

## Maintainer Checklist

Before making or reviewing public changes:

- Verify the source tree does not contain private/decompiled/proprietary material.
- Keep release assets out of the repository; publish installers only through GitHub Releases.
- Avoid direct pushes to `main` except for urgent maintainer-only fixes.
- Confirm CI is green before release.
- Confirm tags and draft releases point only to the intended clean commits.
- Publish draft releases only after required Revit and AutoCAD installer assets are attached and
  smoke tested.
