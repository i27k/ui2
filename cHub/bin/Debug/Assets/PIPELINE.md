# c/Hub original content pipeline

This directory deliberately separates concept art from runtime content. A PNG is
never referenced as a 3D mesh or animation.

## Required local toolchain

- Blender 4.x for mesh, UV, rig, weights, LODs and FBX export.
- The Unity editor version matching the installed 7DTD build.
- The official/current 7DTD modding project or compatible AssetBundle template.
- An audio editor/exporter capable of producing the codec/settings supported by
  that Unity version.

None of Blender, Unity or Unity Hub was installed during the 2026-08-10 audit.
Consequently no runtime AssetBundle has been fabricated or claimed as tested.

## Runtime acceptance gates

1. Every mesh has valid UVs, tangents, three LODs and no missing materials.
2. Character weights are checked on all attack, hit and death clips.
3. Weapon clips are authored for both first- and third-person presentation.
4. Audio events use unique c/Hub names and never replace global vanilla events.
5. The bundle is built for the exact game/Unity version and loaded on a clean
   client before XML references are enabled.
6. Dedicated-server packages omit graphical source files but keep matching item
   and entity IDs; clients receive the same versioned content bundle.
7. Multiplayer soak test: join/leave, death, chunk transition, Blood Moon,
   inventory serialization, held-item switching and late client connection.

The authoritative inventory of expected assets is `asset-manifest.json`.
