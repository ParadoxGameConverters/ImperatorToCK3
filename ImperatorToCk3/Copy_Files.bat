echo on
rem Copy converter data files
copy "Data_Files\configuration.txt" "..\Release\ImperatorToCK3\configuration.txt"
rem copy "Data_Files\ReadMe.txt" "..\Release\ImperatorToCK3\readme.txt"
rem copy "Data_Files\ReadMe.txt" "..\Release\readme.txt"
rem copy "Data_Files\FAQ.txt" "..\Release\ImperatorToCK3\FAQ.txt"
copy "Data_Files\ImperatorToCK3DefaultConfiguration.xml" "..\Release\Configuration\ImperatorToCK3DefaultConfiguration.xml"
copy "Data_Files\SupportedConvertersDefault.xml" "..\Release\Configuration\SupportedConvertersDefault.xml"

rem Create Configurables
del "..\Release\ImperatorToCK3\configurables" /Q
rmdir "..\Release\ImperatorToCK3\configurables" /S /Q
xcopy "Data_Files\configurables" "..\Release\ImperatorToCK3\configurables" /Y /E /I