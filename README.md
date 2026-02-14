# Sorting Chest

This is a lightweight server-side mod that turns renamed storage container into an automatic item sorter.
When the sorter chest inventory is closed, its contents are redistributed into nearby storage containers based on existing item types.

------------------------------------------------------------------------

## How It Works

1. Place any standard storage container.
2. Rename it exactly to `[sort]`
3. Put items into this chest.
4. Close the container.

As soon as you close the chest UI, the server automatically:

- Scans nearby storage containers within a **20-block radius**
- Detects which item types already exist in those containers
- Moves matching items from the `[sort]` chest into them
- Fills partially filled stacks first

There is no button and no manual activation --- sorting happens automatically on close.

------------------------------------------------------------------------

## Sorting Rules

- **Radius:** 20 blocks
- Only loot containers are considered
- Items are only moved into containers that already contain the same item type
- Existing stacks are filled before new stacks are created
- No random distribution; no category rules required
- All logic runs server-side
- Ignoring other `[sort]` chests

Drop items into the `[sort]` chest, close it, and the mod distributes them into your storage system.

------------------------------------------------------------------------

## Installation

1. Copy the mod folder into: `7 Days To Die Dedicated Server/Mods/`
2. Restart the server.

No world reset or regeneration is required.  
No special startup parameters or configuration changes are needed.

The mod can be added to an existing world safely and will start working immediately after the server restart.

## Uninstallation

To remove the mod, simply delete its folder from `7 Days To Die Dedicated Server/Mods/` and restart the server.

The removal is safe and has no side effects.
No world data is modified permanently, and no cleanup steps are required.
Once removed, the automatic sorting functionality will simply stop working.

------------------------------------------------------------------------

## Dedicated Server Compatibility

This mod is fully server-side:

- No client installation required
- EAC can remain enabled
- Players do not need to install anything locally

All sorting logic is handled entirely by the server.

## Single Player / Client Installation (Possible but Not Recommended)

This mod is designed primarily for dedicated server use.

It may theoretically work in single player if installed locally, however:

- EAC must be disabled
- This configuration has not been extensively tested
- Stability and compatibility in client-only environments are not guaranteed

Use at your own discretion.