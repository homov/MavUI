# Mav documentation

Project documentation and verification screenshots live here.

| Document | Contents |
|---|---|
| [AGENTS.md](AGENTS.md) | Library rules, API catalogue and repository map |
| [HANDOFF.md](HANDOFF.md) | Current session status, decisions and next steps |
| [PLAN.md](PLAN.md) | Roadmap and stage history |
| [OPTIMIZATION_PLAN.md](OPTIMIZATION_PLAN.md) | Audit-driven performance work, round 1 (stages 1–7) |
| [OPTIMIZATION_PLAN_2.md](OPTIMIZATION_PLAN_2.md) | Audit-driven performance work, round 2 (stages 1–8 closed) |
| [../CHANGELOG.md](../CHANGELOG.md) | Versions, notable changes and versioning policy |
| [demo/README.md](demo/README.md) | Desktop demo: run, smoke test, mobile preview |
| [gallery/README.md](gallery/README.md) | Control Gallery: groups, hosts and screenshots |
| [gallery/](gallery/) | Gallery screenshots (desktop, web, Android) |
| [handoff/](handoff/) | Historical verification screenshots |

## Repository layout

- `MavUI/` — the library.
- `Tests/` — headless xUnit tests.
- `CHANGELOG.md` — versions and notable changes.
- `samples/` — runnable hosts:
  - `Demo/` and `Demo.Android/`
  - `Gallery/`, `Gallery.Browser/` and `Gallery.Android/`
- `MavUI.sln` — solution with the library, tests and all sample hosts.
- `tools/` — Linux/Windows GUI automation helpers.
- `ref/` — external Avalonia source/reference checkout.

## Quick start

```bash
dotnet build MavUI.sln
dotnet test Tests/MavUI.Tests.csproj
dotnet run --project samples/Gallery/Mav.Gallery.csproj
```
