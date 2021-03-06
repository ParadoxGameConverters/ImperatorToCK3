echo on

rem Create Blank Mod
del "..\Release\ImperatorToCK3\blankMod" /Q
rmdir "..\Release\ImperatorToCK3\blankMod" /S /Q
xcopy "Data_Files\blankMod" "..\Release\ImperatorToCK3\blankMod" /Y /E /I

git rev-parse HEAD > ..\Release\commit_id.txt