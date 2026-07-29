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

## D-09 — Combat retaliation, with Volley as its counterweight
**2026-07-28 · Accepted** *(answers Q-01)*

**Decision:** when A attacks B, B deals its damage back to A. A destroyed defender still strikes back as it falls. `Volley` is implemented and exempts its bearer from retaliation.

**Why:** without retaliation, attacking was free, so every turn reduced to "attack with everything" and there were no interesting decisions — the project's largest *design* problem, bigger than any bug. Retaliation turns every attack into a trade to evaluate, and because morale is only lost when your own cards die, the answer depends on how each deck is built. That is where depth comes from.

Letting a dying defender still hit back is deliberate: otherwise "kill it first" removes all risk and the mechanic collapses.

**Consequences:** `Volley` had to be implemented alongside it — the only ability so far. Battle Chariot (4 hp, 2 damage) could otherwise never attack anything and survive, making a starter card unplayable. The other three abilities remain unimplemented. Known risk from `GameDesign.md` §3: retaliation can cause board stalls. Watch for it in playtesting; more stall-breaking abilities are the intended remedy.

---

## D-10 — Minimal, traceable balance changes only
**2026-07-28 · Accepted**

**Decision:** four card changes, each fixing one specific documented fault, plus a deck-size increase to 20 cards per side.

| Change | Fault it fixes |
|---|---|
| Fianna cost 4 → 5 | strictly the best card in the game |
| Bondi hp 8 → 10 | strictly dominated by Ceithern (same statline, lower cost) |
| Huscarl cost 6 → 5 | 6-cost cards unreachable in a realistic game |
| Captain cost 6 → 5 | as above |

**Why:** a first balance pass should be small and traceable, so a playtest can attribute any change in feel. Sellsword's 4 hp was left alone deliberately — retaliation already corrects it, since a glass cannon now dies to whatever it hits.

**Consequences:** Celtic is now 20 cards / 107 morale and Viking 20 / 117, against 30 starting morale — so **both sides can now lose**, which was not previously true of Celtic (bug M12). Roughly 5–6 card deaths decide a match. These numbers are a starting point for playtesting, not a finished curve.

---

## D-11 — Linear historical campaigns, because teaching is the goal
**2026-07-29 · Accepted** *(answers Q-02)*

**Decision:** campaigns are **linear, authored sequences of historical encounters**, not randomised runs. Milestone 7 builds a fixed chapter structure per period, starting with Celtic Ireland.

**Why (the author's reasoning):** the point of the game is to teach Irish history. A linear campaign can present events in chronological order, build cause and effect across battles, and guarantee every player encounters the material in the intended sequence. A randomised run cannot promise any of that — a player might finish a Viking campaign without ever meeting a *longphort*.

**My earlier recommendation was run-based**, on replayability grounds. That recommendation is superseded, and the reasoning above is sound: if education is the primary goal, authored order is not a limitation but the entire mechanism. Randomisation actively works against a teaching sequence.

**Consequences — the tradeoff we are accepting, and how to cover it:**
- **Replayability no longer comes for free.** It has to be designed in deliberately. Candidates: difficulty tiers (the AI already supports three), alternate starting decks per campaign, optional side encounters, and score/rating on completion.
- **Deckbuilding must stay a between-battle activity**, since it no longer happens organically during a run. The Milestone 6 deckbuilder already fits this: the player edits a deck between chapters.
- **Content cost per hour of play is higher.** Every encounter is authored, so scope discipline matters. `Campaigns.md` §5 already argues for one complete campaign over four unfinished ones — that advice now matters more, not less.
- **Historical material becomes authored encounter text** rather than emergent choice nodes. The presentation rules in `HistoricalResearch.md` §5 become the governing constraint: never make the player read to progress, always make reading rewarding.

---

## D-12 — Supply refills each turn; starting morale is 50
**2026-07-29 · Accepted**

**Decision:** supply refills to a ceiling that grows by one per turn (to a max of 10) rather than accumulating unspent points. Starting morale rises from 30 to 50.

**Why — measured, not argued.** A 4,000-match simulation of the previous rules found:
- **Every 5-cost card went unplayed in every single match.** Fianna, Huscarl and Captain — a third of the roster — were dead cards. With accumulating supply there is always a 1-cost card worth playing, so nothing is ever banked and supply never rose above about three.
- After switching to refill, all nine cards saw play, but **28–51% of matches ended as mutual destruction**: retaliation destroys both cards in an exchange, so with a 30 morale pool against ~100 morale of deck, both sides crossed zero together within ten turns.

At 50 morale a match runs ~14 turns, draws fall to 2–5%, and mirror matches sit at 48–50% — the sanity check that says the rules themselves are not biased.

**Consequences:** cost is now a real decision, because turn number rather than hoarding governs what you can afford. Unspent supply is lost, which adds a small "use it or lose it" tension. Both are standard for the genre and both improved the measurements.

---

## D-13 — Balance by simulation, not by inspection
**2026-07-29 · Accepted**

**Decision:** card and deck balance is driven by `MatchSimulator` + `BalanceReport` (menu: **HToW → Run Balance Report**), which plays thousands of AI-vs-AI matches and reports win rates, draw rates, match length and per-card kill/death ratios.

**Why:** the interaction of a supply curve, a morale pool and retaliation is not something that can be judged by reading a stat table. Every balance conclusion in `CardSystem.md` §3 was written before retaliation existed and several were wrong once it landed.

**Method:** mirror matches are run first as a control. A deck against itself must sit near 50% — if it does not, the rules or the simulator are biased and every other figure is meaningless. That check caught a real defect: the simulator initially restated `StartingMorale` as its own constant, so a change to `BaseCharacter` silently did not reach it and a whole run reported figures for the old value. It now derives every rule constant from `BaseCharacter`. **Derive, never restate.**

**Result of the first pass** — Celtic vs Viking moved 28.4% → 32.1% → 39.1% → **47.1%** across four measured iterations:

| Change | Reason |
|---|---|
| Huscarl 5 → 6 supply | k/d 1.98, the strongest card; should cost the most |
| Captain 5 → 6 supply | k/d 1.66 |
| Battle Chariot 2 → 3 damage, 4 → 5 hp | k/d 0.27, the weakest card |
| Ceithern 4 → 5 damage | k/d 0.76, Celtic's mid-curve card |
| MercArcher gains **Volley** | k/d 0.28. It is literally an archer; it had no ability only because no 2014 `ceffect` data survived for the mercenaries |
| Celtic deck: −1 Chariot, −1 MercArcher, +1 Fianna, +1 Sellsword | Celtic was 40% chaff against Viking's 15% |

**Known limitation:** the report pools statistics for cards that appear in both decks (MercArcher, Sellsword, Captain), so their figures cannot be attributed to a side. Per-side tracking is the obvious next improvement.

---

## D-14 — Celtic's faction identity is blocked on an unimplemented ability
**2026-07-29 · Noted, not yet actioned**

**Observation:** retaliation makes cheap low-stat units close to worthless — they die to anything and rarely trade. Measured k/d: MercArcher **0.26**, Battle Chariot **0.67**, against Fianna 1.77 and Huscarl 1.72.

That directly undermines the faction identity proposed in `GameDesign.md` §5, where Celtic is *"kin-networks… cheap numerous units"*. Under the current rules, numerous cheap units is simply a bad strategy, and the balance pass above fixed Celtic partly by making it **less** numerous — the opposite of its intended character.

**The mechanic that would fix this already exists on paper.** `United` — stronger for each other friendly unit of the same type — is exactly what makes a swarm viable, and it is already authored on Ceithern and Bondi. Celtic's identity is therefore blocked on implementing one of the four recovered abilities.

**Recommendation:** implement `United` before the next balance pass, then re-run the report and rebuild Celtic around numbers rather than elites. Until then, Celtic is balanced but not yet *characterful*.

---

## OPEN QUESTIONS — need your decision

These change what gets built. They do not block Milestone 0, which is why the roadmap starts there.

### ~~Q-01 — Combat retaliation?~~ → **ANSWERED: yes** (2026-07-28). See D-09.

### ~~Q-02 — Linear campaigns or run-based structure?~~ → **ANSWERED: linear** (2026-07-29). See D-11.

### ~~Q-03 — Reinstate the four abilities?~~ → **ANSWERED: yes** (2026-07-28)
See D-07 below.

### Q-04 — Is the target a finishable personal project or an open-ended one?
This changes scope advice throughout. Four campaigns is a multi-year solo undertaking. One polished campaign is achievable. **Recommendation: build Celtic Ireland as a complete vertical slice**, then decide.
