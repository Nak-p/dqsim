# Project Overview
- Game Title: DQSim (Battle Module)
- High-Level Concept: Hexagonal grid-based battle system test.
- Players: Single player (Test mode).
- Target Platform: WebGL.
- Render Pipeline: UniversalRP (2D).

# Game Mechanics
## Core Gameplay Loop
1. Spawn a unit on the hex battlefield.
2. Select the unit to see reachable areas (based on 10 MP).
3. Click a destination to move the unit.
4. MP is consumed based on terrain cost.
5. Reset MP or respawn for further testing.

## Controls and Input Methods
- Mouse Click: Select unit, Select destination.
- UI Buttons: Spawn Unit, Reset MP.

# UI
- **Battle Test Panel**:
    - [Spawn Unit] Button.
    - [Reset MP] Button.
    - Status Text: "Unit Pos: (X, Y), MP: 10/10".

# Key Asset & Context
- `BattleUnit`: Component to track position and MP.
- `BattlePathfinder`: Dijkstra implementation for hex grid terrain costs.
- `BattleController`: Main logic for selection and movement.
- `BattleUIController`: Handles button events and status display.
- `BattleHighlightRenderer`: Tilemap overlay to show reachable hexes.

# Implementation Steps
1. **Implement `BattlePathfinder`**:
    - Create `Assets/Scripts/Battle/BattlePathfinder.cs`.
    - Implement Dijkstra's algorithm using `BattleHexMap` and `BattleTerrainCost`.
    - Provide a method `GetReachableTiles(Vector2Int start, float maxCost)`.

2. **Implement `BattleUnit`**:
    - Create `Assets/Scripts/Battle/BattleUnit.cs`.
    - Properties: `Vector2Int HexPosition`, `float MaxMP = 10`, `float CurrentMP`.
    - Logic to update world position based on `Grid`.

3. **Implement `BattleHighlightRenderer`**:
    - Create `Assets/Scripts/Battle/BattleHighlightRenderer.cs`.
    - Manage a separate `Tilemap` for highlights (e.g., semi-transparent blue).
    - Method `HighlightTiles(IEnumerable<Vector2Int> tiles)`.

4. **Implement `BattleController`**:
    - Create `Assets/Scripts/Battle/BattleController.cs`.
    - Handle `OnMouseDown` or Input System clicks to select/move.
    - Integrate `BattlePathfinder` to validate moves.

5. **Implement `BattleUIController`**:
    - Create `Assets/Scripts/Battle/BattleUIController.cs`.
    - Create a simple uGUI Canvas programmatically (matching project style in `HUDController`).
    - Connect buttons to `BattleController`.

6. **Wiring**:
    - Update `BattleFieldBootstrap` or create a new `BattleTestSetup` to initialize these components in the `BattleFieldTest` scene.

# Verification & Testing
- Spawn unit: Verify it appears on a walkable tile.
- Selection: Verify reachable tiles are highlighted (cost-aware).
- Movement: Click a tile; verify unit moves and MP decreases correctly.
- Terrain Cost: Verify Forest costs 1.5 and Swamp costs 2.0.
- Blocking: Verify unit cannot move to Water or Mountain.
