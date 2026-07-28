# Design & Technical Decisions

A running log. Each entry records **what was decided, when, and why** — so that in another decade the reasoning survives even if the memory doesn't.

Format: `## D-nn — Title` · **Date** · **Status** (Proposed / Accepted / Superseded) · **Decision** · **Why** · **Consequences**.

---

## D-01 — Treat this as archaeology before development
**2026-07-28 · Accepted**

**Decision:** complete a full reverse-engineering pass and write it down before changing any gameplay code.

**Why:** the project had zero documentation, 12 empty doc stubs, three commits, and binary-serialized assets. Implementation details had been forgotten. Changing code first would have meant guessing.

**Consequences:** the investigation found several things that no amount of code-reading alone would have surfaced — the abandoned `CardInfo`/ability system, the deleted `CardLibrary` folder, and a real 2014 stack trace in the committed build log. `Docs/` is now the source of truth.

**Amendment 2026-07-28:** one finding derived from binary string extraction (bug C5, a supposedly missing `Battle Chariot` tag) turned out to be **false** once text serialization made `TagManager.asset` directly readable. Binary-extraction evidence is provisional; absence of a string is weak evidence. See `KnownBugs.md` C5.

---

## D-02 — Upgrade Unity early, in one jump, to Unity 6 LTS
**2026-07-28 · Proposed**

**Decision:** go 4.3.4 → Unity 6 LTS directly. Do not step through intermediate versions.

**Why:** only ~4 hard compile errors exist (`gameObject.audio`, `Application.LoadLevel` ×3), no third-party packages, no shaders, no animation controllers. Incremental upgrading is advice for large dependency-heavy projects; here it multiplies work for no benefit. Unity 4.3.4 also likely will not run on Windows 11, which makes staying put impractical. And the code the upgrade forces you to touch (menus, scene loading, sprite loading) is the same code the gameplay work must rewrite anyway — doing both at once avoids paying twice.

**Consequences:** expect board layout to break visually (hardcoded world coordinates + changed sprite import defaults). Accepted, because that layout code is being replaced regardless. Full plan in `UnityUpgrade.md`.

---

## D-03 — Force Text serialization before anything else
**2026-07-28 · Proposed · amended same day**

**Amendment:** no Unity installation exists on this machine, so Unity 4.3.4 cannot perform the conversion and this can no longer happen *before* the upgrade. Force Text becomes the first action taken inside Unity 6 instead. Editing the binary `EditorSettings.asset` by hand was considered and **rejected** — the sole benefit would have been a reviewable upgrade diff, and that is lost regardless, since opening in Unity 6 converts and upgrades in one pass. See `UnityUpgrade.md` §5 Phase 1.

**Decision:** switch asset serialization from Force Binary to Force Text, as an isolated commit.

**Why:** binary assets cannot be diffed, merged, or reviewed. This investigation had to extract raw byte strings to learn the scene hierarchy. A Dropbox sync conflict in 2014 silently produced a corrupt duplicate scene that could never have been reconciled.

**Consequences:** one large mechanical commit. Every scene/prefab change afterwards is reviewable. Must happen *before* the Unity upgrade so the upgrade itself produces a readable diff.

---

## D-04 — Fix card identity as one refactor, not six patches
**2026-07-28 · Proposed**

**Decision:** bugs C1, C2, M3, M4, M5 and F3 will be fixed together in Milestone 2 by replacing string-based card identity and splitting `Deck` into `CardPile` / `Board`.

**Why:** they are all symptoms of one root cause — a card is identified by its name string, and the in-play area is modelled as a draw pile. Patching them individually means writing six workarounds and then deleting all six.

**Consequences:** deliberately violates "small reviewable steps" at the *milestone* level. Mitigation: implement as a sequence of individually-compiling commits. This is the highest-risk milestone in the roadmap.

---

## D-05 — Restore the abandoned ScriptableObject data model
**2026-07-28 · Proposed**

**Decision:** replace the `.txt` deck files and the 9-branch `if` chain in `Deck.AddToDeck()` with `CardData`/`DeckData` ScriptableObjects, reinstating the 2014 `CardInfo` design including `ctype` and `ceffect`.

**Why:** the original instinct was right; the assets survive in `Resources/*/OldMethod/` even though the C# was deleted. Card stats currently live in source code, so a balance change is a code change. `Application.dataPath` file IO also breaks on any non-desktop platform.

**Consequences:** `CardData` (immutable asset) must be kept separate from `CardInstance` (mutable runtime state). The current `CardDef` conflates them, which is why damage persists across a card's lifetime and why removal is done by name-matching.

---

## D-06 — Delete before upgrading
**2026-07-28 · Proposed**

**Decision:** remove dead code, broken prefabs, Dropbox conflict artifacts and committed build output *before* touching Unity versions.

**Why:** ~40% of `Assets/` is dead. One of the three files with hard compile errors (`MusicManager.cs`) is dead code — deleting it removes a third of the upgrade's errors for free. Fewer broken references to diagnose afterwards.

**Consequences:** `HToW.exe` and `HToW_Data/` leave the working tree but remain in git history. Acceptable for a personal project — a history rewrite is not worth the risk. `output_log.txt`'s findings are preserved in `GameplayLoop.md` §4 first.

---

## D-07 — Carry the four recovered abilities on `CardData` from the start
**2026-07-28 · Accepted** *(answers Q-03)*

**Decision:** `CardAbility` (`Volley`, `United`, `Berserker`, `Bloodrush`) is a first-class field on `CardData` from day one, and the six cards whose 2014 `ceffect` values were recovered carry them. **None are implemented yet.**

**Why:** retrofitting an ability system into an established card model is far more expensive than leaving room for one. The keywords also do real design work for free — `Volley`/`United` read Celtic-defensive, `Berserker`/`Bloodrush` read Norse-aggressive, so faction identity is already latent in the data.

**Consequences:** the three mercenary cards have **no** abilities, because none were ever authored for them — the 2014 project had no `CardInfo` asset for MercArcher, Sellsword or Captain. Rather than invent values and pass them off as recovered, they are `None` and flagged as an open design task. `Docs/HistoricalResearch.md` §4.1 argues the mercenaries should be replaced outright with better-attested units (gallowglass, hired Norse fleets), so authoring abilities for them now would likely be wasted.

Also captured on `CardData`: `Faction`, `CardType` (recovered `ctype`), and `Historicity` — the last so that historical fact, mythology and invented content stay visibly distinguishable, as the project brief requires.

---

## D-08 — Milestone 2 migrates representation, not values
**2026-07-28 · Accepted**

**Decision:** the `CardData` and `DeckData` assets reproduce the **currently live** stats from `Deck.cs` and the exact deck compositions from `celtic.txt`/`viking.txt`. No balance changes.

**Why:** conflating a data-model refactor with a rebalance would make it impossible to tell which change caused a behavioural difference. Verified faithful: the generated decks total **21** and **72** morale, matching the Phase 1 analysis in `GameplayLoop.md` §6 exactly.

**Consequences:** the known balance faults ride along untouched — Celtic still cannot lose (M12), Bondi is still dominated by Ceithern, Fianna is still overpowered. All are Milestone 4. The pre-rebalance 2014 values recovered in `CardSystem.md` §4c are the natural starting point for that pass.

---

## OPEN QUESTIONS — need your decision

These change what gets built. They do not block Milestone 0, which is why the roadmap starts there.

### Q-01 — Combat retaliation? *(blocks Milestone 4)*
Should a defender deal damage back to its attacker? Currently it does not, which makes attacking free and removes most tactical decision-making. **My recommendation: yes** — it's ~5 lines and it's the cheapest large increase in depth available. Tradeoff: can cause board stalls. See `GameDesign.md` §3.

### Q-02 — Linear campaigns or run-based structure? *(blocks Milestone 7)*
The original vision is a linear historical campaign per period. A Hand of Fate–style branching run would give replayability for free, make deckbuilding happen during play, and turn historical events into choice nodes. **My recommendation: run-based**, but this trades away the ability to tell a specific story beat by beat. Genuinely your call — it depends whether replayability or storytelling matters more to you. See `GameDesign.md` §7.

### ~~Q-03 — Reinstate the four abilities?~~ → **ANSWERED: yes** (2026-07-28)
See D-07 below.

### Q-04 — Is the target a finishable personal project or an open-ended one?
This changes scope advice throughout. Four campaigns is a multi-year solo undertaking. One polished campaign is achievable. **Recommendation: build Celtic Ireland as a complete vertical slice**, then decide.
