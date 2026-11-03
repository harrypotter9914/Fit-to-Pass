# Follow a player decision into the source

[Project home](../README.md) · [Level cases](Level-design.md) · [Open and run](Running-the-project.md)

These links lead to the current Unity C# implementation. Read the player situation first, then follow the named entry point. **Yibo Wang — Game Design & Programming. All levels and gameplay systems.**

## “I need to be small, but there is a ceiling above me.”

[`PlayerController.cs`](../Assets/Script/PlayerController.cs) combines continuous mass with three body categories. `FixedUpdate()` applies body-dependent movement/jumping and movement/rest metabolism. `Consume()` feeds the same `ChangeWeight()` path as passive changes. `CanFit()` checks headroom before growth, while `ApplySize()` preserves the feet position.

**Design connection:** body size affects several rules without adding separate movement abilities. Growth rejection is readable feedback rather than letting the player expand into geometry. Region-specific metabolism is assigned in [`GameFlow.SceneLoaded()`](../Assets/Script/GameFlow.cs), using [`CampaignMap.Region()`](../Assets/Script/CampaignMap.cs).

## “I opened the stone. Why is it closing again?”

[`PlatformAndRockController.FixedUpdate()`](../Assets/Script/LevelTwo/PlatformAndRockController.cs) evaluates rider size every physics tick: heavy reverses toward the lowered plate/raised stone; medium returns without moving either; light or an empty plate returns toward the original position. There is no empty-plate hold timer.

**Design connection:** changing size on the plate can interrupt or balance motion; stepping off creates Slag Return's visible deadline. [`RegionalRoutes.SlagReturn()`](../Assets/Script/RegionalRoutes.cs) places supplies, the return approach and the later upper loop. The related [`ElevatorController`](../Assets/Script/LevelThree/ElevatorController.cs) uses small/up, medium/hold and heavy/down for mass lifts.

## “I am at the right height. How do I stay here?”

[`VectorAirwell.FixedUpdate()`](../Assets/Script/VectorAirwell.cs) runs after normal player movement. Tiered fields converge toward a body-dependent feet height. Free fields use small/rise, medium/zero vertical target and heavy/descent. Horizontal input remains the player's ordinary movement.

**Design connection:** `AuroraDescent()` uses two separate shafts and a middle side port; the same three body states first select a destination, then control the timing of a sideways exit.

## “The carriage reached the end, but my exit link is not stored.”

[`RailShuttle.FixedUpdate()`](../Assets/Script/RailShuttle.cs) separates riding, direction, terminal arrival, return initiation and dock completion. On a `requireReturn` line, `TurnaroundReached` and `ReturnLegStarted` must precede the medium, stationary stop near cyan. `RestoreDock()` restores the completed state on retry.

**Design connection:** chapter 09's second rail asks for a round trip and a deliberate brake, rather than awarding an objective for merely riding right. The first and second lines have different requirements.

## “What does this prism power, and why is the door still locked?”

[`MassPrism.FixedUpdate()`](../Assets/Script/MassPrism.cs) stores three receiver charges; `Complete` requires all three. [`OpticalLiftPower.Connect()`](../Assets/Script/OpticalLiftPower.cs) binds that completion to the freight call's `PowerReady` predicate and creates its visible P1 connection.

[`RouteCircuit.cs`](../Assets/Script/RouteCircuit.cs) combines body relays and other interlocks. `MissingReason`, `NextSource` and `NextObjective` expose the missing prerequisite; `Complete` controls whether the exit is ready. [`BodyRelay.FixedUpdate()`](../Assets/Script/BodyRelay.cs) requires the correct body on the correct landing before recording a link.

**Design connection:** the HUD, source and lock share identifiers. This makes a whole-level dependency legible instead of giving every closed door the same unexplained rejection.

## “I need heavy weight here and a small body over there.”

[`MassEcho.Record()`](../Assets/Script/MassEcho.cs) accepts a grounded heavy body and an explicit **E** interaction. Its temporary recorded load powers a linked gate while the player shrinks. Simply standing heavy on the pad is insufficient.

[`MassLock.Activate/Buffer/Release()`](../Assets/Script/MassLock.cs) holds the physical body while storing future weight changes. Releasing applies those changes through the normal clearance check; if growth is blocked, it waits for space. [`MassLockStation`](../Assets/Script/MassLockStation.cs) handles the nearby activation.

**Design connection:** echo separates a body's presence from its owner; mass lock separates the current shape from its prepared next state. They solve different planning problems.

## “A heavy landing builds a bridge; a wrong scan tells me why.”

[`KineticCapacitor.OnCollisionEnter2D()`](../Assets/Script/KineticCapacitor.cs) checks impact and body category before energizing the temporary crossing. `FixedUpdate()` drains stored energy; a heavy crossing consumes it faster. Standing on the core alone does not charge it.

[`MassGate.FixedUpdate()`](../Assets/Script/MassGate.cs) distinguishes wrong size, movement and a valid still scan. A completed scan opens the physical shutter continuously into its recess. For conventional launch/weight responses, see [`SpringController`](../Assets/Script/LevelThree/SpringController.cs) and [`BalanceBridge`](../Assets/Script/BalanceBridge.cs).

## “This is my first encounter. I do not know what E does.”

[`FirstEncounterGuide.cs`](../Assets/Script/FirstEncounterGuide.cs) defines each lesson's purpose, steps, keys and source/target relationship. Explicit acknowledgement is tracked separately from action completion. Whole-circuit overviews and local device explanations use different flows, and local cards have a minimum readable duration.

[`WorldGuideView`](../Assets/Script/WorldGuideView.cs) handles contextual world guidance. [`IllustratedUI`](../Assets/Script/IllustratedUI.cs) draws the reusable diagrams. The current first-encounter system avoids introducing a device from a different floor and clears unread queues at chapter boundaries.

## “I fell after solving the optical circuit. Must I redo everything?”

[`GameFlow.Checkpoint/Retry()`](../Assets/Script/GameFlow.cs) records position, body mass, food and remaining pickups. [`CampaignCheckpointState.Capture/Restore()`](../Assets/Script/CampaignCheckpointState.cs) preserves completed relays, optical charge and rail terminal/return/dock state. Temporary effects are intentionally not promoted to permanent accomplishments.

[`CheckpointBeacon`](../Assets/Script/GamePresentation.cs) requires at least medium-sized clearance before accepting a small-body touch. [`RespawnSafety.Find()`](../Assets/Script/RespawnSafety.cs) searches supported, clear floor positions; the restored body is checked again after scene loading.

[`CameraFollow.LateUpdate()`](../Assets/Script/CameraFollow.cs) follows the interpolated actor with a horizontal dead zone, no velocity-based look-ahead and no automatic horizontal recentring. Vertical framing uses the feet so changing shape does not bob the room. [See the problem and revision](Design-iteration.md#camera-comfort-stop-means-stop).

## Project map

| Location | What to read |
| --- | --- |
| [`CampaignMap.cs`](../Assets/Script/CampaignMap.cs) | Fifteen chapter identities, regions, scene mapping and old-save migration. |
| [`RegionalRoutes.cs`](../Assets/Script/RegionalRoutes.cs) | Individually authored geometry and device arrangements for twelve later chapters. |
| [`CampaignExpansion.cs`](../Assets/Script/CampaignExpansion.cs) | Runtime route selection and reusable placement helpers. |
| [`Assets/Scenes`](../Assets/Scenes) | Three authored region foundations, sector scenes and menu. |
| [`Assets/Editor`](../Assets/Editor) | Editor-side build, placement and regression helpers; excluded from the player build. |
| [`Packages/manifest.json`](../Packages/manifest.json) | Declared dependencies, including pinned optional authoring integration. |
| [`ProjectSettings/EditorBuildSettings.asset`](../ProjectSettings/EditorBuildSettings.asset) | Actual menu + fifteen-chapter build order. |

Some legacy helper names still say `Annex`; the active campaign no longer uses an annex teleport. Old extra scene files remain for history/compatibility and are not extra active campaign chapters. Source organization preserves Unity GUIDs and scene references rather than renaming serialized resources for presentation.
