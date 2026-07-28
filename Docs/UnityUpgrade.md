# Unity Upgrade Assessment

**Current: Unity 4.3.4f1 (Feb 2014). Target: Unity 6 LTS (or 2022.3 LTS).**

**Recommendation: do it, do it early, and do it in one jump — but only after the preparation steps in §5.**

---

## 1. Headline conclusion

This is a **small** upgrade by Unity-migration standards. That is a direct consequence of how little code there is: 16 files, ~35 KB, no third-party packages, no custom shaders, no editor scripts, no networking, no animation controllers, no physics simulation, no addressables.

Realistic effort: **1–2 focused sessions to compile and run**, plus a session of visual re-tuning. The upgrade is *not* the risk on this project. The gameplay code is.

The strongest argument for upgrading early: much of the code you must touch to fix the game (`OnGUI` menus, `LoadLevel`, string `Resources.Load`, the card factory) is *also* what the upgrade forces you to touch. Doing both at once avoids paying twice.

---

## 2. Hard breaking changes — code that will not compile

| Location | Deprecated API | Replacement | Notes |
|---|---|---|---|
| `MusicManager.cs:21-22` | `gameObject.audio` | `GetComponent<AudioSource>()` | **Removed in Unity 5.** The `.audio`/`.rigidbody`/`.camera` component shortcuts are gone. This file is dead code — delete it rather than fix it. |
| `UI.cs:80`, `Game.cs:157,176` | `Application.LoadLevel(string)` | `SceneManager.LoadScene(string)` + `using UnityEngine.SceneManagement` | Removed in 5.3. 3 call sites. |
| `Game.cs`, `Deck.cs` | `MonoBehaviour.print()` | `Debug.Log` | Still exists, but flag it — 1 call site. |

**That is the complete list of hard compile errors.** Three files, four call sites, one of which is in code that should be deleted anyway. This is unusually clean.

---

## 3. Soft breaks — compiles, behaves differently

These are the ones that will actually cost time.

### 3.1 `OnGUI` / `GUIStyle` — the entire menu system
`UI.cs` is immediate-mode `OnGUI` with hardcoded pixel coordinates (`Screen.width/2-100`, `y=600`). It still *works* in modern Unity, but:
- It is effectively deprecated for runtime UI.
- The `GUIStyle` and `Texture` fields serialized in `MainMenu.unity` may not survive cleanly.
- The layout is already broken at any resolution other than the one it was authored at — buttons are placed at y=600–770, which is off-screen below 810px tall.

**Verdict:** do not port `OnGUI`. **Rebuild the menus in uGUI** as part of the upgrade. It is ~150 lines of `OnGUI` being replaced by a scene with proper anchoring, and it is the only way the menu will work on more than one screen size. This also lets you finally delete `OldUnusedCode/`.

### 3.2 Sprite import settings and `Pixels Per Unit`
Unity 4.3 was the *first* version with the 2D sprite system, and defaults changed afterwards. Every card sprite will likely need its `Pixels Per Unit` and pivot re-checked. Combined with the hardcoded world-space positions in `Game.cs` (`x = -10 + count*2.0f`, `y = -3.0f`), **expect the board layout to be visibly wrong after upgrading.**

This is the single most likely "it looks broken" moment. It is cosmetic and it is fixable, but budget a session for it. Since the layout code is being replaced anyway (`TechnicalDebt.md` §4), fold this into that work rather than re-tuning magic numbers twice.

### 3.3 `TextMesh` sorting and rendering
The five message objects and four HUD objects in `Battle.unity` use legacy `TextMesh`. It still renders, but sorting against `SpriteRenderer` changed. Replace with TextMeshPro during the HUD work (bug M8) — the HUD needs writing from scratch regardless.

### 3.4 Colour space and quality settings
Unity 4 defaulted to gamma; modern defaults differ. Purely visual, easily corrected.

### 3.5 `System.Random` vs. `UnityEngine.Random`
`Deck.Shuffle()` and `FlipCoin()` construct `new System.Random()`. Fine in both versions — but note that constructing `System.Random` twice in quick succession on old Mono could yield identical seeds. Modern .NET seeds differently. Not a break; worth knowing when shuffles look suspicious.

---

## 4. Non-issues

Worth stating explicitly so no time is wasted on them:

- **No third-party packages, plugins, or Asset Store content.** Nothing to re-license or replace.
- **No custom shaders or materials of consequence** (one `Button Material`).
- **No Mecanim / Animator controllers.** No animation to migrate.
- **No physics simulation.** `BoxCollider`s exist purely for `OnMouseDown` raycasts.
- **No networking**, despite a `NetworkManager.asset` (it's a default settings file).
- **No `.NET` API surface issues.** The code uses `List<T>`, `System.IO.File`, `System.Random` — all still fine.
- **`RuntimePlatform` / build targets** — desktop only, no legacy platform config to strip (the Wii/PS3/BlackBerry entries in `ProjectSettings.asset` are just Unity 4 defaults).

---

## 5. Migration strategy

**Do these before opening the project in a new Unity version.**

### Phase 0 — preparation (no Unity involved)
1. **Branch.** `git checkout -b unity-upgrade`. Never upgrade on `master`.
2. **Add `.gitignore`** (Unity standard) and untrack `HToW.exe` + `HToW_Data/` — but first confirm `GameplayLoop.md` §4 has captured everything needed from `output_log.txt`.
3. **Delete the dead code and broken prefabs** listed in `TechnicalDebt.md` §7. Fewer broken references to diagnose after the upgrade. Critically, this removes `MusicManager.cs` — one of only three files with hard compile errors.
4. **Commit.** This is your known-good pre-upgrade baseline.

### Phase 1 — serialization

> **Superseded 2026-07-28: no Unity installation exists on this machine** (no Unity Hub, no editor under `Program Files`). Unity 4.3.4 therefore cannot perform the text conversion, and the fallback below applies.
>
> **Do not** attempt to flip the setting by editing `ProjectSettings/EditorSettings.asset` directly — it is binary, and the only benefit of doing so would be a reviewable upgrade diff, which is lost anyway: opening in Unity 6 performs the text conversion *and* the upgrade in a single pass regardless of when the flag is set. Binary surgery for no gain.

5. **First action after installing Unity 6 and opening the project:** set **Edit → Project Settings → Editor → Asset Serialization → Force Text**, let Unity rewrite every asset, and commit that rewrite **on its own**, changing nothing else.

   Accepted cost: the upgrade itself is one large binary→text commit that cannot be meaningfully reviewed. Every change *after* it is fully diffable, which is the point.

### Phase 2 — the jump
6. Open in **Unity 6 LTS directly.** Do not step through 5.x → 2017 → 2019 → 2021. Incremental upgrades are advice for large projects with deep package dependencies; here they would multiply the work with no benefit. Take the API Updater's automatic fixes.
7. Fix the remaining compile errors — expect only `Application.LoadLevel` × 3.
8. Commit as soon as it compiles, **before** touching anything visual.

### Phase 3 — make it run
9. Fix Build Settings: remove the two entries for deleted scenes (`BattleMenu`, `CardMenu`) and the duplicate `Battle` entry.
10. Re-check sprite import settings; expect to re-tune board layout.
11. Verify the game reaches the same (broken) state it does today. **The success criterion for the upgrade is "equally broken, not differently broken."** Do not fix gameplay bugs in the upgrade branch — that is Milestone 1 onward.

### Phase 4 — merge and proceed
12. Merge to `master`, tag it, then start `Roadmap.md` Milestone 0.

---

## 6. Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Board layout visually wrong post-upgrade | **High** | Low | Expected; the layout code is being rewritten anyway |
| `OnGUI` menu unusable at modern resolutions | **High** | Low | Already broken; rebuild in uGUI |
| Binary scenes contain unrecoverable cruft | Medium | Medium | Force Text first if Unity 4 still runs; otherwise the scenes are small enough to rebuild by hand |
| Unity 4.3.4 won't install/run on Windows 11 | Medium | Low | Skip Phase 1; do Force Text after the jump |
| Prefab references break | Low | Low | 4 of 6 prefabs are already broken and slated for deletion |
| Sprite `.meta` GUIDs regenerate, breaking references | Low | Medium | All assets have `.meta` files except three `Thumbs.db` (junk) — verified. Don't delete `.meta` files. |
| Hidden runtime API break | Low | Medium | Codebase is 35 KB; a full read-through is cheap |

---

## 7. Should you upgrade at all?

Stated fairly, because it is a real question.

**Argument for staying on 4.3.4:** the project compiles and runs today; upgrading is work that produces no new gameplay.

**Argument for upgrading — stronger:**
- Unity 4.3.4 will not reliably install or run on Windows 11, which makes the question close to moot.
- Modern TextMeshPro, uGUI, and the 2D toolchain remove work you would otherwise hand-roll — particularly for a card game, which is *mostly UI*.
- The HUD, the menus, and the card rendering all have to be built anyway (they don't exist), and building them on a 2014 immediate-mode GUI would be actively wasted effort.
- You will get no support, no documentation, and no answers for a 12-year-old version.

**Verdict: upgrade, early, before gameplay work.** The cost is low precisely because the project is small, and the cost only grows as you add content. But run Phase 0 first — deleting dead code before upgrading removes a third of the compile errors for free.
