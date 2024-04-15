# MDK-SE
_(Malware's Development Kit for SE)_

- - -

## Visual Studio 2022 17.9.0 Has Broken MDK
Microsoft has broken the MDK Deploy feature in their latest version of Visual Studio in a way I don't know if I can fix (they have flat out deleted something). I am working on figuring out what to do.

In the mean time, you can easily roll back to the previous Visual Studio version by starting your Visual Studio Installer, clicking the More button, there should be a Roll Back option there.

Hopefully this will tide you over until I can either fix the problem... or finish MDK2. One or the other.

Please vote:
https://developercommunity.visualstudio.com/t/Something-got-deleted-when-VS-updated-b/10598067

There is now a nuget package _early alpha_ of the MDK2 packager.

Mal.Mdk2.PbPackager

This should be deploying your scripts every time you build, MDK1's Deploy function will no longer be necessary... just compile your script.

Of course, being an early alpha, it's likely riddled with problems.

Please make issues prefixed with [MDK2] - I will take both suggestions and bug reports at this time.

I am dependent on whoever may be willing to help.

Some limited instructions are available at https://www.nuget.org/packages/Mal.Mdk2.PbPackager/

### Installing MDK2 in your MDK1 project

* Right click on your MDK1 project
* Select `Manage Nuget Packages`
* Find the search bar, search for `Mal.`
* Install the `Mal.MDK2.PbPackager` package into your project
* Rebuild!

### Creating a brand new pure MDK2 project

Obviously there will eventually be a template to do this for you, but it _is_ actually possible to create a pure MDK2 project already:

1. Set up a new-style C# project (create a .NET 8 class library project, for example).
3. Open the project file:  
    change the TargetFramework from `net8.0` to `netframework48`  
    remove the `ImplicitUsings` and `Nullable` tags.  
    Add a tag `<RootNamespace>IngameScript</RootNamespace>`
    Add a tag `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`
5. Delete the pregenerated Class1.cs file
6. Add the nuget packages:  
    `Mal.Mdk2.PbAnalyzers`
    `Mal.Mdk2.References`
    `Mal.Mdk2.PbPackager`
5. Add a `Program` class deriving from `MyGridProgram`

This is highly likely to be what the MDK2 project will finally look like when I'm done.

- - -

A toolkit to help with ingame script (programmable block) development for Keen Software House's space sandbox Space Engineers. It helps you create a ready-to-code project for writing ingame scripts, and provides an analyzer which warns you if you're trying to use something that is not allowed in Space Engineers.

### ...but it hasn't been updated for ages?
Because there hasn't been any  _need_ to. It's for all intents and purposes "done". If and when something breaks it, either a Visual Studio update or an SE update, I will do my best to fix it. Or, obviously, if I come up with a feature I want... but for now, there's nothing to do. "But there's bugs", I hear you say. Yeah, there's some minor issues. But they're small enough that I can't manage to find the time to fix them. I have limited time for this and not much help...

### Can I use this in VSCode?
No. Visual Studio Code and Visual Studio has nothing in common outside of the name.

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
