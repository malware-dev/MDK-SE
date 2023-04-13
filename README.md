# MDK-SE
_(Malware's Development Kit for SE)_

- - -

A toolkit to help with ingame script (programmable block) development for Keen Software House's space sandbox Space Engineers. It helps you create a ready-to-code project for writing ingame scripts, and provides an analyzer which warns you if you're trying to use something that is not allowed in Space Engineers.

### ...but it hasn't been updated for ages?
Because there hasn't been any  _need_ to. It's for all intents and purposes "done". If and when something breaks it, either a Visual Studio update or an SE update, I will do my best to fix it. Or, obviously, if I come up with a feature I want... but for now, there's nothing to do. "But there's bugs", I hear you say. Yeah, there's some minor issues. But they're small enough that I can't manage to find the time to fix them. I have limited time for this and not much help...

### Can I use this in VSCode?
No. Visual Studio Code and Visual Studio has nothing in common outside of the name.

### Visual Studio is Throwing Errors!
If you see an error in Visual Studio like:
>Unable to create project (MDK.Services.IngameScriptWizard does not exist)

Make sure you're up to date with the latest version of Visual Studio.

### Visual Studio 2022 Support...?
Microsoft made some rather major changes to how extensions work with Visual Studio, meaning that I would have to keep two separate versions around to support VS2022. I have decided that this is the perfect opportunity for me to make MDK2, since it would take time either way. I have no idea when this will be finished, however.

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
