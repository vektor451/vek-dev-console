# Vekkie's C# Developer Console
Simple C# based developer console for Godot 4.3+.

![image](https://github.com/user-attachments/assets/9ad28997-5251-4451-8f33-2446188540ae)

## Setup
1. `git clone` this repo into the `/addons` folder of your Godot project. You should have a `/addons/vekDevConsole` folder with all of the plugin's files.
2. Enable the `Dev Console` in the plugins tab of your project settings.
3. Add a `dev_console` action into your project's input map, which can also be found in the project settings (this is necessary for opening the console).
4. Add the `DevConsole` node to a desired scene in your project. It is strongly recommended to then set the anchor preset to `Full Rect`.
5. Console time B).
   
## Usage
To add a command to the console, use the DevConsole's `AddCommand(name, command)` method. This is static, and will be accessed anywhere in your project as `DevConsole.AddComand` The name property is a generic string, however the command is a class consisting of 3 notable properties:

- Action: The main method to execute with the console command. Supports arguments of type int, float, string or bool, and is required. 
- ReadAction: An alternate method executed if the command was submitted with no arguments. Usually used to retrieve the value of a property, but can be used for other means (for instance with the help command, printing all commands and their descriptions). Doesn't support arguments, and is optional.
- Description: The command's description used when getting help. Technically optional, but not advised to omit. 

You can also remove commands with `RemoveCommand(name)`

This will not work natively in GDScript, and will require you to create a C# script with methods that call the GDScript from there, and then create commands for those methods.

## Todo
- Make a more in-depth readme with more of the actual usage details of the console.
- Add further customisation options to the console (background opacity, accent color, size)
- Save and load console settings
- ~~Autocomplete and suggestion functionality~~ done!
- ~~Key blacklisting (for example when you press \` to close the console it also types a \` into it, and we don't want that)~~ Implemented differently but also done!
- Other small fixes and improvements.
