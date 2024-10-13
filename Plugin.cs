#if TOOLS
using Godot;

[Tool]
public partial class Plugin : EditorPlugin
{
	const string CONTAINER_SCRIPT_PATH = "res://addons/vekDevConsole/scripts/DevConsoleContainer.cs";
	const string CONTAINER_ICON_PATH = "res://addons/vekDevConsole/consoleIcon.svg";
	const string SINGLETON_PATH = "res://addons/vekDevConsole/scripts/DevConsole.cs";
	
	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		Script devConsoleScript = GD.Load<Script>(CONTAINER_SCRIPT_PATH);
		Texture2D devConsoleIcon = GD.Load<Texture2D>(CONTAINER_ICON_PATH);

		AddCustomType("DevConsole", "Control", devConsoleScript, devConsoleIcon);
		AddAutoloadSingleton("DevConsole", SINGLETON_PATH);
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		RemoveCustomType("DevConsoleContainer");
		RemoveAutoloadSingleton("DevConsole");
	}
}
#endif
