# Open and run Fit to Pass

[Project home](../README.md) · [Source guide](Code-guide.md)

## Play without the editor

[Download the Windows demo on itch.io](https://hajimi9914.itch.io/fit-to-pass). The published game is **Fit to Pass**. The current source snapshot is **v2.4.0**, with fifteen chapters across three regions.

## Unity project

1. Clone this repository, retaining its folder structure.
2. In Unity Hub, add the repository root containing `Assets`, `Packages` and `ProjectSettings`.
3. Use **Unity 6000.5.9f1** with Windows x64 build support. This is the verified project editor version, not a claim about the latest Unity release.
4. Let Unity Package Manager resolve [`manifest.json`](../Packages/manifest.json) and [`packages-lock.json`](../Packages/packages-lock.json). Git must be installed for the pinned Git package.
5. Open [`Assets/Scenes/MainMenu.unity`](../Assets/Scenes/MainMenu.unity), then enter Play mode.
6. For a Windows build, use the existing scene list in [`EditorBuildSettings.asset`](../ProjectSettings/EditorBuildSettings.asset): one menu and fifteen gameplay scenes.

Declared packages include URP **17.6.0**, Unity UI **2.0.0**, 2D Tilemap Extras **8.0.3**, ProBuilder **6.1.2** and the Unity Test Framework **1.4.5**. Unity supplies its editor modules separately. Font and asset notices are [here](Third-party-notices.md).

**Optional authoring integration:** MCP for Unity is pinned to **v10.2.0** in the manifest. It uses a separate local service when an author wants MCP control. Its server environment, account settings and connection credentials are not distributed. A server connection is not needed to run the game; simply leave the authoring panel disconnected.

## Controls

| Input | Action |
| --- | --- |
| A / D or Left / Right | Move |
| Space | Jump; hold for a higher normal jump |
| F | Collect nearby food |
| 1 | Eat grow food |
| 2 | Eat shrink food |
| E | Record a heavy echo, or activate a nearby mass-lock station |
| Q | Release the mass lock and apply stored change when there is room |
| R | Retry from the current checkpoint |
| Escape | Pause / resume |
| Tab | Acknowledge a first-encounter card once it is ready |

## Saves and scene identity

Progress and preferences are stored using Unity `PlayerPrefs`; personal saves are not part of the repository. `New Game` asks before clearing current campaign progress. For recording or tests, use a separate OS profile, or back up and restore PlayerPrefs rather than overwriting a player's run.

Scene filenames are not chapter numbers. `Level1`, `Level2` and `Level3` map to chapters **01, 06 and 11**. The twelve other active chapters use `SectorXX` scenes. [`CampaignMap.Scene()`](../Assets/Script/CampaignMap.cs) is authoritative. Retained `Level4`–`Level10` files are legacy scenes, not the current campaign order.

## Included and excluded

The source contains C#, authored scenes, resource assets, package declarations and project settings. `Library`, `Temp`, `Logs`, builds, editor recovery files, screenshots, personal Unity account configuration, local MCP environments and video-production working files are excluded. The portfolio PDFs and web media are presentation resources outside `Assets`.

The current public source was synchronized before the homepage redesign, as its own commit. [Release verification](Release-2.4.0.json) records the existing author/scripted checks; publishing documentation does not imply a fresh native rebuild or an external playtest.
