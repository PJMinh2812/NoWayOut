<div align="center">

# 🗡️ NoWayOut

**A 2D top-down dungeon crawler with procedurally generated levels, built in Unity.**

![Unity](https://img.shields.io/badge/Unity-6000.3.2f1-black?logo=unity)
![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.0.3-blue)
![Language](https://img.shields.io/badge/language-C%23-239120?logo=c-sharp)
![Status](https://img.shields.io/badge/status-in%20development-yellow)

</div>

---

## About

**NoWayOut** is a 2D roguelite dungeon crawler. Every run generates a brand-new dungeon — rooms, doors, decorations, traps, and enemies are all placed procedurally, and an AI Director watches how well the player is performing to tune the difficulty of the next map. Fight through enemies and bosses, solve light-mirror puzzles, gear up, and push toward one of multiple endings.

Built as a course project for **PRU213** at FPT University.

## Table of Contents

- [🗡️ NoWayOut](#️-nowayout)
  - [About](#about)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Tech Stack](#tech-stack)
  - [Project Structure](#project-structure)
  - [Getting Started](#getting-started)
  - [Gameplay Systems](#gameplay-systems)
  - [Contributors](#contributors)

## Features

- 🧩 **Procedural Dungeon Generation** — every run builds a new room/door graph with automatic floor, wall, and decoration placement.
- 🤖 **AI Director & Run Telemetry** — tracks per-map player performance and adapts the next map's difficulty accordingly.
- ⚔️ **Combat** — melee attacks, ranged projectiles, a spell/hotbar system, and environmental traps.
- 👹 **Enemies & Bosses** — regular enemies, a mini-boss, and full bosses, each with their own animations and cinematics.
- 🧍 **Player Systems** — health, mana, dash with after-image effect, status effects, weapon equipment, inventory & hotbar.
- 🔮 **Puzzles** — light-mirror/receiver reflection puzzles that unlock hidden doors.
- 🚪 **Progression** — checkpoints, goal chests, goal portals for map transitions, a minimap, and good/bad endings with video playback.
- 🖥️ **UI** — HUD, pause menu, audio settings, game over screen, main menu.
- 💡 **2D Lighting** — per-room lighting and door-based visibility via URP.

## Tech Stack

|                     |                                                             |
| ------------------- | ----------------------------------------------------------- |
| **Engine**          | Unity 6000.3.2f1                                            |
| **Render Pipeline** | Universal Render Pipeline (URP) 17.0.3                      |
| **Language**        | C#                                                          |
| **Input**           | Unity Input System                                          |
| **Other packages**  | 2D Animation, SpriteShape, Tilemap Extras, 2D Pixel Perfect |

## Project Structure

```
Assets/
  Scripts/
    ProceduralGeneration/   # Map generation: rooms, doors, decor, visual generator
    Core/                   # Bootstrap, state machine, scene loader, global managers
    MapManager/              # Run progression, spawning, boss management, checkpoints
    Player/                  # Controller, combat, animation, status effects
    Enemies/                 # Enemy2D + bosses
    Combat/, Spell/, Items/, Inventory/
    Puzzle/                  # Light-mirror puzzle system
    UI/                      # HUD, menus, health/mana bars, minimap
    Camera/, Audio/, Traps/, Environment/
  Scenes/                    # MainMenu, IntroScene, SampleScene, Aethon, ...
```

## Getting Started

1. Install **Unity Hub** and Unity **6000.3.2f1**.
2. Clone the repository:
   ```bash
   git clone https://github.com/PJMinh2812/NoWayOut.git
   ```
3. Open the project folder from Unity Hub and let it import.
4. Open `Assets/Scenes/MainMenu.unity` and press Play to run the game from the main menu, or open `Assets/Scenes/SampleScene.unity` to jump straight into gameplay for testing.

## Gameplay Systems

- **Dungeon generation** — rooms are laid out as a connected graph, then decorated and populated with traps/enemies at instantiation time.
- **Run progression** — clearing a map opens a goal portal (or a goal chest on milestone maps) that carries the player to the next map, with the AI Director adjusting pacing along the way.
- **Combat loop** — melee, projectiles, and spells share a common player controller, with status effects and a hotbar for equipped items.
- **Endings** — how a run is played (deaths, time, damage taken) feeds into which ending — good or bad — plays out at the end.

## Contributors

- PJMinh2812
- gwould
- zawy2004
