# c/Hub runtime asset signature

Every original c/Hub entity, item, block, vehicle or structure must be visibly and
technically attributable without depending on its display name.

## Required visual signature

- One gear/skull or divided `c/Hub` mark integrated into the object, never floated
  on top as an unrelated sticker.
- Blackened metal, aged dark organic material and one controlled emissive accent.
- Default accent: deep crimson. Rot-series creatures additionally use oxidized
  copper and sickly amber/green details.
- A recognizable asymmetric silhouette that remains readable at gameplay distance.
- No copied silhouettes, costumes, logos, names or texture layouts from other games.

## Required technical signature

- IDs begin with the content category and contain `CHub`, for example
  `gunCHubNightglassR01`; legacy `zombieZombiIon` remains stable for saved worlds.
- Tags include `chub` plus a family tag such as `chub_rot` or `chub_nightglass`.
- Custom sounds use the `chub_` prefix and never override global vanilla events.
- Bundle assets live below `Assets/cHub/` and use explicit versioned manifests.
- Server owns AI decisions, damage, drops, score and spawning. Clients own only
  presentation and interpolation.
- Every held item needs first-person and third-person validation, reload interruption,
  empty reload, weapon swap, death-drop and late-join tests.

## Quality gates

- No missing material, pink shader, invalid bone, unbounded animation event or
  non-deterministic gameplay decision may ship.
- New AI extends the game's navigation instead of running a second pathfinder per
  frame. Expensive perception is staggered and server-authoritative.
- Texture size follows visible density; file size is not a quality target.
