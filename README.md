# SpaceEngineersLoader

This comes with absolutely no warranty. Please do not report bugs to Keen if using this.

## What is this?

Experiments to run Space Engineers on .NET. Related experiment: [SpaceEngineers4Linux](https://github.com/opekope2/SpaceEngineers4Linux), which is a superset of this project. This project's goal is only .NET 6, on Windows or in Wine (not native Linux).

## How to set up

1. I recommend using [JetBrains Rider](https://www.jetbrains.com/rider/)
2. Install .NET 6 SDK
3. Open `Loader.csproj` and add your Space Engineers Bin64 path in between `<SpaceEngineersBin64></SpaceEngineersBin64>`
4. Build and publish project `dotnet publish -c Release -o dist -r win-x64 --self-contained` (you can do `-c Debug`)
5. Create a folder named `Loader` in the game's `Bin64` folder
6. Copy the content of the `dist` foldder to the `Loader` folder you just created
7. Copy `SpaceEngineersLoader.py` into the game's `Bin64` folder (Linux-only)
8. Set steam launch options to `Loader\SpaceEngineersLoader.exe %command%` (Windows) or `./SpaceEngineersLoader.py %command%` (Linux). I recommend testing without any options, since this is an experiment
9. Start Space Engineers

## What is the furthest point the game runs?

When the Automatons update got released, I tried this again, and now it mostly works. Multiplayer does'nt, it crashes.
Huge thanks to [.NET 7 Torch](https://github.com/PveTeam/Torch) for inspiration and scripting fix.
