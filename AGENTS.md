# AGENTS.md

Guidance for AI coding agents working in this repository. Read this first — it captures the non-obvious conventions, guardrails, and gotchas that aren't visible from the file tree.

## What this repo is

A .NET library implementing the Repository pattern for Neo4j, published as two NuGet packages:

- `Ambystech.Neo4j.Repository.Contracts` — base types, attributes, search models. No driver dependency.
- `Ambystech.Neo4j.Repository` — driver wiring, generic repository, converters, DI extensions.

Everything else (`example/`, `unit-tests/`, `site/`) exists to support those two packages.

## Layout

| Path                   | Purpose                                                          | Ships? |
|------------------------|------------------------------------------------------------------|--------|
| `contracts/`           | `Ambystech.Neo4j.Repository.Contracts` package                   | NuGet  |
| `lib/`                 | `Ambystech.Neo4j.Repository` package                             | NuGet  |
| `example/`             | Console app — User/Post social graph. Reference usage.           | No     |
| `unit-tests/`          | MSTest suite (multi-targeted).                                   | No     |
| `site/`                | Astro Starlight documentation site.                              | GH Pages |
| `.github/workflows/`   | CI: PR gate + 3 NuGet publishers + Pages deploy.                 | —      |
| `Ambystech.Neo4j.Repository.sln` | The solution root. All `dotnet` commands use this.     | —      |

## Target frameworks

`contracts/` and `lib/` multi-target **`net8.0;net9.0;net10.0`**. `unit-tests/` multi-targets the same three. `example/` is single-target `net10.0`.

- Don't add TFM-specific APIs without a preprocessor guard (`#if NET10_0_OR_GREATER`, etc.).
- When adding a dependency, verify it supports all three TFMs.
- The `.NET 10 SDK` can build all three — the PR workflow installs all three SDKs so tests run on each runtime.

## Build & test

Always operate on the solution, not individual projects:

```bash
dotnet restore Ambystech.Neo4j.Repository.sln
dotnet build   Ambystech.Neo4j.Repository.sln -c Release --no-restore
dotnet test    Ambystech.Neo4j.Repository.sln -c Release --no-build
```

The PR gate runs exactly these commands. If the above is green locally, the gate will be green.

## Docs site (`site/`)

Astro Starlight. The `site/` tree has its own lockfile and dependencies; don't mix it with the .NET build.

```bash
cd site
npm ci           # CI-parity install
npm run dev      # http://localhost:4321/Ambystech.Neo4j.Repository/
npm run build    # production build → site/dist/
```

**Critical pins in `site/package.json`**:
- `@astrojs/starlight@^0.37.7` — **do not bump to 0.38+**. Starlight 0.38 requires Astro 6.
- `overrides.zod: ^3.25.76` — Astro 5 still uses zod v3.
- `overrides["@astrojs/sitemap"]: 3.6.1` — sitemap ≥3.7 depends on zod v4 and crashes the 404 route.

If you need a Starlight/Astro upgrade, expect to revisit those pins together.

**Base path gotcha**: the site deploys to `https://ambystechcom.github.io/Ambystech.Neo4j.Repository/`. `astro.config.mjs` sets `base: '/Ambystech.Neo4j.Repository/'`. Starlight auto-prefixes sidebar, nav, and pagination links — but **hero action links and markdown body links are NOT auto-prefixed**. Always write internal links with the full path:

```markdown
See [Repository](/Ambystech.Neo4j.Repository/packages/repository/)
```

A bare `/packages/repository/` 404s on GitHub Pages.

**Icons**: the `Card` component's `icon` prop only accepts names from `@astrojs/starlight/components/Icons.ts`. Seti icons (`seti:time`, etc.) are registered only for the file-tree component and render blank in CardGrid.

## Branching, versioning, releases

[GitVersion](https://gitversion.net) in **Mainline** mode (see `GitVersion.yml`):

- `main` → stable NuGet (patch bump by default).
- `develop` → prerelease NuGet tagged `-beta` (minor bump).
- Feature branches → PR into `develop` (features) or `main` (fixes).

**Don't edit `<Version>` in `*.csproj` — GitVersion computes it.**

Override the bump level from the **merge commit message**:

| Fragment                        | Bump  |
|---------------------------------|-------|
| `+semver: breaking` or `major`  | Major |
| `+semver: feature` or `minor`   | Minor |
| `+semver: fix` or `patch`       | Patch |

`commit-message-incrementing: MergeMessageOnly` — only merge commits matter, feature-branch commits don't bump.

## Workflow topology

Path filters ensure only one pipeline fires per change. **Never re-add `pull_request:` triggers to deploy workflows** — the PR gate handles PRs, deploys fire only on `push`.

| Workflow                  | Trigger                                     | Does                                      |
|---------------------------|---------------------------------------------|-------------------------------------------|
| `build-test.yml`          | PR to `main`/`develop`                      | Restore, build, test, upload `.trx`       |
| `deploy-contracts.yml`    | Push to `main`, ignoring `lib/**`, `site/**`, `.github/workflows/**` | Publish contracts NuGet     |
| `deploy-prerelease.yml`   | Push to `develop`, ignoring `contracts/**`, `site/**`, `.github/workflows/**` | Publish `-beta` NuGet  |
| `deploy-release.yml`      | Push to `main`, ignoring `contracts/**`, `site/**`, `.github/workflows/**` | Publish stable, tag, release |
| `deploy-site.yml`         | Push to `main` under `site/**`              | Build + deploy docs to GitHub Pages       |

When adding a new top-level directory, **update the `paths-ignore` lists** in the three deploy workflows so edits there don't spuriously bump NuGet versions.

## Public API stability

- `IBaseGraphRepository<T>` is implementation-facing. Adding a member is a breaking change for third-party implementers. If you add one, mark the release with `+semver: breaking`.
- `BaseNode` field names (`Id`, `CreatedAt`, `UpdatedAt`, `DeletedAt`) are serialised to Neo4j property names (`created_at`, `updated_at`, `deleted_at`). Renaming them breaks existing graphs — don't.
- `AddNeo4jRepository()` has two overloads. The `Action<ConfigBuilder>` overload **does not read auth** from configuration (it calls `GraphDatabase.Driver(uri, configBuilder)`). Document this whenever you touch it.

## Coding conventions

- Public members get XML doc comments. `GenerateDocumentationFile` is on, so missing docs show up as CS1591 warnings — triage them, don't suppress globally.
- `LangVersion=13.0` across both packages. OK to use C# 13 features.
- `Nullable` and `ImplicitUsings` are enabled.
- Prefer a unit test over a manual repro. The suite uses MSTest + AutoFixture + Moq — match the existing style.
- Keep the repository surface small. Escape hatches exist (`ExecuteAsync`, `ExecuteReadAsync<T>`) — use them instead of bloating `IBaseGraphRepository<T>`.

## Things to avoid

- Editing `<Version>` by hand (GitVersion owns it).
- Re-adding `pull_request:` triggers to deploy workflows.
- Introducing a package that doesn't support `net8.0`.
- Committing secrets — use `dotnet user-secrets` locally, GitHub Actions secrets in CI.
- `git push --force` to `main` or `develop`.
- Upgrading Starlight or Astro in isolation — they're pinned together with `zod`/`sitemap` overrides.
- Writing bare absolute links (`/foo/`) in docs markdown — they 404 under the GitHub Pages base path.

## Manual one-time setup (for reference)

These can't be automated from workflow YAML — flag them if the agent notices they're missing:

- **GitHub → Settings → Pages → Source: GitHub Actions**. Required for `deploy-site.yml` to deploy.
- **Branch protection on `main` and `develop`**: require the `Build & Test` status check to pass before merge.

## Quick-ref: common agent tasks

| Task                                         | Touch these files                                                                 |
|----------------------------------------------|-----------------------------------------------------------------------------------|
| Add a repository method                      | `lib/IBaseGraphRepository.cs`, `lib/BaseGraphRepository.cs`, `unit-tests/...`, docs under `site/src/content/docs/packages/repository.md` |
| Add a new contract type                      | `contracts/`, docs under `site/src/content/docs/packages/contracts.md`            |
| Add a new TFM                                | `lib/*.csproj`, `contracts/*.csproj`, `unit-tests/*.csproj`, `.github/workflows/build-test.yml`, `site/src/content/docs/getting-started.md`, `contributing.md`, `index.mdx` |
| Add a new docs page                          | New `.md`/`.mdx` under `site/src/content/docs/`, add to `sidebar` in `site/astro.config.mjs` |
| Add a new workflow                           | New file under `.github/workflows/`, and update `paths-ignore` of the deploy workflows if the new trigger path should be excluded from them |

## Sources of truth

- Runtime behaviour: the code in `lib/` and `contracts/`.
- Versioning rules: `GitVersion.yml`.
- CI topology: `.github/workflows/*.yml`.
- User-facing docs: `site/src/content/docs/`.
- Legacy intro: `README.md` (short — defers to the docs site for detail).

When any of the above disagree, trust the code and file a docs fix.
