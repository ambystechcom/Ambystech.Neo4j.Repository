---
title: Contributing
description: How to set up the repo, run the tests, and get a change merged.
---

Thanks for taking the time to contribute. This guide covers the moving parts — branching, versioning, CI, and the local dev loop — so your first PR lands without surprises.

## Prerequisites

- [.NET SDK 10.0.x](https://dotnet.microsoft.com/download) — the SDK can build the library for `net8.0`, `net9.0`, and `net10.0`.
- [Node.js 20+](https://nodejs.org) (only needed if you're editing the documentation site under `site/`)
- A running Neo4j instance — only required for the `example/` project and ad-hoc experimentation. Unit tests run fully in-memory against mocked sessions.

## Getting the code

```bash
git clone https://github.com/ambystechcom/Ambystech.Neo4j.Repository.git
cd Ambystech.Neo4j.Repository
dotnet restore Ambystech.Neo4j.Repository.sln
```

## Repository layout

| Path            | What lives there                                                        |
|-----------------|-------------------------------------------------------------------------|
| `contracts/`    | `Ambystech.Neo4j.Repository.Contracts` — attributes, base node, search models |
| `lib/`          | `Ambystech.Neo4j.Repository` — driver wiring, repository, converters    |
| `example/`      | Console app demonstrating a User / Post social graph                    |
| `unit-tests/`   | MSTest suite                                                            |
| `site/`         | Astro Starlight documentation site (this site)                          |
| `.github/workflows/` | CI: PR gate, NuGet publish, docs deploy                            |

## Branching & versioning

The repo uses [GitVersion](https://gitversion.net/) in `Mainline` mode. You don't edit `<Version>` by hand — GitVersion computes it from branch and merge-commit messages.

- `main` — released packages. Each push publishes a stable NuGet version (patch bump by default).
- `develop` — prerelease packages tagged `-beta`. Pushes publish prerelease NuGets with a minor bump.
- Feature branches — open PRs against `develop` for new work, or against `main` for fixes that should ship immediately.

Override the bump level from a **merge commit message**:

| Message fragment             | Bump    |
|------------------------------|---------|
| `+semver: breaking` or `major` | Major |
| `+semver: feature` or `minor`  | Minor |
| `+semver: fix` or `patch`      | Patch |

Plain commits on a feature branch don't bump the version — only the merge commit into `main` or `develop` does.

## Local dev loop

### Build and test

```bash
dotnet build Ambystech.Neo4j.Repository.sln --configuration Release
dotnet test  Ambystech.Neo4j.Repository.sln --configuration Release
```

The PR gate runs exactly this. If it's green locally, CI will be green too.

### Run the example

Configure a Neo4j instance via user secrets and run:

```bash
cd example
dotnet user-secrets set "Neo4j-Uri"      "bolt://localhost:7687"
dotnet user-secrets set "Neo4j-User"     "neo4j"
dotnet user-secrets set "Neo4j-Password" "your-password"
dotnet run
```

### Edit the docs site

```bash
cd site
npm install
npm run dev      # http://localhost:4321/Ambystech.Neo4j.Repository/
npm run build    # production build into site/dist/
```

## Coding guidelines

- Target `net8.0`, `net9.0`, and `net10.0` for code under `contracts/` and `lib/` — don't introduce APIs that only exist on a newer TFM without a conditional compilation guard (e.g. `#if NET10_0_OR_GREATER`).
- Public types and members need XML docs — `GenerateDocumentationFile` is on, so missing docs surface as CS1591 warnings.
- Keep the repository surface small. New public members on `IBaseGraphRepository<T>` are a breaking change for consumers that implement the interface directly.
- Prefer a new test in `unit-tests/` over a manual repro. The suite uses MSTest + AutoFixture + Moq — follow the style already in place.

## Pull requests

1. Fork or branch, commit your change.
2. Open a PR against `develop` (new features) or `main` (fixes). Describe the change and link any related issue.
3. The **Build & Test** check runs automatically — it restores the solution, builds in Release, runs `dotnet test`, and uploads a `.trx` artifact.
4. A failing check blocks the merge. Fix the code, not the guard.
5. On merge:
   - PRs into `develop` publish a new `-beta` prerelease NuGet for `Ambystech.Neo4j.Repository`.
   - PRs into `main` publish a stable NuGet and, for `lib/` changes, cut a GitHub release with a tag, changelog, and the `.nupkg` attached.
   - PRs under `contracts/` publish the contracts package on merge to `main`.
   - Changes confined to `site/` trigger a GitHub Pages deploy and **skip the NuGet pipeline**.

## What to avoid

- Don't bump `<Version>` manually — GitVersion owns that.
- Don't re-add `pull_request` triggers to the deploy workflows. The PR gate is the PR gate; the deploy workflows run on push only.
- Don't commit secrets. Use `dotnet user-secrets`, environment variables, or GitHub Actions secrets.
- Don't land breaking changes on `main` without the `+semver: breaking` marker on the merge commit.

## Reporting bugs & asking questions

Open an issue on [GitHub](https://github.com/ambystechcom/Ambystech.Neo4j.Repository/issues). A minimal repro — ideally a failing test or a small snippet — is the fastest path to a fix.
