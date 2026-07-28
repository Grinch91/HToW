# Historical Research

**Working reference for card, campaign, and flavour design.**

Every entry is tagged so the distinction the project cares about is never lost:

- **[FACT]** — supported by historical/archaeological evidence.
- **[MYTH]** — Irish mythology or saga literature. Real *culture*, not real *events*.
- **[FICTION]** — invented for gameplay. Must be clearly signposted in-game.

Where scholarship is contested or my confidence is low, it says so. Nothing here should be treated as settled without checking a real source — I am flagging what to verify, not replacing verification.

---

## 1. Existing card roster — accuracy audit

| Card | Tag | Notes |
|---|---|---|
| **Ceithern** | **[FACT]** | *Ceithearn* — light Irish foot-soldiers, anglicised "kern". Lightly armoured, javelin/sword/shield, skirmishers. **Caveat:** the term is best attested in the later medieval period (13th–16th c.); using it for the Celtic/Viking era is a mild anachronism. Defensible as a general term for light Gaelic infantry. |
| **Fianna** | **[MYTH]** | Roving warrior-bands of the Fenian Cycle, associated with Fionn mac Cumhaill. Literary, not historical units. **Should be labelled as mythology in-game.** Whether real *fian* warrior-bands existed as a social institution is genuinely debated among historians — an interesting nuance worth surfacing rather than hiding. |
| **Battle Chariot** | **[MYTH]** leaning | Chariots are prominent in the Ulster Cycle (*Táin Bó Cúailnge*). Evidence for chariot *warfare* in early medieval Ireland is thin, and by the Viking era it is essentially literary. **Recommend labelling as mythology/heroic-age**, which is more interesting than pretending otherwise. |
| **Bondi** | **[FACT]** | *Bóndi* — free Norse landholding farmers, the backbone of a levy. Not professional soldiers. Correctly modelled as numerous and unremarkable. |
| **Huscarl** | **[FACT]** | *Húskarl* — household retainers of a lord, professional and well-equipped. Strongest Viking-side card, correctly. |
| **Raider** | **[FACT]** | Generic but accurate. Raiding was the characteristic Norse activity in Ireland far more than pitched battle. |
| **MercArcher / Sellsword / Captain** | **[FICTION]** | Generic fantasy mercenaries. **Biggest missed opportunity in the roster** — see §4. |

---

## 2. Period outline

Dates are conventional; several are debated.

### Pre-Viking Ireland (to ~795) **[FACT]**
- Politically fragmented: ~150 *túatha* (petty kingdoms), each under a *rí túaithe*, in shifting hierarchies under provincial overkings.
- Warfare was **raiding, cattle-taking, and hostage-taking** — not wars of conquest or annihilation. Armies were small; campaigns were short and seasonal.
- Brehon law; a wealth economy measured substantially in cattle.
- Monasteries were major economic and political centres — and fought each other.

> **Design note:** this is the strongest possible justification for the `MoraleCost` system. Early Irish warfare genuinely ended when one side's will broke, not when it was destroyed. The game's core mechanic is more historically honest than a hit-point total would be.

### Viking Age (~795–1014) **[FACT]**
- First recorded raids ~795 (Rathlin Island and Lambay are both cited in the sources).
- Shift from hit-and-run raiding to *longphorts* — fortified ship-camps — and then permanent towns. **Dublin, conventionally dated 841**, plus Waterford, Wexford, Limerick, Cork.
- Norse and Irish rapidly intermarried, allied, and fought *alongside* each other as often as against. **The clean "Irish vs. Vikings" framing is the single biggest popular misconception about this period** — and correcting it would make the game more interesting, not less.
- Battle of Tara, 980 — Máel Sechnaill II defeats the Dublin Norse.

### Brian Boru (~941–1014) **[FACT]**
- Brian mac Cennétig of the Dál gCais, Munster; rose from a minor dynasty to *Ard Rí* (High King), breaking the Uí Néill monopoly.
- **Battle of Clontarf, Good Friday 1014.** Brian's forces won; Brian was killed.
- Key figures: **Máel Mórda mac Murchada** (King of Leinster), **Sitric Silkbeard** (Norse King of Dublin), **Gormlaith** (sister of Máel Mórda; married at different times to both Sitric's father and to Brian — a genuinely extraordinary political figure and badly under-used in popular treatments).
- **Crucially: Clontarf was not "Irish vs. Vikings."** Irish and Norse fought on *both* sides. Leinster allied with Dublin against Brian. Presenting it accurately is a better story than the nationalist myth.

### Norman Invasion (1169–) **[FACT]**
- **Diarmait Mac Murchada**, deposed King of Leinster, invited Anglo-Norman help.
- 1169 landings at Bannow Bay; **Richard de Clare ("Strongbow")** takes Waterford 1170 and marries Diarmait's daughter **Aoife**.
- **Henry II arrives 1171** — largely to stop Strongbow establishing an independent rival kingdom.
- Military asymmetry is the story: mailed heavy cavalry, archers, and **castles** against lightly-armoured Gaelic infantry. Castles in particular changed the strategic logic permanently — raiding cannot take a stone keep.

> **Design note:** this is where teaching-through-mechanics is at its most powerful. If the player's Gaelic deck genuinely struggles against cavalry and fortification cards, they *learn why the Normans succeeded* by losing to it. That is the thing a game can do that a textbook cannot.

---

## 3. Card ability grounding

Reinstating the four abandoned `ceffect` values (see `CardSystem.md` §4c):

| Ability | Tag | Basis |
|---|---|---|
| **Volley** | **[FACT]** | Missile troops — javelins, slings, bows. Skirmishing at range was standard Gaelic practice. |
| **United** | **[FACT]** | *Túath* levies and Norse *bóndi* fought as bodies of kin and neighbours; cohesion was social, not drilled. |
| **Berserker** | **[MYTH]** | *Berserkir* appear in Old Norse literature. Whether they were a real institution, a literary trope, or something in between is genuinely disputed. **Label as saga tradition.** |
| **Bloodrush** | **[FICTION]** | Invented name, but it expresses something real: the Norse strategic advantage was *speed*, delivered by ships. |

---

## 4. Highest-value historical additions

### 4.1 Fix the mercenaries — the biggest easy win
Cards 7–9 are generic fantasy filler in a game that has real material available:

- **Gallóglaigh (gallowglass)** **[FACT]** — Norse-Gaelic heavy infantry from the Hebrides, axe-armed, hired by Irish lords. **Caveat: 13th century onward**, so they belong to a Norman-era or later campaign, *not* Clontarf. Perfect for a later campaign; would be an anachronism in an early one.
- **Norse mercenary fleets** **[FACT]** — hiring Dublin's ships and men was routine for Irish kings. Fits the Viking-era campaigns exactly.
- **Bonnaught (buannacht)** **[FACT]** — billeted mercenary troops maintained by imposed quartering. Mechanically evocative: costs upkeep, alienates your own people.

**Design payoff:** mercenaries costing *morale* as well as supply is both a good mechanic and accurate. Hired swords with no loyalty genuinely cost a lord standing. This is the clearest example in the project of history and mechanics reinforcing each other.

### 4.2 Fill the roster gaps
Currently missing and well-attested **[FACT]**: monastery/church units (major powers, and raiding them was economically significant), **cattle** as an objective rather than a unit, hostages (*giallna*) as a political mechanic, ships/*longphort*, and — for the Norman period — castles and heavy cavalry.

### 4.3 Event material for a run-based campaign
If Q-02 in `Decisions.md` resolves toward runs, these are natural choice-nodes: the sack of a monastery; a hostage exchange; a cattle raid; Gormlaith's marriage alliances; **Máel Mórda's insult at the chess game** — a chronicle episode that reads as a ready-made branching choice; the decision to hire Norse ships; Diarmait's decision to seek foreign help.

---

## 5. Presentation rules

1. **Never make the player read to progress.** Flavour is always available, never blocking. (`GameDesign.md` §6.)
2. **Always mark [MYTH] visibly.** Fianna and Berserker are culture, not chronicle. Marking them costs nothing and is genuinely respectful of the material — and the distinction is itself interesting.
3. **Don't flatten the politics.** "Irish vs. Vikings" is wrong and less interesting than the truth. Let the player ally with Norse Dublin.
4. **Two lines of flavour, one paragraph of history on demand.** Keep the historical note in a separate field on `CardData`.
5. **Verify before shipping.** Everything above is a starting point written from general knowledge. Anything that appears in the game should be checked against a real source — Ó Cróinín's *Early Medieval Ireland* and the *New History of Ireland* volumes are the standard starting points.
