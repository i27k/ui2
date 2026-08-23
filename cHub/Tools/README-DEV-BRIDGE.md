# c/Hub Dev Bridge

## Current state

- `c/Hub AI` already uses the OpenAI Responses API server-side.
- Questions are rate-limited and stored under `Data/GameAssistantReview`.
- `Setup-cHubOpenAI.ps1` securely configures `OPENAI_API_KEY` for Windows.
- No API key is stored in the repository, DLL, XML, JSON, or player client UI.

## Safe bridge boundary

The Dev Bridge will listen only on `127.0.0.1`, generate a local session token,
and allow access only after the game server confirms the c/Hub `Owner` role.
It will expose allowlisted operations:

1. inspect project/build/log state;
2. edit c/Hub JSON and theme data with atomic backup and rollback;
3. validate and stage XUi XML changes;
4. request a clean build and display diagnostics;
5. stage a deployment after an explicit confirmation;
6. connect a Codex development session through an external local process.

DLL changes cannot be hot-reloaded into the active Unity AppDomain. They require
a clean build followed by a controlled game restart. JSON/theme changes can be
live; compatible XUi changes can reload their affected window group.

The bridge must never expose arbitrary shell execution or unrestricted paths to
the game client.
