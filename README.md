# SpaceEngineers4Linux

## What is this?

Experiments to run Space Engineers on .NET. Related experiment: [SpaceEngineers4Linux](https://github.com/opekope2/SpaceEngineers4Linux), which is a superset of this project. This project's goal is only .NET 6, on Windows or in Wine (not native Linux).

## How to set up

1. I recommend using [JetBrains Rider](https://www.jetbrains.com/rider/)
2. Install .NET 6 SDK
3. Open `Loader/Loader.csproj` and add your Space Engineers Bin64 path in between `<SpaceEngineersBin64></SpaceEngineersBin64>`
4. Build and publish project `dotnet publish Loader -c Release -o dist -r win-x64 --self-contained` (you can do `-c Debug`)
5. Open `/loader.py` and edit the published loader path on line 14 (Linux-only)
6. Set steam launch options to `C:\path\to\SpaceEngineersLoader\dist\Loader.exe %command%` (Windows) or `/path/to/SpaceEngineersLoader/dist/loader.py %command%` (Linux). I recommend testing without any options, since this is an experiment
7. Start Space Engineers

## What is the furthest point the game runs?

Past splash screen, when it crashes. This doesn't happen on .NET Framework 4.8.

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at Sandbox.Definitions.MyDefinitionManager.InitDefinition[T](MyModContext context, MyObjectBuilder_DefinitionBase builder)
   at Sandbox.Definitions.MyDefinitionManager.InitScenarioDefinitions(MyModContext context, DefinitionDictionary`1 outputDefinitions, List`1 outputScenarios, MyObjectBuilder_ScenarioDefinition[] scenarios, Boolean failOnDebug)
   at Sandbox.Definitions.MyDefinitionManager.LoadScenarios(MyModContext context, DefinitionSet definitionSet, Boolean failOnDebug)
   at Sandbox.Definitions.MyDefinitionManager.LoadScenarios()
   at Sandbox.MySandboxGame.LoadData()
   at Sandbox.MySandboxGame.Initialize()
   at Sandbox.MySandboxGame.Run(Boolean customRenderLoop, Action disposeSplashScreen)
   at SpaceEngineers.MyProgram.Main(String[] args)
   at Loader.Load(String[] args)
   at Loader.Main(String[] args)
```

This is the crashing method. What I figured out, is `CreateInstance<T>(builder.GetType())` returns null only when being called form this method. If I create a Harmony prefix to `InitScenarioDefinitions` and execute the first line in it, it returns a non-null object. What?

```cs
private static T InitDefinition<T>(MyModContext context, MyObjectBuilder_DefinitionBase builder) where T : MyDefinitionBase
{
    T val = MyDefinitionManagerBase.GetObjectFactory().CreateInstance<T>(builder.GetType());
    val.Context = new MyModContext();
    val.Context.Init(context);
    if (!context.IsBaseGame)
    {
        UpdateModableContent(val.Context, builder);
    }
    val.Init(builder, val.Context);
    if (MyFakes.ENABLE_ALL_IN_SURVIVAL)
    {
        val.AvailableInSurvival = true;
    }
    return val;
}
```