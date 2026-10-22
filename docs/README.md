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
| [gallery/README.md](gallery/README.md) | Control Gallery: groups, hosts and screenshots |
| [gallery/](gallery/) | Gallery screenshots (desktop, web, Android) |
| [handoff/](handoff/) | Historical verification screenshots |

## Repository layout

- `MavUI/` — the library.
- `Tests/` — headless xUnit tests.
- `CHANGELOG.md` — versions and notable changes.
- `samples/ControlsGallery/` — the control gallery: shared library plus Desktop, Android and Browser heads.
- `MavUI.sln` — solution with the library, tests and all sample hosts.
- `tools/` — Linux/Windows GUI automation helpers.

## Quick start

```bash
dotnet build MavUI.sln
dotnet test Tests/MavUI.Tests.csproj
dotnet run --project samples/ControlsGallery/Desktop/ControlsGallery.Desktop.csproj
```
