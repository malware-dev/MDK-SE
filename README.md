# MDK-SE
_(Malware's Development Kit for SE)_

-- NOTE: The [MDK² repository has now been made public](https://github.com/malforge/mdk2). 

This repository is now defunct.

- - -

## Visual Studio 2022 17.9.0 Has Broken MDK
Microsoft has broken the MDK Deploy feature in their latest version of Visual Studio in a way I don't know if I can fix (they have flat out deleted something). 

Please vote:
https://developercommunity.visualstudio.com/t/Something-got-deleted-when-VS-updated-b/10598067

The MDK2 packager nuget package is now complete. While I understand that you might want to stick with MDK1 for now, I do recommend trying to transition
over to MDK2 (see below for how to create a pure MDK2 project).

`Mal.Mdk2.PbPackager`

This should be deploying your scripts every time you build, MDK1's Deploy function will no longer be necessary... just compile your script.

Of course, being an early alpha, it's likely riddled with problems.

Please make issues prefixed with [MDK2] - I will take both suggestions and bug reports at this time.

I am dependent on whoever may be willing to help.

Some limited instructions are available at https://www.nuget.org/packages/Mal.Mdk2.PbPackager/

### Installing MDK2 in your MDK1 project

* Right click on your MDK1 project
* Select `Manage Nuget Packages`
* Find the search bar. To the right of the search bar, there is a check box named "Include prerelease". Check it, then search for `Mal.`
* Install the `Mal.MDK2.PbPackager` package into your project
* Rebuild!

### Creating a brand new pure MDK2 project

Soon there will be an installer for this, but you can already install the template manually:

1. Open a terminal or console
    _press the Windows `Start` button, type `cmd`, press enter (starts `Command Prompt`_
2. In the console, type `dotnet new install Mal.Mdk2.ScriptTemplates`, press enter
3. You should now find the templates in Visual Studio, and can create fully modern MDK2 projects.
   3a. Jetbrains Rider should also have this template available now, you no longer need to use Visual Studio!
   3b. VSCode too! I'm not too familiar with how VSCode does things, but I believe you'll have to use the `dotnet new` command directly
       with the new templates to create new projects.  
       - `dotnet new mdk2pbscript` creates a new script project in the current folder.  
       - `dotnet new mdk2pbmixin` creates a new mixin project in the current folder.

- - -

A toolkit to help with ingame script (programmable block) development for Keen Software House's space sandbox Space Engineers. It helps you create a ready-to-code project for writing ingame scripts, and provides an analyzer which warns you if you're trying to use something that is not allowed in Space Engineers.

### ...but it hasn't been updated for ages?
Because there hasn't been any  _need_ to. It's for all intents and purposes "done". If and when something breaks it, either a Visual Studio update or an SE update, I will do my best to fix it. Or, obviously, if I come up with a feature I want... but for now, there's nothing to do. "But there's bugs", I hear you say. Yeah, there's some minor issues. But they're small enough that I can't manage to find the time to fix them. I have limited time for this and not much help...

### Can I use this in VSCode?
Not MDK1. Visual Studio Code and Visual Studio has nothing in common outside of the name.
However, MDK2, as described above, _can_ be used in VSCode!

### Visual Studio is Throwing Errors!
If you see an error in Visual Studio like:
>Unable to create project (MDK.Services.IngameScriptWizard does not exist)

Make sure you're up to date with the latest version of Visual Studio 2022.

### General features
* Helps you create a fully connected script project in Visual Studio, with all references in place
* Requires that you have the game installed, but does _not_ require you to have it running
* Class templates for normal utility classes and extension classes
* Tells you if you're using code that's not allowed in Space Engineers (whitelist checker)
* Deploys multiple classes into a single PB script, which then is placed in the local Workshop space for easy access in-game - no copy/paste needed
* Supports optional code minifying: Fit more code within the limits of the programmable block
* Allows real reusable code libraries through the use of Visual Studio's Shared Project
* Out-of-game script blueprint manager allows you to rename and delete script blueprints without starting the game

### Quick links
* [MDK/SE Wiki page](https://github.com/malware-dev/MDK-SE/wiki)  
* [Getting Started with MDK](https://github.com/malware-dev/MDK-SE/wiki/Getting-Started-with-MDK)
* [Quick Introduction to Space Engineers Ingame Scripts](https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts)  
  (You don't have to use the extension to get something out of this guide)
* [Contributing to MDK](https://github.com/malware-dev/MDK-SE/blob/master/CONTRIBUTING.md)

- - -

Space Engineers is trademarked to Keen Software House. This toolkit is fan-made, and its developer has no relation to Keen Software House.
