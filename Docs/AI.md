# AI

**Reverse-engineered 2026-07-28.** The README's second urgent to-do was "Implement AI". That is accurate — what exists is a first sketch, not an opponent.

---

## 1. What exists

Three methods in `Game.cs`, ~90 lines total:

| Method | Role |
|---|---|
| `SelectCardAI()` | choose a card to play |
| `AttackingCard()` (AI branch) | choose which of its units attacks |
| `TargetCard()` (AI branch) | choose what to attack |

Plus `EnemyTurn()`, which sequences them behind a 5-second timer.

There is no difficulty setting, no personality, no lookahead, no evaluation function, and no notion of the AI's own life total.

---

## 2. Card selection — `SelectCardAI()`

```csharp
for (int count = 0; count < aiDeck.deck.Count; count++)
    if (aiDeck.deck[count].Dmg > temp.Dmg && aiDeck.deck[count].Cost <= enemySupply)
        temp = aiDeck.deck[count];              // + a 9-branch if-chain mapping name → id string
```

**Intended policy:** play the highest-damage card you can afford. That is a reasonable baseline greedy heuristic.

**Actual behaviour — four defects:**

1. **It reads the draw pile, not the hand.** `aiDeck` is the deck; `aiHand` is filled by `AddToEnemyHand()` and then **read by nothing, ever**. The AI has perfect knowledge of its entire remaining deck and plays straight out of it. The player's hand-management constraint does not apply to the opponent at all.

2. **The chosen card is never removed from the deck.** `aiDeck.deck` is never modified by `SelectCardAI()`, so the same card can be selected and played on every subsequent turn, forever.

3. **The ID mapping can go stale.** `selectedCardTemp[0]` is only written inside the `if`. Because `temp` and the string are assigned together but the loop continues, a later high-damage-but-unaffordable card cannot overwrite it — yet a subtler ordering can leave the string pointing at an earlier card. If *no* card qualifies, the string stays `null`, `AddToDeck` matches nothing, and `AddToActiveAi()` runs anyway, duplicating a board card via bug C2.

4. **`runOnce` is dead.** Declared `false`, checked once, set `true` at the end — it never guards anything, since the method returns immediately after.

**Policy blind spots** even if the bugs were fixed:
- Damage is the *only* consideration. HP, morale cost, and board state are ignored.
- No curve awareness: it will never hold supply to afford a bigger unit next turn.
- No notion of playing *multiple* cards per turn — it plays exactly one.

---

## 3. Attacker selection — `AttackingCard()`

```csharp
foreach (CardDef card in aiActive.deck)
    if (card.Dmg > selectAttacker.Dmg) selectAttacker = card;
```

Picks the highest-damage friendly unit. Reasonable, and it works. But it ignores `HasAttacked`, so the *same* card is chosen every time within `EnemyTurn()`'s attack loop — the loop iterates over every card that has not attacked, but always resolves combat using the single strongest one.

---

## 4. Target selection — `TargetCard()`

This is the only piece of real tactical reasoning in the project:

```csharp
foreach (CardDef card in playerActive.deck) {
    if (card.Hp < attacker.Dmg)                              selectTarget = card;   // rule A
    if (card.Hp > attacker.Dmg && card.Dmg > attacker.Dmg)   selectTarget = card;   // rule B
}
```

- **Rule A:** prefer a target you can kill this turn. Sound.
- **Rule B:** otherwise prefer a target that is both tougher and more dangerous than you. Defensible as "attack the biggest threat".

**Problems:**
- The rules are evaluated per-card with no scoring, so **the last matching card in list order wins**, not the best one. Given a killable 1-HP unit at index 0 and a threatening unit at index 3, it attacks the threat and lets the free kill go.
- Rule A uses `<` not `<=`, so a target with *exactly* lethal HP is not recognised as killable.
- `MoraleCost` — the actual win condition — is never considered. The AI has no idea which kills bring it closer to winning. Killing a 1-morale MercArcher and a 10-morale Fianna are equivalent to it.
- If nothing matches either rule, `selectTarget` stays the `"null"` sentinel card and `Battle()` correctly skips — so the AI simply declines to attack rather than taking a chip-damage trade.

---

## 5. Turn pacing — `EnemyTurn()`

```csharp
timer += Time.deltaTime;
if (timer >= timerMax) { AddToEnemyHand(); SelectCardAI(); timer = 0; }
foreach (...) Battle();          // runs EVERY frame, outside the timer
m_state = GameState.Resolved;
EndTurn();                       // runs EVERY frame
```

The 5-second timer gates only *drawing and playing*. The attack phase and `EndTurn()` run on every frame, which is half of why turns free-run (bug C3). The `do { ... } while (n < 1)` wrapper is a loop that executes exactly once — a stand-in for a "how many cards to play" rule that was never written.

---

## 6. Honest assessment

| Aspect | State |
|---|---|
| Plays cards | ~20% — greedy heuristic, wrong data source, no removal |
| Chooses attackers | ~40% — works, ignores `HasAttacked` |
| Chooses targets | ~50% — the best-developed part; two real heuristics, no scoring |
| Understands the win condition | 0% — `MoraleCost` never consulted |
| Manages resources | 0% — never holds supply, never plans a curve |
| Difficulty tuning | 0% |
| Plays by the player's rules | **0% — this is the headline problem** |

The AI cheats without meaning to: it plays from an infinite deck, ignores its hand, and never runs out of anything. Any balance work is meaningless until the AI is subject to the same constraints as the player.

---

## 7. Recommended direction

**Step 1 — make it legal before making it smart.**
The AI must draw into `aiHand`, play *from* `aiHand`, remove played cards, and pay supply. Symmetry first. This alone converts it from a scripted prop into an actual opponent, and it costs very little code once the turn system and card identity are fixed.

**Step 2 — replace the if-chains with a scoring function.**
The natural shape, matching what the code was groping toward:

```csharp
// play phase: score every affordable card in hand, play the best, repeat while affordable
float ScorePlay(CardInstance c)  => c.Damage * wDmg + c.Hp * wHp - c.Cost * wCost;

// attack phase: score every (attacker, target) pair, take the best
float ScoreAttack(CardInstance a, CardInstance t) {
    float s = 0;
    if (a.Damage >= t.CurrentHp) s += 100 + t.MoraleCost * 10;  // a kill, weighted by morale swing
    else                         s += a.Damage;                  // chip damage
    if (t.Damage > a.Damage)     s += 20;                        // remove a threat
    return s;
}
```

Scoring gives three things the current design cannot: it always picks the *best* option rather than the last matching one, it makes `MoraleCost` (the win condition) drive decisions, and difficulty becomes tunable by perturbing the weights or occasionally picking the second-best move rather than by writing separate AI code paths.

**Step 3 — only then consider lookahead.** With ≤10 units on a board this small, a one-ply simulation of "what does the board look like after this play" is cheap. But it is not worth building until steps 1 and 2 are done and the game is actually playable.

**Do not** build personality/faction AI (a cautious Norman vs. an aggressive Viking) until the baseline works — but it is a strong later feature for the campaign, and it fits the historical framing well.
