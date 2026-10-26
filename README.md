# RecipeHelper

Shows a sidebar beside the Wandering CHEF's crafting dialog listing everything you can craft with the items you have, including results you can only reach by crafting an intermediate item first.

## What you see

- While the CHEF's dialog is open, a list of results on the right side of the screen, nearest first: what you can make now, then what you can make in 2 crafts, then 3.
- Under each result, the crafts to make it, in order, for example `A + B → C`, then `C + D → E`.
- The list updates as your inventory changes.
- Only permanent items count, as in the CHEF itself. Recipes from expansions that aren't enabled in the run are left out.

The mod never changes gameplay and never sends anything over the network. It works when you join a host who doesn't have it installed.

## Configuration

Settings are in `BepInEx/config/sevenstack.RecipeHelper.cfg`. If [Risk Of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) is installed, they are also in its in-game menu.

| Setting | Default | Description |
|---|---|---|
| Sidebar / Enabled | true | Show the list beside the CHEF's dialog. Takes effect on the next stage or HUD change. |
| Sidebar / MaxChainSteps | 3 | The most crafts in a row to look ahead (1–5). |
| Sidebar / MaxRows | 14 | The most results listed at once (4–40). The rest are summarized as a count. |
| Sidebar / SidebarWidth | 360 | Width of the sidebar in UI pixels. |
| Debug / DebugLogging | false | Enables the dump key. |
| Debug / DebugAlwaysShow | false | Lists every recipe regardless of your inventory, for testing the layout. |
| Debug / DumpKey | F8 | With DebugLogging on, logs the recipe count and the plan for your inventory. |

## Notes

- Crafting an equipment swaps it for the one you hold rather than using it up, so a chain that ends in equipment doesn't count your old equipment as lost.
- Looking far ahead with a large inventory can take a while, so the search stops at a fixed number of states. The sidebar says so when it does.

## Building

Requires the .NET 8 SDK and a Risk of Rain 2 install. Paths default to the Linux Steam location and the r2modman Default profile; override them with `-p:RoR2Dir=...` and `-p:ProfileDir=...` or in a `Directory.Build.props.user` file.

```bash
dotnet build -c Release   # also copies the plugin into the profile; add -p:CopyToProfile=false to skip
./build.sh                # builds and writes dist/RecipeHelper-<version>.zip
```

To release: bump `version_number` in `thunderstore/manifest.json`, add a `CHANGELOG.md` entry, run `./build.sh`, then publish a GitHub release tagged `v<version>` with the zip attached. The release workflow uploads it to Thunderstore.
