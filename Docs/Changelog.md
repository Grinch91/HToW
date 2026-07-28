# Changelog

Notable changes to the project. Newest first.

---

## 2026-07-28 — Phase 1: reverse-engineering complete

**No code changed.** Investigation and documentation only.

### Added
- `Docs/Architecture.md` — project structure, scene hierarchies, class map, data layer, build drift.
- `Docs/GameplayLoop.md` — actual vs. intended loop, verified against the 2014 runtime log.
- `Docs/CardSystem.md` — full card roster, stats, balance analysis, card lifecycle, art/code cross-reference.
- `Docs/AI.md` — analysis of the three AI methods and a recommended direction.
- `Docs/KnownBugs.md` — 6 critical, 12 major, 14 minor, 6 latent, with root causes and fix ordering.
- `Docs/TechnicalDebt.md` — debt inventory with tradeoffs and verdicts.
- `Docs/UnityUpgrade.md` — 4.3.4 → Unity 6 assessment, migration plan, risk register.
- `Docs/Roadmap.md` — 9 milestones with difficulty, risk, and dependencies.
- `Docs/GameDesign.md` — design critique and recommendations.
- `Docs/Campaigns.md` — campaign structure proposal (pending decision Q-02).
- `Docs/HistoricalResearch.md` — period reference, fact/myth/fiction tagging, accuracy audit.
- `Docs/Decisions.md` — decision log and open questions.

### Key findings
- Project is **Unity 4.3.4f1** (2014), binary-serialized, 16 C# files / ~35 KB.
- `Game.cs` (871 lines) is ~60% of all gameplay code.
- **Turns run at frame rate** — 716 full turn cycles recorded in ~12 seconds of real play.
- **Combat crashes on first use** — `NullReferenceException` in `Game.AttackingCard`, confirmed by a stack trace in the committed 2014 build log.
- **Playing a card plays the wrong card** — the active area is modelled as a draw pile.
- **Cards costing 3+ can never be played** — the affordability check reads a freshly-constructed `Player`.
- **`Battle Chariot` has no Unity tag** — playing one throws `UnityException`.
- An **abandoned ScriptableObject card system** was recovered from orphaned assets, including four designed-but-unimplemented abilities: `Volley`, `United`, `Berserker`, `Bloodrush`.
- Scene objects exist for a **HUD** and **Generals** with zero backing code.
- The committed `HToW.exe` was built from **a different revision than the committed source**.
- ~96% of repo size is committed build output; there is no `.gitignore`.

### Notes
- Completion estimate refined: **~35% of a battle, ~5% of a game.** The original 35% estimate was accurate for combat specifically.
- Four open questions recorded in `Docs/Decisions.md` await a decision. None block Milestone 0.

---

## 2017-03-06 — Repository created
Three commits (`215cd97`, `c8945e8`, `7a6a835`) uploading the existing 2014 project and a README noting two urgent items: *"Fix turn system"* and *"Implement AI"*. Both assessments were correct and remain open.

## 2014 — Original development
Final-year university project. Built in Unity 4.3.4f1. Development appears to have stopped around April 2014 (latest build log timestamp).
