# Game Design Review

**Written 2026-07-28, wearing the designer hat rather than the engineer hat.**

The brief asked me to challenge the original concept where warranted. This document does that. Section 1 argues the design is better than the code suggests; sections 3–8 argue where it needs to change.

---

## 1. The core is stronger than you probably remember

Buried under the bugs is a resource system that is genuinely *not* a Hearthstone clone:

> **You do not lose life when you are attacked. You lose morale when your own units die.**

That single rule — `MoraleCost` — is the most valuable thing in the project, and I suspect it was half-accidental. Its consequences are excellent:

- **A deck's total MoraleCost is its life pool.** Deck construction and survivability become the same decision. Cheap chaff (MercArcher, 1 morale) is genuinely expendable; elite units (Fianna, 10 morale) are catastrophic to lose. That is a real, legible tension.
- **It fits the setting perfectly.** Early medieval Irish warfare was not attrition to annihilation — it was cattle raids, hostage-taking, and armies that broke and went home. *Morale* as the losing condition is more historically honest than a hit-point total, and it's thematically justified rather than bolted on.
- **It creates an unusual decision:** sometimes the correct play is to *not* contest the board, because losing the trade costs you more than conceding it.

**Keep this. Build the game around it.** Make `MoraleCost` visible on every card, and show the morale swing when a unit dies. Right now it is invisible to the player and ignored by the AI.

---

## 2. Where the current design fails

### 2.1 There are no interesting decisions
A turn consists of: play the biggest card you can afford, attack with everything. There is no reason to do anything else. Specifically:

- **No retaliation** — attacking is free, so you always attack.
- **No card abilities** — cards differ only by three numbers.
- **No positioning, no board limit, no summoning sickness** — no spatial or tempo decisions.
- **No hand-size pressure, no discard** — no reason to hold anything.
- **Perfect information about outcomes** — combat is deterministic and one-directional.

A player never faces a choice where two options are both defensible. That is the definition of a game with no depth, and it is the most important design problem — bigger than any bug.

### 2.2 The cards are not differentiated
Nine cards, all "Warrior", all identical in function. Ceithern and Bondi have the *same statline at different costs*. The abandoned `ceffect` values (`Volley`, `United`, `Berserker`, `Bloodrush`) show you already knew this in 2014.

### 2.3 The theme is currently invisible
Nothing in the running game teaches Irish history. Card names are the only historical content, and they are not explained anywhere. The stated vision — "players learn Irish history through gameplay" — is at 0%.

---

## 3. Recommendation: add retaliation

**Change:** when A attacks B, B deals its damage back to A.

**Why:** this is the single highest-impact design change available, and it costs about five lines.

It converts every attack from a free action into a **trade evaluation**: is my 4-morale Raider worth your 6-morale Ceithern? Combined with `MoraleCost`, that question has a *different answer depending on deck construction* — which is exactly where deckbuilding games get their depth. Without retaliation, `MoraleCost` never actually creates a decision, because you never risk your own units.

**Tradeoff, stated honestly:** retaliation favours high-HP defensive units and can make boards stall. Mitigate with abilities that break stalls (`Volley` as ranged/no-retaliation is the obvious one — and it's already in your 2014 design). Also, retaliation punishes the aggressor, which may sit oddly with a warfare theme; a "first strike" mechanic for cavalry/chariots is a natural counterweight.

**Alternative if you dislike it:** keep one-directional combat but let the *defender choose* whether to block, Magic-style. More decisions, but a much larger rules change.

---

## 4. Recommendation: build the four abilities you already designed

You had `Volley`, `United`, `Berserker`, `Bloodrush` in 2014 and the code was lost. Proposed readings that fit both the names and the history:

| Ability | Suggested rule | Historical/thematic basis |
|---|---|---|
| **Volley** | Deals damage without taking retaliation | Missile troops — slingers, javelin-throwers, archers. *Historical.* |
| **United** | +1 damage for each other friendly unit with the same name/type | Túath levies and the *bóndi* fighting as a body of kin and neighbours. *Historical.* |
| **Berserker** | +damage while below half HP; must attack each turn | *Berserkir* — Norse literary tradition. **Flag as mythology/saga, not history.** |
| **Bloodrush** | May attack the turn it is played | Raiding speed — the Viking strategic signature. *Historical in spirit.* |

Four keywords is exactly the right number to start with: enough to differentiate cards, few enough to teach. They also happen to map onto faction identity — Volley/United reading Celtic-defensive, Berserker/Bloodrush reading Norse-aggressive — which does a lot of characterisation work for free.

---

## 5. Recommendation: make factions mean something

Currently "Celtic" and "Viking" are just two text files. Give each an **identity the player can feel**:

| Faction | Fantasy | Mechanical expression |
|---|---|---|
| **Celtic / Gaelic** | Kin-networks, terrain, defensive resilience | `United` synergies, cheap numerous units, low individual morale cost |
| **Norse** | Speed, raids, elite shock troops | `Bloodrush`, `Berserker`, high damage, high morale cost |
| **Mercenary** | Available to anyone, at a price | Costs supply *and* morale to play; no loyalty |

The Mercenary idea is already implicit — cards 7–9 appear in both decks. **Lean into it.** Mercenaries who cost you morale to hire is both a good mechanic and genuinely accurate to how Norse-Gaelic *gallóglaigh* and hired fleets actually worked. That's the kind of thing where history and mechanics reinforce each other instead of fighting.

---

## 6. Recommendation: teach history through *consequence*, not text boxes

The vision says players should learn Irish history. The failure mode here is a wall of encyclopedia text between battles that everyone skips.

Better, in rough order of value:

1. **Teach through mechanics.** If the Norman campaign gives the enemy heavy cavalry and castles that your Gaelic deck genuinely struggles against, the player *learns why the Normans won* by losing to it. That is the thing this genre can do that a book cannot.
2. **Flavour text on cards** — one or two lines, always visible, never blocking.
3. **A "historical note" field on every card**, viewable on demand in the collection browser. This is where accuracy lives, and where you can honour the brief's requirement to distinguish **historical fact / mythology / gameplay fiction**. Make that distinction a visible label on the card — it costs nothing and it is genuinely respectful of the material.
4. **Short scene-setting before a battle** — 2–3 sentences, not an essay.

**Design constraint worth committing to:** never make the player read to progress. Make reading *rewarding* instead.

---

## 7. Recommendation: reconsider the campaign structure

The vision lists Celtic Ireland → Vikings → Brian Boru → Normans as separate campaigns. That's a natural chronology, but as *game structure* it has a problem: it's linear, and linearity is the enemy of replayability, which is also on your list.

Given the Hand of Fate inspiration, a **run-based structure** fits better and is far cheaper to build:

- A campaign is a **run** through a branching map of encounters (battles, events, recruitment, choices).
- You start with a small historically-flavoured deck and build it *during* the run.
- Losing ends the run; you keep meta-progression (unlocked cards, new starting decks).
- Each historical period is a different run with different starting decks, enemies, and events.

Why this is better here:
- **Replayability comes for free** rather than being designed in later.
- **Deckbuilding happens during play**, which is more interesting than pre-building a deck in a menu — and it means Milestone 6 can be simpler.
- **Historical events become encounter cards**, which is a much better teaching vehicle than cutscenes. "Máel Mórda's insult at the chess board" is a *choice node*, not a paragraph.
- It scales down. You can ship **one** campaign and have a complete game.

**Tradeoff:** it's a departure from a strict historical-narrative campaign, and it makes telling a specific story (e.g. Clontarf beat by beat) harder. If the storytelling matters more to you than replayability, the linear structure is the right call — but then accept that replayability will need to come from elsewhere (difficulty tiers, alternate decks). **This is a decision worth making explicitly and recording in `Decisions.md`.**

---

## 8. Smaller recommendations

- **Show card stats on the card.** HP, damage, cost and morale cost are currently invisible in-game. Nothing else on this list matters until this is true.
- **Show the morale swing when a unit dies** — big, animated, unmissable. It's the win condition; make it felt.
- **Cut the 5-second auto-return to the main menu** after a win. Let the player sit with the result.
- **Rename `MoraleCost` in the UI.** Call it *Renown*, *Standing*, or *Honour* — "morale cost" reads as a stat, but it's really "how much it hurts your cause to lose this warrior."
- **Board limit of ~6 units per side.** Creates a real cost to overextending and prevents the board from becoming unreadable.
- **The Generals.** Art and scene objects exist (`celticGeneral.png`, `General-Player`). A general who grants a passive faction bonus and is targetable late is a strong hook, and it's already half-designed. Good Milestone 8 candidate.

---

## 9. What I would *not* change

- **The `MoraleCost` core.** Distinctive and thematically right. Protect it.
- **The historical setting.** Under-served, genuinely interesting, and a real differentiator. Irish history is not a crowded genre space.
- **The card names.** Ceithern, Fianna, Bondi, Huscarl — well chosen, specific, evocative. Whoever picked those did the research.
- **The Mercenary cross-faction concept.** Accidentally the most interesting faction idea in the project.

---

## 10. The most important design question

**Is a single battle fun?**

Right now that question is unanswerable, because a battle cannot be completed. Milestones 0–4 in `Roadmap.md` exist purely to make it answerable. Once one battle is playable end to end, **stop and play it for a week** before building campaigns on top of it.

If the answer is no, the fix is almost certainly in §2.1 — not enough interesting decisions — and §3 and §4 are the cheapest routes to fixing it.
