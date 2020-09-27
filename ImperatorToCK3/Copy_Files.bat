echo on
rem Copy converter data files
rem copy "Data_Files\ReadMe.txt" "..\Release\ImperatorToCK3\readme.txt"
rem copy "Data_Files\ReadMe.txt" "..\Release\readme.txt"
rem copy "Data_Files\FAQ.txt" "..\Release\ImperatorToCK3\FAQ.txt"

rem Copy DLLs
xcopy "..\ImageMagick\dll" "..\Release\ImperatorToCK3" /Y /E /I

mkdir "..\Release\Configuration"
copy "Data_Files\fronter-configuration.txt" "..\Release\Configuration\fronter-configuration.txt"
copy "Data_Files\fronter-options.txt" "..\Release\Configuration\fronter-options.txt"
copy "Data_Files\*.yml" "..\Release\Configuration\"

rem Create Configurables
del "..\Release\ImperatorToCK3\configurables" /Q
rmdir "..\Release\ImperatorToCK3\configurables" /S /Q
xcopy "Data_Files\configurables" "..\Release\ImperatorToCK3\configurables" /Y /E /I