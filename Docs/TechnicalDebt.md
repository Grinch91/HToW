# Technical Debt

**Compiled 2026-07-28.** Ordered by leverage, not severity. Tradeoffs are stated rather than assumed — see "Verdict" on each item.

---

## 1. `Game.cs` is a God class — 871 lines, ~60% of all gameplay code

It owns the state machine, turn order, deck construction, GameObject instantiation, sprite loading, screen-position maths, AI card selection, AI targeting, combat resolution, morale bookkeeping, win detection, and UI message toggling.

**Cost today:** every bug touches it; nothing can be unit-tested; two features cannot be worked on independently.

**Tradeoff.** The instinct is to shatter it into `TurnManager` / `CombatResolver` / `BoardView` / `AIController` immediately. I'd advise against doing that first. The class is badly *structured* but it is small in absolute terms, and a big-bang split of code you don't yet trust means debugging a new architecture and old logic simultaneously.

**Verdict:** split it **incrementally, behind the milestones**, extracting one responsibility at a time only when a milestone needs to touch it. Extract in this order: `TurnController` (Milestone 1) → card factory / `BoardView` (Milestone 2) → `CombatResolver` (Milestone 3) → `AIController` (Milestone 5). Each extraction is testable on its own.

---

## 2. Card identity is a string

`CardDef.Name` is used simultaneously as: display name, GameObject name, **Unity tag**, the key for `FindGameObjectWithTag` destruction, the key for list removal by name, and the AI's selection key.

**Cost today:** directly causes bugs M4 (destroys the wrong card), M5 (removes the wrong duplicate), and F3. It also means every card name must be maintained as a Unity tag by hand — currently correct, but a rename or a new card silently breaks card creation.

**Verdict:** highest-leverage fix in the project. Replace with object references (`CardInstance`) and delete the tag usage entirely. Six known bugs collapse into one refactor. Non-negotiable before any content work.

---

## 3. `Deck` does three incompatible jobs

`Deck : MonoBehaviour` is used for the draw pile, the hand, **and** the active board area. Piles are FIFO queues (append/deal-from-front); the board is an unordered set. Using one class for both directly causes bug C2 — playing a card spawns a different card.

**Verdict:** split into `CardPile` (draw/discard, ordered) and `Board` (in-play, a set). Also make `Deck` a plain class rather than a `MonoBehaviour` — it has no per-frame behaviour, and it is only a component so it can be dragged into the Inspector. That also removes the six `new Deck()` warnings (bug N1).

---

## 4. Four near-identical card-construction methods

`AddToPlayerHand`, `AddToEnemyHand`, `AddToActivePlayer`, `AddToActiveAi` are ~30 lines each, differing only in parent transform, Y coordinate, which controller is attached, and whether the front or back sprite is used. ~120 lines that should be ~30.

**Verdict:** collapse into one `CardView` **prefab** plus a small factory. This also fixes bug N6 (cards stacking once the deck empties), removes the hardcoded layout magic numbers, and makes card visuals designer-editable for the first time. Do this in Milestone 2, alongside item 2 — they touch the same code.

---

## 5. Binary serialization

`ProjectSettings` has asset serialization set to **Force Binary**. Scenes, prefabs and `.asset` files are opaque blobs.

**Cost:** no diffs, no merges, no code review of scene changes, and — as this investigation showed — no way to inspect the project without byte-level string extraction. It also means a Dropbox conflict silently produced a corrupt duplicate scene (bug N12) with no way to reconcile it.

**Verdict:** switch to **Force Text** as one of the very first actions, as its own commit. Zero risk, permanent benefit, and it must happen *before* the Unity upgrade so the upgrade's changes are reviewable.

---

## 6. No `.gitignore`; build output is committed

`HToW.exe` (11 MB) and `HToW_Data/` (14 MB) are tracked. They are ~96% of the repo, and the `.exe` was built from **a different revision than the committed source** (`Architecture.md` §10) — so it is actively misleading.

**Verdict:** add Unity's standard `.gitignore`; remove `HToW.exe` and `HToW_Data/` from tracking. **First extract `HToW_Data/output_log.txt`** — it is the only recording of the game actually running and its findings are already captured in `GameplayLoop.md` §4. Note the files stay in git history; that's acceptable for a personal project (a history rewrite isn't worth the risk here).

---

## 7. Dead code and abandoned experiments

| Item | Disposition |
|---|---|
| `Assets/Scripts/Menus/OldUnusedCode/` (3 files) | delete — attached to nothing, reference deleted scenes |
| `MusicManager.cs` | delete — placeholder (`GameObject.Find("song name here")`), attached to nothing, won't compile in Unity 5+ |
| `MyUnitySingleton.cs` | delete — a generic singleton template, used by nothing |
| `Assets/Resources/{celtic,viking}.txt` | delete — dead duplicates with *different contents* from the live files |
| `Assets/Scenes/Battle (Cielo's conflicted copy...).unity` | delete — Dropbox conflict artifact |
| `HToW_Data/output_log (Cielo's conflicted copy...).txt` | delete with `HToW_Data/` |
| `Thumbs.db` × 3 | delete — Explorer junk inside `Assets/` |
| `Assets/Prefabs/{CelticDeck,Enemy}.prefab` | delete — empty/broken, contain no components |
| `Assets/Prefabs/Player.prefab` | delete — serialized against a `_startMorale`/`_startSupply` script that no longer exists |
| `Assets/Prefabs/BattleScripts.prefab` | delete — contains nothing but a transform |
| `GameState.Pending`, `GameState.Pause` | delete — never assigned or read |
| `FlyTime`, `seperatingCardsAI/Player`, `userlist`, `enemylist`, `buttonPushed`, `justChanged`, `runOnce` | delete — written but never meaningfully read |
| `Assets/Resources/*/OldMethod/*.asset` (9 files) | **delete only after** confirming `CardSystem.md` §4c captured every value — the C# classes are gone, so these are unloadable |
| `Assets/Data.meta`, `Scripts/Battles.meta`, `Cards/CardLibrary.meta` | delete — orphan folder metas |

Roughly 40% of the files in `Assets/` are dead. Clearing them is low-risk and makes everything afterwards easier to reason about. Do it as one clearly-labelled commit so it can be reverted wholesale.

---

## 8. Unity anti-patterns

| Pattern | Where | Why it matters |
|---|---|---|
| Logic in `Update()` with no gating | `Game.Update()` | root cause of bug C3 |
| `new` on a `MonoBehaviour` | six `public Deck` field initialisers | warning per field; the object is a non-functional orphan |
| `GameObject.Find` by string | `MouseController`, `BattleController`, `MusicManager` | called **per click**; slow, silent-null, breaks on rename |
| `FindGameObjectWithTag` for identity | `Attack()` | destroys an arbitrary card with a matching name (bug M4) |
| `Resources.Load` by string, per card | all four factory methods | no compile-time checking; `Resources/` is loaded wholesale into the build |
| 3D `BoxCollider` on 2D sprites | all four factory methods | works by accident; `Battle.unity` also has a stray `CircleCollider2D` |
| `OnGUI` immediate-mode UI | `UI.cs` and the three dead menu files | allocates every frame; effectively deprecated; hardcoded pixel coordinates don't scale |
| `Application.LoadLevel` | `UI.cs`, `Game.cs` | removed in Unity 5.3+ → `SceneManager.LoadScene` |
| Public mutable fields everywhere | `Game.cs` | Inspector-editable into invalid states |
| `Debug.Log` as the only diagnostics | ~60 call sites | one 37k-line log for 12 seconds of play; a real cost at frame rate |
| File IO via `Application.dataPath` | `Deck.Load` | breaks on non-desktop platforms; deck data is user-editable in a shipped build |

---

## 9. Correctness landmines that are not yet bugs

- `if (intValue != null)` — appears in `Deck.Deal()` and `InitialSetUp()`. Always true. Compiles, does nothing, hides an empty-collection crash (bug C6).
- `Deck.Shuffle()` — biased. `random.Next(deck.Count)` should be `random.Next(i, deck.Count)`.
- `m_state` used as an argument-passing channel — `Start()` sets the state machine to `PlayerTurn`/`AITurn` purely to tell `BuildDeck()` which deck to build. Any future code that reacts to state changes will fire spuriously during initialisation.
- Mutating a collection while a `foreach` over it is in scope (`Attack()`) — survives today only by luck (`break` immediately after `Remove`). The first combat feature added will break it (bug F1).
- `HasAttacked` never reset (bug F2) — invisible now, becomes "every unit attacks once per match" the moment turns are fixed.

---

## 10. Missing engineering infrastructure

None of the following exist: `.gitignore`, tests, assembly definitions, a `README` that reflects reality, CI, or any documentation (the `Docs/` folder was 12 empty files).

**Verdict:** add `.gitignore` immediately. Add **tests once the data model is extracted** — the moment combat resolution and AI scoring are plain C# rather than `MonoBehaviour` code, they become directly unit-testable, and that is a large part of the argument for doing that refactor. Assembly definitions and CI are premature for a solo project of this size.

---

## Priority summary

| # | Item | Effort | Risk | Leverage |
|---|---|---|---|---|
| 5 | Force Text serialization | trivial | none | very high |
| 6 | `.gitignore`, untrack build output | trivial | none | high |
| 7 | Delete dead code and assets | small | low | high |
| 2 | Card identity: strings → references | medium | medium | **very high** — fixes 6 bugs |
| 3 | Split `Deck` into pile vs. board | medium | medium | very high — fixes bug C2 |
| 4 | One card prefab + factory | medium | low | high |
| 1 | Decompose `Game.cs` | large | medium | high — but **incrementally** |
| 8 | Replace `OnGUI`, `LoadLevel`, `Resources.Load` | medium | low | forced by the Unity upgrade anyway |

Items 5, 6 and 7 are pure preparation and can all land before any behaviour changes.
