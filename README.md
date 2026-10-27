# RecipeHelper

Shows a sidebar beside the Wandering CHEF's crafting dialog listing everything you can craft with the items you have, including results you can only reach by crafting an intermediate item first.

## What you see

- While the CHEF's dialog is open, a list of results in the space left of the dialog, continuing on its right when it doesn't fit. Results are grouped by how many crafts they take (now, then 2 crafts, then 3), rarest first within each group.
- The crafts to make each result, in order, drawn with item icons, for example `A + B → C`, then `C + D → E`. Items an earlier craft makes have a faint frame.
- Under a slot, the other items you hold that would also fit it.
- Three styles:
  - **List** (the default): a row per result showing its crafts, with the later crafts of a chain dimmed. Click a row to put the first craft's items in the CHEF's slots; the row then says "In slots". You still confirm the craft yourself.
  - **Cards**: a card per result.
  - **Grid**: an icon grid of results, with a panel showing how to make the one you click.
- The list updates as your inventory changes.
- Only permanent items count, as in the CHEF itself. Recipes from expansions that aren't enabled in the run are left out.

The mod never changes gameplay. The only thing it sends to the host is the List style's slot filling, which makes the same picks as clicking the items in the CHEF's dialog yourself. It works when you join a host who doesn't have it installed.

## Configuration

Settings are in `BepInEx/config/sevenstack.RecipeHelper.cfg`. If [Risk Of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) is installed, they are also in its in-game menu.

| Setting | Default | Description |
|---|---|---|
| Sidebar / Enabled | true | Show the list beside the CHEF's dialog. Takes effect on the next stage or HUD change. |
| Sidebar / MaxChainSteps | 3 | The most crafts in a row to look ahead (1–5). |
| Sidebar / Style | List | List: a row per result showing its crafts; click a row to put the first craft in the CHEF's slots. Cards: a card per result showing each craft as item icons. Grid: the results as an icon grid; click one to see how to make it. |
| Debug / DebugLogging | false | Enables the dump key. |
| Debug / DebugAlwaysShow | false | Lists every recipe regardless of your inventory, for testing the layout. |
| Debug / DumpKey | F8 | With DebugLogging on, logs the recipe count and the plan for your inventory. |

## Notes

- Filling the slots only covers the next craft of a chain. Once it's cooked, talk to the CHEF again for the next one.
- Crafting an equipment swaps it for the one you hold rather than using it up, so a chain that ends in equipment doesn't count your old equipment as lost.
- Looking far ahead with a large inventory can take a while, so the search stops at a fixed number of states. The sidebar says so when it does.

## Building

Requires the .NET 8 SDK and a Risk of Rain 2 install. Paths default to the Linux Steam location and the r2modman Default profile; override them with `-p:RoR2Dir=...` and `-p:ProfileDir=...` or in a `Directory.Build.props.user` file.

```bash
dotnet build -c Release   # also copies the plugin into the profile; add -p:CopyToProfile=false to skip
./build.sh                # builds and writes dist/RecipeHelper-<version>.zip
```

To release: bump `version_number` in `thunderstore/manifest.json`, add a `CHANGELOG.md` entry, run `./build.sh`, then publish a GitHub release tagged `v<version>` with the zip attached. The release workflow uploads it to Thunderstore.
