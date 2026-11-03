# Fit to Pass

A body-size puzzle-platformer about turning laboratory machinery into a way out.

**Yibo Wang — Game Design & Programming. All levels and gameplay systems.**

**Current source: v2.4.0 · Unity 6000.5.9f1 · 15 chapters · Windows x64**

[Play the current Windows demo](https://hajimi9914.itch.io/fit-to-pass) · [Watch the gameplay and design film](https://www.youtube.com/watch?v=_GRWXU7BOKU)

This commit synchronizes the current game before the portfolio homepage revision. The campaign progresses through the Greenhouse (01–05), Foundry (06–10) and Frozen Observatory (11–15). The active build has one menu and fifteen gameplay scenes; scene names are mapped explicitly by `Assets/Script/CampaignMap.cs`.

## Open the source

1. Install Unity **6000.5.9f1** with Windows build support.
2. Add this repository folder in Unity Hub. Open the folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Allow the pinned Unity Package Manager dependencies to resolve. Git is needed for the pinned MCP for Unity package.
4. Open `Assets/Scenes/MainMenu.unity` and enter Play mode. The build list is already stored in `ProjectSettings/EditorBuildSettings.asset`.

The optional MCP authoring integration targets a local server. Its private Python environment is deliberately excluded. No MCP connection is needed to play the downloadable build.

## Controls

| Input | Action |
| --- | --- |
| A / D or arrow keys | Move |
| Space | Jump; hold for a higher jump |
| F | Collect nearby food |
| 1 / 2 | Eat grow / shrink food |
| E / Q | Lock / release at a mass-lock station |
| Escape | Pause / resume |

`Assets/Script` contains the current C# game systems. `Assets/Scenes` contains the authored scenes. `Assets/Resources/Generated` contains the current generated visual assets. Legacy extra scenes are retained but are not in the active fifteen-chapter build order.

Art & Music — AI-generated. [Asset notices and dependencies](Documentation/Third-party-notices.md). [Existing release verification record](Documentation/Release-2.4.0.json).
