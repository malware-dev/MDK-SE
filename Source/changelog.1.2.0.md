
- #111 thumb.jpg in existing scripts not recognized in blueprint manager
- #124 [PRE] [VS2019] Error when attempting to create a new Project 
- #125 [PRE] [VS2019] New projects default to C# 7 or higher. 
- #130 Suggestion: Refreshing whitelist runs SE; Add -skipintro to not show intro video 
    
- Changed how script options are stored in order to make it easier to work on projects on multiple computers
- Renamed Blueprint Manager to Script Manager
- Deprecated Readme.cs (more or less), changed to Instructions.readme and added item template for it
- Fixed crash while VS is trying to read a project from a network (MDK will ignore this case)
- Added project template for ingame script mixin (shared project)
- MDK is now more code repository (for example git) friendly. 
    - The paths are now stored in a separate file, MDK.paths.props, which should not be checked in.  
      The file should be restored on the other side.
- Backup zips are made before any project upgrades or repairs are made
- Finalizers are reported as prohibited
- Experimental VS2019 support
- Proper install-time version checking, finally
- Fixed #98 Unimportant Non-SE Project Error
- Added #99 Include "Do not deploy" option in MDK Script Options window / #79 Deploy option in files.
  "Exclude from Deploy All" checkbox    