# Changelog

Notable changes to the project. Newest first.

---

## 2026-07-28 — Milestone 0: Foundations (partially complete)

Branch: `milestone-0-foundations`. **Steps 1, 2 and 6 are done. Steps 3, 4 and 5 are blocked — no Unity installation exists on this machine.**

### Done
- **Added `.gitignore`** and untracked `HToW.exe` + `HToW_Data/` (~25 MB, ~96% of repo size). Left on disk; `output_log.txt` findings already preserved in `GameplayLoop.md` §4.
- **Removed ~40% of `Assets/`** — dead scripts, four broken prefabs, dead duplicate deck files, the abandoned `OldMethod` ScriptableObjects, a Dropbox conflict scene, three `Thumbs.db`, and three orphan folder `.meta`s. 53 files, all verified unreferenced first, all retained in git history.
- **`Assets/` now has zero orphan and zero missing `.meta` files**, which keeps GUIDs stable across the upgrade.
- **Rewrote `README.md`** to describe reality rather than a 2017 to-do list.
- Set repo-local git identity to match the existing commit author.

### Findings during the work
- **The numeric values in the `OldMethod` ScriptableObjects were successfully decoded** from the binary before deletion — full stat lines, not just the ability names. Recorded in `CardSystem.md` §4c. Two notable results: morale costs were roughly **halved** in the later balance pass (Fianna, Bondi and Huscarl exactly 2×), and **Bondi was heavily nerfed** (3 cost/12 HP → 4 cost/8 HP), which explains why it is now the strictly-dominated worst card in the game.
- **Bug C1 independently re-confirmed by a second method.** GUID analysis of every script against every scene and prefab shows `BattleController.cs` is attached to nothing — matching both the field-name evidence and the 2014 stack trace.
- Also confirmed dead by the same analysis: `CardAttributes.cs` and `MouseController.cs` appear in no scene (they are added at runtime via `AddComponent`, so they are live code and were kept).

### Blocked — needs Unity installed
- Force Text serialization (step 3), the Unity 6 upgrade (step 4), and the Build Settings fix (step 5). See `UnityUpgrade.md` §5.

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
