# GGJ26 - Unity Project

## Project Overview

This is a Unity project developed for the **Global Game Jam 2026**. The game features a "Mask" character with a unique possession mechanic, allowing the player to take control of enemy units.

**Core Mechanics:**

*   **Mask Movement:** The main character (Mask) moves primarily through physics-based jumps and lunges (`MaskController.cs`). It has a recovery time after landing before it can move again.
*   **Possession:** The Mask can possess specific enemies (`PossessableEnemy`).
    *   **Host Control:** When possessed, the camera follows the enemy, and the player gains control over their movement and attacks.
    *   **Stability:** Hosts have a "Stability" stat that decays over time.
    *   **Enlightenment/Death:** If the player ejects from a host when stability is critically low, the host dies ("Enlightened"). If stability is high, the enemy survives and returns to its AI state.
*   **Enemy AI:** Enemies use a state machine (Idle, Chase, Attack) powered by `NavMeshAgent`.

## Directory Structure

*   **`Assets/Scripts`**: Contains the core custom logic.
    *   `Player/`: Scripts for the Mask controller and Possession logic.
    *   `EnemyAI/`: Enemy behaviors, state machines, and inheritance hierarchy (`EnemyBase`, `PossessableEnemy`).
*   **`Assets/Scenes`**: Development scenes (`_FatihScene`, `_MehmetScene`).
*   **`Assets/Plugins`**: External libraries including **DOTween** and **MicroBar**.
*   **`Assets/StarterAssets`**: Unity's standard assets package (Input System, Third Person Controller), likely used for prototyping or auxiliary systems.
*   **`Assets/InputSystem_Actions.inputactions`**: Configuration for Unity's new Input System.

## Development Conventions

*   **Input Handling:** The project currently appears to use **Legacy Input** (`Input.GetAxis`, `Input.GetKeyDown`) in its custom scripts (`MaskController`, `PossessableEnemy`), despite the presence of the new Input System assets. Ensure the project's Input Manager settings are set to "Both" or "Legacy" to avoid errors.
*   **Architecture:**
    *   **Singleton Pattern:** Used for major managers like `PossessionManager`.
    *   **Inheritance:** Heavy use of inheritance for enemies (`EnemyBase` -> `PossessableEnemy` -> specific types).
    *   **Namespaces:** Code is organized into namespaces like `Player` and `EnemyAI`.
*   **AI:** Built on Unity's `NavMeshAgent`.

## Building and Running

1.  **Open Project:** Open the project directory in Unity (2022.3+ recommended, check ProjectSettings if unsure).
2.  **Scene:** Open `Assets/Scenes/_FatihScene.unity` to see the latest gameplay mechanics in action.
3.  **Play:** Press the Play button in the Editor.
4.  **Controls (Default):**
    *   **WASD / Arrow Keys:** Move (Mask jumps/lunges).
    *   **Space:** Eject from a possessed host.
    *   **Fire1 (Ctrl/Mouse0):** Attack (while possessing).
