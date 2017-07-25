# MDK-SE
(Malware's Development Kit for SE)

A toolkit to help with ingame script (programmable block) development for Keen Software House's space sandbox Space Engineers. It helps you create a ready-to-code project for writing ingame scripts, and provides  an analyzer which warns you if you're trying to use something that is not allowed in Space Engineers.

Space Engineers is trademarked to Keen Software House. This toolkit is fan-made, and its developer has no relation to Keen Software House.

## Important Note
This is a project I pretty much made for _myself_. I'm publishing it in case someone else might have a use for it. Fair warning: Make requests, by all means, but if your request is not something I myself have any use for, someone else is gonna have to do the work. I'm fully employed, and this is a spare-time project. I'll be working on it on and off.

## Contribution
Yeah, sure. Absolutelyl. I will gladly accept contribution to the project. I'll be grateful for any assistance, especially for any features requested that I myself may not have any use for. But I won't be merging contributions willy nilly. I will expect a certain minimum standard, and I reserve the right to deny features I don't like. Another fair warning :D
 
## Getting Started

### Step 1
First of all, obviously, you need to make sure you have installed Visual Studio 2017. You can find instructions on how to do that here:
https://www.visualstudio.com/vs/getting-started/new-install/

### Step 2
Download the extension from here:
https://github.com/malware-dev/MDK-SE/releases

You should stay away from any pre-release builds unless you know what you're doing, or if you want to test.

### Step 3
After installing the extension, you can now start Visual Studio and create your script project. You do this by finding the **File** menu, then **New** and **Project...**

Now make sure you select the correct .NET Framework version.

![Select .NET Framework 4.6.1](images\wiki-newproject-framework.jpg)

After this you can select the Space Engineers category on the left. You will now be able to see the ingame script template.

![Select the Ingame Script template](images\wiki-newproject-template.jpg)



Now you can select your project's location and names in the boxes below.

![Enter your project information](images\wiki-newproject-properties.jpg)

Press** OK** to create your project.

You may now write your script directly in this class if you wish. If your scripts are not too large, this is quite fine. However this extension has another couple of tricks up its sleeve for the slightly more advanced users:



### Deploying your script
To deploy your script to Space Engineers, press the **Tools** menu and then **MDK** and **Deploy Script(s)**

![Deploy Scripts](images\wiki-deploy.jpg)

This will deploy all script projects in the active solution.

### Advanced 1: Utility Classes

Right-click on your project and select **Add** and **New Item...**
Once more select the **Space Engineers** category on the left hand side
Select the **Utility Class** template, name it and press **OK**

You have now added a new utility class to your project. This class will be merged into your script when deploying, so you don't have to keep all your types in one single code file.

Note that your new class resides within a partial Program class, just like your main script. This is what makes it a utility class, and any code here has the same access as the code in your Program code file.

### Advanced 2: Extension Classes

[See here for an explanation on what an extension class is](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods).

Right-click on your project and select **Add** and **New Item...**
Once more select the **Space Engineers** category on the left hand side
Select the **Extension Class** template, name it and press **OK**

Extension classes is another advanced concept. It's officially allowed in programmable blocks, but it's a bit tricky to get them to work. The MDK utility helps you by doing that trick for you, you just need to worry about the code.

Note though that code in extension classes reside _outside_ of the Program class. This means that it does _not_ immediately have the same access as the code in your Program code file. Any required information must be passed in.

### Advanced 3: Mixin Projects
You can use Shared Projects as mixins.

_tutorial pending_