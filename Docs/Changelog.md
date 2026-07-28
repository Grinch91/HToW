# Changelog

Notable changes to the project. Newest first.

---

## 2026-07-28 — Milestone 2, slice 2: rewiring ✅ M2 COMPLETE

**Card identity is now a reference, not a string.** The old model is gone.

### Added
- **`CardZone`** — replaces `Deck`. A place cards can be, plus the anchor they lay out around. Cards move by reference, which is what makes bug C2 impossible. `DrawTop()` returns null on an empty pile instead of indexing it (C6). `Shuffle()` is a correct Fisher–Yates (N2). `LayOut()` centres cards on the zone rather than deriving x from the draw pile's remaining count (N6).
- **`CardView`** — replaces `CardAttributes`, `MouseController` **and** `BattleController`. Splitting click handling across two controllers by zone is what put selection state on cards but read it from their container (C1). A view now just reports the click.
- **`BattleSelection`** — one place holding the chosen attacker and target, with the ability to clear and to forget a destroyed card. The real fix for C1.

### Changed
- `Game.cs` rewritten onto the new model. **The four near-identical card-construction methods collapse into one `CreateView`.**
- Cards are no longer tagged with their own name, and `FindGameObjectWithTag` is gone — so destroying a card can no longer delete the attacker's own copy (M4) or an arbitrary duplicate (M5).
- **Supply is now spent** when a card is played, and checked against the real player (C4).
- **The AI plays from its hand and pays supply**, like the player. It previously scanned its draw pile and never removed what it played (M1). Target choice is now a scoring function that weights `MoraleCost` — the actual win condition, which the old heuristics ignored (M2).
- Attacks resolve by clicking your card then an enemy card.

### Removed
`Deck.cs`, `CardAttributes.cs` (and `CardDef`), `MouseController.cs`, `BattleController.cs`, `Assets/celtic.txt`, `Assets/viking.txt`.

### Scene surgery
Four of the six zones were instances of `Deck.prefab`, so the script swap was applied to the prefab plus two loose scene components — 7 references in total. Stale serialized fields from the old `Game` class were stripped.

### Verified
- Compile: zero errors. Tests: **17/17 pass**.
- Scene load-check through `AssetDatabase`/`EditorSceneManager` reported **`problems=0`**: all six zone fields resolve to the right GameObjects (`playerDeck`→`Deck-Player`, `aiHand`→`Hand-Enemy`, …), exactly six `CardZone` components present, **no missing scripts anywhere in the scene**, all `Game` children found, deck assets and card-back sprite loadable.

### Known gaps
- **No `CardView` prefab.** Views are still built procedurally, but in *one* place instead of four. A prefab is the natural follow-up and would make card visuals designer-editable.
- Combat is still one-directional — a design question (Q-01), not an oversight.
- Morale is still overwritten to 1 in `BaseCharacter` defaults and there is still no HUD (M8) — Milestone 3.

---

## 2026-07-28 — Milestone 2, slice 1: the card data model

Branch: `milestone-2-card-identity`. **Additive only — nothing existing was modified, so the game behaves exactly as it did after Milestone 1.**

### Added — code
- **`CardData : ScriptableObject`** — the immutable definition of a card, authored in the Inspector. Restores the abandoned 2014 design, including its `ctype` and `ceffect` fields.
- **`CardInstance`** — plain C# runtime state (`CurrentHp`, `HasAttacked`, `Owner`). **The definition/state split is the whole point**: conflating them in the old `CardDef` is the root cause of C2, M4 and M5. Identity is now reference identity, so two Raiders are two distinct objects.
- **`DeckData : ScriptableObject`** — a named deck list with counts, replacing `File.ReadAllLines(Application.dataPath + ...)`. Exposes `TotalMoraleCost`, since a deck's combined morale cost is effectively its life pool and needs to be visible while authoring (bug M12).
- **`CardEnums.cs`** — `Faction`, `CardType`, `CardAbility`, `Historicity`.

### Added — assets
- **9 `CardData` assets** with the four recovered abilities attached (Volley, United, Berserker, Bloodrush). See Decisions D-07.
- **2 `DeckData` assets**, Celtic and Viking.

### Verified
- Unity batch compile: exit 0, zero errors.
- Assets hand-authored as YAML then **loaded back through `AssetDatabase`** to prove every reference resolves — sprite links, ability arrays and enum values all correct.
- `DeckData.BuildCards()` produces 5 and 11 `CardInstance`s totalling **21** and **72** morale — matching the Phase 1 figures in `GameplayLoop.md` §6 exactly, confirming a faithful migration (Decisions D-08).

### Still to do in Milestone 2
Card zones (pile vs. board), a `CardView` prefab and factory replacing the four duplicated construction methods, a selection service replacing `BattleController` (bug C1), and rewiring `Game.cs` onto the new model. Until then `Deck.cs`, `CardDef`, `CardAttributes` and the `.txt` deck files remain live and in use.

---

## 2026-07-28 — Milestone 1: A real turn ✅ COMPLETE

Branch: `milestone-1-turns`. **The defining defect of the project is fixed: turns no longer advance on their own.**

### Added
- **`Assets/Scripts/Battle/TurnController.cs`** — owns turn order, phase and turn number. A plain C# class, not a MonoBehaviour: it has no per-frame behaviour, needs no scene wiring, and can be tested without Unity. First extraction from the `Game.cs` God class.
- Phases are `Setup → TurnStart → Main → TurnEnd`, and **`Main` cannot self-advance**. Only `RequestEndTurn()` leaves it. The fix is structural rather than a patch: no amount of calling `Advance()` can move a turn on.

### Changed in `Game.cs`
- `Update()` no longer runs a player turn and an AI turn every frame. It drains ready transitions and parks in `Main`.
- **The End Turn button works.** It previously set a `buttonPushed` field that no code ever read.
- Turn start now draws, grants supply, **resets `HasAttacked`**, and shows the turn banner.
- Attacks resolve at turn end, iterating a **snapshot** of the attacker list.
- `GameState` (nine states, two never used, one tautological) replaced by `TurnPhase` + `Side`. `BuildDeck`, `CheckIsDeckEmpty`, `Battle`, `AttackingCard`, `TargetCard` and `Attack` now take an explicit `Side` instead of reading the state machine — removing the "state machine as argument channel" anti-pattern.
- Removed dead fields: `playerFirst`, `playerTurnOver`, `justChanged`, `buttonPushed`, `n`, `timer`, `timerMax`, `messageTimer`, `seperatingCardsAI/Player`, `FlyTime`, `Buttons`.
- Win/lose messages no longer depend on a timer that never reset.

### Verified
- Unity batch compile: exit code 0, **zero errors**. The four remaining warnings are pre-existing; one is the compiler independently confirming bug C6.
- **19/19 behavioural assertions pass** against `TurnController`, compiled standalone with Unity's bundled .NET 8 SDK — possible only because the class has no Unity dependency. The headline assertion: *10,000 frames with no player input do not advance the turn*, against a 2014 baseline of 716 turn cycles in roughly twelve seconds.

### Deliberately not fixed
- **C1 is guarded, not fixed.** `AttackingCard`/`TargetCard` no longer dereference a null `BattleController`, so combat stops throwing — but the player still cannot select an attacker or target, so player attacks do not resolve. A stopgap, clearly marked in the source, so that the turn system can be observed at all. The real fix is Milestone 2's selection service.
- C2 (playing the wrong card), C4 (supply never spent), M1 (AI plays from its deck) are untouched — Milestones 2 and 3.

---

## 2026-07-28 — Milestone 0: Foundations ✅ COMPLETE

Branch: `milestone-0-foundations`. All six steps done.

### Unity 6 upgrade
- **Upgraded 4.3.4f1 → 6000.5.5f1.** Asset serialization is now **Force Text** (`m_SerializationMode: 2`); every scene, prefab, `.meta` and ProjectSettings asset is YAML. Assets are diffable and mergeable for the first time in the project's history.
- **Replaced `Application.LoadLevel` with `SceneManager.LoadScene`** (3 call sites, `Game.cs` ×2 and `UI.cs` ×1) — applied *before* the first editor open so the initial import was clean. This was the complete set of hard compile errors, exactly as `UnityUpgrade.md` predicted; deleting `MusicManager.cs` in the cleanup step had already removed the fourth.
- **Fixed Build Settings** — removed entries for the deleted `BattleMenu.unity` and `CardMenu.unity` (both had null GUIDs) and the duplicate disabled `Battle.unity` entry. Now two scenes, both enabled.
- **Verified by batch-mode compile**: exit code 0, zero `error CS`, zero `warning CS`. `Assembly-CSharp.dll` rebuilt containing all 11 classes, referencing `UnityEngine.SceneManagement`, with no `LoadLevel` remnant.

### Correction — bug C5 retracted
**`Battle Chariot` is present in `TagManager.asset`.** The Phase 1 claim that it was missing came from raw byte-string extraction of the binary asset, which silently dropped the space-containing tag. With text serialization the file is directly readable and lists all ten tags. Playing a Battle Chariot does not throw.

Recorded as a methodology lesson in `KnownBugs.md` C5 and `Decisions.md` D-01: binary-extraction evidence is provisional, and *absence* of a string is weak evidence. Other **[STATIC]** findings that depended on extraction rather than on C# source should be re-verified now that everything is text.

### Repo hygiene
- **Added `.gitignore`** and untracked `HToW.exe` + `HToW_Data/` (~25 MB, ~96% of repo size). Left on disk; `output_log.txt` findings already preserved in `GameplayLoop.md` §4.
- **Removed ~40% of `Assets/`** — dead scripts, four broken prefabs, dead duplicate deck files, the abandoned `OldMethod` ScriptableObjects, a Dropbox conflict scene, three `Thumbs.db`, and three orphan folder `.meta`s. 53 files, all verified unreferenced first, all retained in git history. Tracked files: 197 → 131.
- **`Assets/` now has zero orphan and zero missing `.meta` files**, which keeps GUIDs stable across the upgrade.
- **Rewrote `README.md`** to describe reality rather than a 2017 to-do list.
- Set repo-local git identity to match the existing commit author.

### Findings during the work
- **The numeric values in the `OldMethod` ScriptableObjects were successfully decoded** from the binary before deletion — full stat lines, not just the ability names. Recorded in `CardSystem.md` §4c. Two notable results: morale costs were roughly **halved** in the later balance pass (Fianna, Bondi and Huscarl exactly 2×), and **Bondi was heavily nerfed** (3 cost/12 HP → 4 cost/8 HP), which explains why it is now the strictly-dominated worst card in the game.
- **Bug C1 independently re-confirmed by a second method.** GUID analysis of every script against every scene and prefab shows `BattleController.cs` is attached to nothing — matching both the field-name evidence and the 2014 stack trace.
- Also confirmed dead by the same analysis: `CardAttributes.cs` and `MouseController.cs` appear in no scene (they are added at runtime via `AddComponent`, so they are live code and were kept).

### Deliberately not done
- The game's behaviour is unchanged. Milestone 0's success criterion was **"equally broken, not differently broken"**, and it holds: turns still run at frame rate, combat still throws on the first attack, cards costing 3+ are still unplayable. Those are Milestones 1–3.
- Left in place despite being listed as bug N1: the six `new Deck()` field initialisers. Removing them is behaviour-neutral in principle, but it changes a "fake null" into a real null if scene deserialization ever fails — a different failure mode, which Milestone 0 explicitly avoids introducing.

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
- ~~`Battle Chariot` has no Unity tag~~ — **later retracted; this was a false positive from binary string extraction. See the Milestone 0 entry.**
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
