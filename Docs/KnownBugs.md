# Known Bugs

**Compiled 2026-07-28 from static analysis of all 16 source files, cross-checked against `HToW_Data/output_log.txt` (a real 2014 play session).**

Legend: **[LOG]** = confirmed by the recorded runtime log. **[STATIC]** = derived by reading the code; high confidence but not yet observed. **[UNVERIFIED]** = suspected, needs the project opened in Unity to confirm.

---

## Status

| Milestone | Resolved |
|---|---|
| **M1 — Real turns** | **C3** (turns at frame rate) · **M6** (turn messages never shown) · **M7** (wrong timer reset) · **F2** (`HasAttacked` never reset) · **N5**, **N8**, **N10** (dead fields/states removed) · **F1** *mitigated* |
| **M2 — Card identity** | **C1** (selection service replaces `BattleController`) · **C2** (cards move by reference) · **C4** (supply checked against the real player and actually spent) · **C6** (`DrawTop()` guards emptiness) · **M1** (AI plays from its hand and pays supply) · **M2** (AI target choice is a scoring function weighting `MoraleCost`) · **M3**, **M4**, **M5** (no more tags or name-matching) · **N1**, **N2**, **N4**, **N5**, **N6**, **N11**, **F3** |
| Retracted | **C5** — never a bug, see below |
| **Still open** | **M8** (no HUD) · **M9** *(moot — `MusicManager` deleted)* · **M10** (morale still not the intended value) · **M11**, **M12** (deck size and the unreachable loss condition) · **N3**, **N7**, **N9** · **F4**, **F5**, **F6** |

Milestones 3 and 4 close the remainder. Note **F1** is now structurally handled — `CardZone.Snapshot()` exists precisely so attack loops iterate a copy.

---

## CRITICAL — the game cannot be played through

### C1. Combat throws `NullReferenceException` on the first attack **[LOG]**

`Game.AttackingCard()` and `Game.TargetCard()`:
```csharp
if (m_state == GameState.PlayerTurn && playerActive.GetComponent<BattleController>().attacker != null)
```
`playerActive` is the `Deck` component on the `Active-Player` GameObject. **No `BattleController` is attached to that GameObject** — I verified `BattleController` appears in no scene and no prefab. `BattleController` is only ever added at runtime to *individual card* objects, which are *children* of `Active-Player`. `GetComponent` does not search children, so it returns `null` and dereferencing `.attacker` throws.

Recorded stack trace:
```
NullReferenceException
  at Game.AttackingCard (.CardDef selectAttacker)
  at Game.Battle () / Game.UserTurn () / Game.LaunchTurn () / Game.Update ()
```

**Why it happens:** the selection state (which card did the player click?) was put on the *card* but read from the *container*. There is also no mechanism to ever clear a selection once made.

**Fix direction:** selection state belongs in one place — a battle/selection service that cards report *into*, not a component the container is assumed to have. Do not fix this by adding a `BattleController` to `Active-Player`; that keeps a broken model.

---

### C2. Playing a card plays the **wrong card** **[STATIC]**

`OnCardHandClick()` → `playerActive.AddToDeck(temp)` appends the chosen card to `playerActive.deck`, then `AddToActivePlayer()` calls `playerActive.Deal()`, which **removes and returns element 0** — the card that has been on the board longest.

| Board before | You play | Actually spawned | Board after |
|---|---|---|---|
| (empty) | B | B ✔ | [B] |
| [A] | B | **A** ✘ | [B, A] — a second copy of A |
| [A, C] | B | **A** ✘ | [C, B, A] |

Every play after the first spawns a duplicate of an existing card and silently queues the card you actually chose. `AddToActiveAi()` has the identical defect.

**Why it happens:** `playerActive` is a `Deck`, and `Deck` is modelled as a draw pile (append at the end, deal from the front). The "active area" is not a pile — it is a set. Reusing `Deck` for both roles is the root cause.

---

### C3. Turns run at frame rate — hundreds per second **[LOG]**

`Update()` re-enters the turn block every frame because `EndTurn()` resets `m_state` to `Begin`. 716 full turn cycles were recorded in ~12 seconds of play. The `playerTurnOver` guard cannot fire (`LaunchTurn()` clears it immediately before checking it), and the End Turn button's `buttonPushed` flag **is never read by any code**.

Full analysis in `GameplayLoop.md` §3. This is the README's "Fix turn system".

---

### C4. Cards costing 3+ can never be played **[LOG]**

`MouseController.OnMouseDown()`:
```csharp
playerTemp = new Player();                       // ← brand new player, every click
if (playerTemp.playerSupply >= ...Data.Cost)
```
It constructs a fresh `Player`, whose supply is always `StartSupply` = 2. The real `Game.playerInstance` — the one that accumulates +1 per turn — is never consulted. Supply is also **never deducted** when a card is played.

Consequence: cost-1 and cost-2 cards are always free and unlimited; cost-3+ cards are permanently unplayable. Of the three Celtic cards, only Battle Chariot (1) and MercArcher (1) are ever usable — Ceithern (3) and Fianna (4) are dead weight. 12 rejected clicks appear in the log as `Nay`.

---

### ~~C5. Playing a Battle Chariot throws `UnityException`~~ — **RETRACTED 2026-07-28. Not a bug.**

> **This finding was wrong.** It was derived from raw byte-string extraction of the binary `TagManager.asset`, which silently dropped the space-containing tag. Once the project was converted to text serialization during the Unity 6 upgrade, `TagManager.asset` could be read directly and lists **ten** tags:
>
> ```
> Game, Ceithern, Battle Chariot, Fianna, Bondi, Raider, Huscarl, Captain, Sellsword, MercArcher
> ```
>
> `Battle Chariot` is present at index 2 — exactly where the extraction skipped it. Every card name used by `newObj.tag = c1.Name` has a registered tag. **Playing a Battle Chariot does not throw.**
>
> The C-numbering is left with a gap rather than renumbered, so that references in other documents and in commit messages stay valid.
>
> **Lesson recorded:** conclusions drawn from binary string extraction are provisional. Absence of a string is weak evidence; presence is strong. Findings in this document tagged **[STATIC]** that depended on binary extraction — rather than on reading C# source — should be re-verified now that all assets are text.

---

### C6. `Deck.Deal()` will throw on an empty deck **[STATIC]**

```csharp
int check = deck.Count;
if (check != null) { CardDef returnCard = deck[0]; ... }
return null;
```
`check` is an `int`. `check != null` uses the lifted nullable operator and is **always true**. The guard does nothing; `deck[0]` on an empty list throws `ArgumentOutOfRangeException`.

Currently masked because `CheckIsDeckEmpty()` happens to gate the draw calls. Any new call site — a mulligan, a "draw 2" effect, a deckbuilder — will hit it immediately. The identical `!= null`-on-an-int mistake appears again in `InitialSetUp()`.

---

## MAJOR — systems behave wrongly but do not crash

### M1. The AI plays from its **draw pile**, not its hand, and never removes the card **[STATIC]**

`SelectCardAI()` scans `aiDeck.deck` — the draw pile. The AI's hand (`aiHand`) is filled by `AddToEnemyHand()` and then **never read by anything**. The chosen card is never removed from `aiDeck`, so the AI can re-play the same card indefinitely.

The AI is not playing the game the player is playing. See `AI.md`.

### M2. AI card selection is order-dependent and can pick nothing **[STATIC]**

The loop `if (deck[i].Dmg > temp.Dmg && deck[i].Cost <= supply)` assigns `temp` and *then* sets `selectedCardTemp[0]` — but only inside the `if`. If a later card has higher damage but is unaffordable, the ID string keeps a stale value from an earlier iteration. If nothing qualifies at all, `selectedCardTemp[0]` stays `null`, `AddToDeck` silently adds nothing, and `AddToActiveAi()` proceeds anyway — duplicating an existing board card via C2.

### M3. Enemy-killed player cards leave ghost GameObjects **[STATIC]**

`Attack()` calls `Destroy(GameObject.FindGameObjectWithTag(target.Name))` **only** in the `PlayerTurn` branch. When the AI kills a player card, the `CardDef` is removed from the list but the sprite stays on screen forever, still clickable.

### M4. `FindGameObjectWithTag` can destroy the wrong card **[STATIC]**

Tags are card *names*, not instances. If both sides have a Raider in play, `FindGameObjectWithTag("Raider")` returns whichever Unity finds first — potentially the player's own card being destroyed instead of the enemy's. With duplicates on one side, it destroys an arbitrary one.

### M5. Card removal matches by name, not identity **[STATIC]**

`Attack()` removes with `if (target.Name == card.Name)`. With two Raiders in play, the *first* Raider in the list dies regardless of which one was actually attacked — and the survivor keeps the damaged `Hp` value belonging to the dead one.

### M6. Turn/win messages are unreachable **[STATIC]**

`ShowMessage()` handles four cases, but its only caller is `CheckForWinner()`, which passes only `"AI wins"` / `"Player wins"`. `MessagePlayerTurn` and `MessageAITurn` are set inactive in `Start()` and **never shown again**. The player receives no turn feedback at all.

### M7. `ShowMessage` resets the wrong timer **[STATIC]**

```csharp
messageTimer += Time.deltaTime;
if (messageTimer >= timerMax) Application.LoadLevel("MainMenu");
timer = 0.0f;                      // ← resets `timer`, not `messageTimer`
```
`messageTimer` is never reset. Harmless today (the scene reloads anyway) but wrong, and it will misfire once the battle scene is reused.

### M8. The HUD is never updated **[STATIC]**

`playerHealth`, `enemyHealth`, `playerSupply`, `enemySupply` exist as TextMesh objects in `Battle.unity`. **No code references them.** The player cannot see their own morale or supply — which, combined with C4, means the resource system is entirely invisible.

### M9. `MusicManager` is broken by construction **[STATIC]**

```csharp
go = GameObject.Find("song name here");   // placeholder name, returns null
go.audio.clip = NewMusic;                 // NRE; NewMusic is also always null
```
The log shows no such exception, so the component is evidently **not attached to anything** — music presumably plays via an AudioSource's "Play On Awake". `MusicManager.cs` is dead placeholder code. Note `go.audio` is also a hard compile error in Unity 5+ (see `UnityUpgrade.md`).

### M10. Starting morale is overwritten to 1 **[STATIC]**

`Start()` contains `playerInstance.playerMorale = 1; enemyInstance.enemyMorale = 1;` with the comment `//For testing purposes`. A debug value left in the shipped path — the first card death would end the match.

### M11. The Celtic deck is empty before turn 1 **[LOG]**

`celtic.txt` has 5 cards; `InitialSetUp()` draws 5. `Deck is empty` appears **717 times** in the log — on every player turn of the session. The player never draws another card.

### M12. The Celtic deck cannot lose **[STATIC]**

Morale is lost only when your own cards die. Celtic's total MoraleCost is 21 vs. 30 starting morale — even losing every card leaves the player alive. Viking totals 72. See `GameplayLoop.md` §6.

---

## MINOR

- **N1.** `new Deck()` field initialisers on six `public Deck` fields — `Deck` is a `MonoBehaviour` and cannot be constructed with `new`. Unity logs a warning per field on load; the values are overwritten by the serialized scene references anyway. Pure noise.
- **N2.** `Deck.Shuffle()` is described as "Knuth Shuffle" but uses `random.Next(deck.Count)` over the *full* range each iteration instead of `Next(i, count)`. This is the classic biased-shuffle bug — not all permutations are equally likely. Cosmetically minor here; worth fixing since it is one line.
- **N3.** Public mutable state that should be private: `timer`, `i`, `n`, `tempX`, `tempY`, `seperatingCardsAI/Player`, `userlist`, `enemylist` are all `public` and appear in the Inspector where they can be edited into nonsense.
- **N4.** `userlist` / `enemylist` are appended to on every card creation and **never read**. Unbounded memory growth, though slow.
- **N5.** `seperatingCardsAI` / `seperatingCardsPlayer` are incremented but never used in any position calculation (position uses `playerDeck.deck.Count` instead). Dead, and misspelled.
- **N6.** Card X-position is derived from `playerDeck.deck.Count` — the *draw pile* size — so once the deck empties every card spawns at the same coordinates, stacked on top of each other.
- **N7.** `BoxCollider` (3D) is used on 2D sprites, while `Battle.unity` also contains a `CircleCollider2D`. Mixed physics dimensions; `OnMouseDown` happens to work with 3D colliders, so this survives, but it is inconsistent.
- **N8.** `GameState.Pending` and `GameState.Pause` are declared and never used.
- **N9.** `const float FlyTime = 0.5f` — an animation constant, never referenced. Evidence of a planned card-flight tween.
- **N10.** `justChanged` in `EndTurn()` is reset to `false` at the top of the method every call, making the field equivalent to a local variable.
- **N11.** `Assets/Resources/celtic.txt` and `viking.txt` are dead duplicates with *different contents* from the live files — a trap for anyone editing decks.
- **N12.** `Assets/Scenes/Battle (Cielo's conflicted copy 2014-03-21).unity` and `HToW_Data/output_log (Cielo's conflicted copy 2014-04-10).txt` are Dropbox conflict artifacts committed to git.
- **N13.** Three `Thumbs.db` files are committed and have no `.meta` — Windows Explorer junk inside `Assets/`.
- **N14.** `Assets/Scripts/Menus/OldUnusedCode/` contains three files (`MenuGUI`, `BattleMenu`, `CardMenu`) that are attached to nothing and reference scenes that no longer exist.

---

## POTENTIAL FUTURE BUGS

- **F1. Collection modified during enumeration.** `Attack()` mutates `playerActive.deck` / `aiActive.deck` while `UserTurn()`/`EnemyTurn()` are iterating active-area lists. It survives today only because the mutated list is the *opposing* one and because the inner `foreach` hits `break` immediately after `Remove`. Any change to combat — retaliation damage, area effects, multi-target — will produce `InvalidOperationException`. Iterate over a snapshot or index backwards before touching this.
- **F2. `HasAttacked` is never reset.** Once a card attacks, `HasAttacked` stays `true` for the rest of the match. Currently invisible behind C1/C3; the moment turns are fixed, every unit becomes a one-shot.
- **F3. String-keyed everything.** Card name is simultaneously the GameObject name, the Unity tag, the identity used for removal, and the AI's selection key. Renaming a card, or adding a card whose name collides, breaks four systems at once. This is the highest-leverage thing to remove.
- **F4. Binary serialization.** With `Force Binary` set, scene and prefab changes are unmergeable and undiffable. Any collaboration or careful review is impossible until this is switched to Force Text (see `TechnicalDebt.md`).
- **F5. Missing scenes in Build Settings.** `BattleMenu.unity` and `CardMenu.unity` are listed but deleted. Harmless now, but scene *indices* shift, so any future `LoadLevel(int)` call will load the wrong scene.
- **F6. `Application.dataPath` file IO.** `Deck.Load` reads loose `.txt` files next to the executable. This breaks outright on mobile/console/WebGL and makes deck data user-editable in a shipped build. Should move to `Resources`/`StreamingAssets` or, better, to ScriptableObjects.

---

## Fix ordering

The dependency chain matters. Fixing combat before turns is wasted work.

1. **C3** (turn gating) — nothing else is testable until turns are discrete.
2. **C1 + C2** (selection model, active-area model) — these are one refactor, not two patches.
3. **C4 + M10 + M8** (real supply economy + a visible HUD) — the economy is meaningless while invisible.
4. **C6, M3–M5** (identity: replace name-strings and tags with instance references).
5. **M1, M2** (make the AI play by the same rules as the player).
6. **M11, M12** (deck contents and the morale/deck-size relationship).

Items C1, C2, M3, M4, M5, F3 all share a single root cause — **card identity is a string and the active area is modelled as a pile**. Fixing that data model resolves six bugs at once, which is why `Roadmap.md` puts it in Milestone 2 rather than patching each symptom.
