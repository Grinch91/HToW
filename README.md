# Hibernia: Tales of Warfare (HToW)

A single-player deckbuilding card battle game set in ancient and medieval Ireland.

Originally a final-year university project built in **Unity 4.3.4f1 (2014)**. Currently being revived.

---

## Status

**Not playable.** The project compiles and runs, but a battle cannot be completed — combat throws on the first attack, turns advance at frame rate, and most cards cannot be played at all.

| | Completion |
|---|---|
| Battle system | ~35% (partially implemented, several blocking bugs) |
| Game (campaigns, progression, deckbuilding, saving) | ~5% (essentially nonexistent) |

A full reverse-engineering pass was completed on 2026-07-28. **Everything known about this project is in [`Docs/`](Docs/)** — start there.

---

## Documentation

| Document | Contents |
|---|---|
| [Architecture](Docs/Architecture.md) | Structure, scenes, class map, data layer |
| [GameplayLoop](Docs/GameplayLoop.md) | What the game actually does vs. what it intended to |
| [CardSystem](Docs/CardSystem.md) | Card roster, stats, balance analysis, card lifecycle |
| [AI](Docs/AI.md) | Opponent logic and where it falls short |
| [KnownBugs](Docs/KnownBugs.md) | 6 critical, 12 major, 14 minor, 6 latent |
| [TechnicalDebt](Docs/TechnicalDebt.md) | Debt inventory with tradeoffs |
| [UnityUpgrade](Docs/UnityUpgrade.md) | 4.3.4 → Unity 6 assessment and migration plan |
| [Roadmap](Docs/Roadmap.md) | Nine milestones with difficulty, risk, dependencies |
| [GameDesign](Docs/GameDesign.md) | Design critique and recommendations |
| [Campaigns](Docs/Campaigns.md) | Campaign structure proposal |
| [HistoricalResearch](Docs/HistoricalResearch.md) | Period reference, fact/myth/fiction tagging |
| [Decisions](Docs/Decisions.md) | Decision log and open questions |
| [Changelog](Docs/Changelog.md) | Change history |

---

## The concept

Players progress through campaigns drawn from Irish history — Celtic Ireland, the Viking invasions, Brian Boru, the Norman invasion — building a deck as they go.

The distinctive mechanic is **morale**. You do not lose life when you are attacked; you lose morale when *your own units die*, in proportion to how valuable they were. A deck's total morale cost is effectively its life pool, which ties deckbuilding and survivability together — and reflects how early Irish warfare actually ended, with one side's will breaking rather than its army being destroyed.

---

## Building

**Requires Unity 4.3.4f1**, which will not reliably install on modern Windows. Migration to Unity 6 LTS is planned as part of Milestone 0 — see [UnityUpgrade.md](Docs/UnityUpgrade.md).

The playable scene is `Assets/Scenes/Battle.unity`. Note that Build Settings currently references two scenes that no longer exist.

---

## Working on this

See [Roadmap.md](Docs/Roadmap.md). Work is milestone-ordered and the ordering matters — fixing combat before fixing the turn system is wasted effort.

The priority is **a playable game before a polished one**. Milestones 0–4 exist to get one complete, satisfying battle working end to end; nothing else is worth building until that is true.

Four open design questions are logged in [Decisions.md](Docs/Decisions.md) and need answers before Milestones 4 and 7.
