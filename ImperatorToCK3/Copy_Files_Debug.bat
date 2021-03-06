echo on

rem Create Blank Mod
del "..\Debug\ImperatorToCK3\blankMod" /Q
rmdir "..\Debug\ImperatorToCK3\blankMod" /S /Q
xcopy "Data_Files\blankMod" "..\Debug\ImperatorToCK3\blankMod" /Y /E /I

git rev-parse HEAD > ..\Release\commit_id.txt