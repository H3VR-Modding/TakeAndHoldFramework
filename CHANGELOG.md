## [0.1.0] - 2024-07-04
- First release, based on devyndamonster-TakeAndHoldTweaker 1.7.9.
- Removed requirements for Deli and Magazine Patcher, now just optional.
- Swapped plugin from loading via Deli to loading via Stratum.
- Added support for loading custom characters via Stratum. Documentation is TBD.
- YAML support added for character, sosig and vault files. Field format for characters has been overhauled, so they're not the same as the fields in character.json.
- Added support for V2 vault files. Support is only available for Stratum-based custom characters.
- Support is now included for Institution. Plugin checks if the current character is a vanilla character and will try to replicate Institution's changes if it is. If it's a custom character, it tries to be more faithful.
- Added a Disabler feature. This is designed to allow for installing the original version alongside this custom fork - old custom characters require the original to function, but having both installed at the same time will conflict, thus, this renames the TakeAndHoldTweaker.deli file to prevent it from loading. It's not great, but it works.
- Improved non-Magazine Patcher functionality. Had issues with Magazine Patcher not being present. This issue hasn't been reported, so it might've merely been an individual issue. Nonetheless, this should be better.
- Removed HasPrimaryWeapon, HasSecondaryWeapon, booleans and just enable the pools based on whether they have been populated or not.
- Removed "RequiredQuest", as it was an unfinished feature that didn't do anything.
- Stratum only: Added "DespawnBetweenWaves" and "UsesVFX" booleans.
  - DespawnBetweeenWaves (default=true): Causes any remaining sosigs to explode when the encryption phase in a Hold is completed.
  - UsesVFX (default=true): Plays the "Encryption neatralized" announcer line when the encryption phase in a Hold is completed.
- Prevent round types that cost tokens from being chosen when purchasing a firearm from the object constructor. Ammo that is spawned with the firearm is chosen from the list of "free" ones.
- Expanded the character selection UI to allow more characters per category.
- Fixed the bug where selecting past the 6th character wouldn't work.

## [0.1.1] - 2024-07-04
- Changed manifest to say TNHFramework instead of TNHTweaker. 

## [0.1.2] - 2024-07-05
- Added EnableScoring boolean to config in order to allow uploading of scores to custom TNH Dashboard.
- Fixed character loader to load one character at a time.

## [0.1.3] - 2024-09-03
- EDIT: Forgot to push changes, so 0.1.3 is no different from 0.1.2.

## [0.1.4] - 2024-09-05
- Updated logger to say TNHFramework instead of TNHTweaker.
- Fixed sosig item drops (e.g. PMC Pete, PorkUnknown).
- Fixed patrol cadence. Patrols were always spawning immediately.

## [0.1.5] - 2024-10-31

### Features

- Implemented sentry patrols (used in Institution) with loot and custom configs. Previously, it was only using non-Institution patrol behavior.
- Added scanbox effect to all new panels when you place a valid item in the scan area.
- Show total tokens and token cost on new panels when possible.
- Added config file option to always change Mag Duplicator panel to Mag Upgrade panel, which was the default behavior in TNHTweaker.
  - When option is disabled, "MagDuplicator" will be the old panel (with 2 options), while "MagUpgrader" will be the new one (with 3 options).
- Added appropriate text title to each new panel.
- Spawn each type of patrol before the same type is allowed to spawn again.
- Added GlobalObjectBlacklist to character JSON.
- Copy new vault files to output directory when using BuildCharacterFiles option.
- Added "DisplayName" and "IsModdedContent" columns to Objects.csv when using BuildCharacterFiles option.
- Show SosigEnemyIDs instead of numbers for custom sosigs in SosigIDs.txt when using BuildCharacterFiles option.
- Filter out uninitialized data in internal mag patcher.
- Added option to disable internal mag patcher even if MagazinePatcher is not detected.
- Track all objects so they can be cleaned up quicker in order to save memory.
- Vanilla characters use original behavior for supply boxes and token spawns, but custom characters use custom behavior.

### Fixes

- Fixed sosig item drops not working in Institution.
- Fixed MagazinePatcher not being detected correctly.
- Fixed random loot calls to use correct weights when subgroups are present.
- Fixed random loot calls to be more robust.
- Fixed scoring stats not being reset after Hold phase.
- Fixed check that all components exist in vault guns.
- Fixed logging not working when loading through Deli.
- Fixed max number of sentry patrols.
- Fixed loading of custom characters by validating JSON input.
- Fixed tag handling for character and sosig JSON deserialization.
- Fixed BespokeAttachmentChance not being checked when spawning a weapon case.
- Fixed large cases being allowed to be purchased multiple times, causing clipping issues. Cases can only be purchased once per object constructor.
- Fixed grenade spawn errors during Hold phase if GrenadeVectors do not exist.
- Fixed sosigs being alerted when alert system is not enabled (vanilla bug).
- Fixed GenerateSentryPatrol being called with incorrect parameters (vanilla bug).
- Fixed wrong table being used in GetRandomSafeSupplyIndexFromHoldPoint (vanilla bug).
- Fixed error sound being played when spawning a clip at the ammo reloader (vanilla bug).
- Fixed vault pump action shotguns not spawning with the correct safety and chamber states (vanilla bug).
- Fixed vault open bolt guns not spawning with the correct safety and chamber states (vanilla bug).
- Fixed objects like loot drops failing to spawn sometimes.

### Miscellaneous

- Removed JSONBuilder because it wasn't being used and doesn't do anything anymore.
- Removed high score submission because custom TNH Dashboard is permanently offline. This has been offline since before TNHFramework was created.
- Internal mag patcher waits until OtherLoader finishes loading before running.
- Internal mag patcher loads GameObjects when necessary to obtain metadata, unlike MagazinePatcher which loads ALL objects.
- Remove dead sosigs BEFORE checking squads, instead of during the check itself.
- Changed spawning method of vault guns to hopefully load them a little quicker.

## [0.2.1] - 2025-XX-XX

- Fixed patrol algorithm from skipping patrol types occassionally.
- Fixed the case where the ammo blacklist removes all compatible rounds. It will now leave at least one type of round for each gun at the ammo spawner.
- Fixed panels (ammo reloader, etc.) sometimes breaking when being used a lot.
- Fixed the case where the secondary or tertiary starting item spawn point is missing from the map. This caused some characters to not work on certain maps (e.g. Aporkalypse Now).
- Fixed compatibility with Ammo Pouch mod.
- Respect the HasPrimaryWeapon, HasSecondaryWeapon, etc. booleans. Some mod characters have an equipment pool populated, but disabled via boolean. This makes it consistent with old behavior.
- Don't play the announcer line to advance to the next node after finishing the last hold (vanilla bug).
