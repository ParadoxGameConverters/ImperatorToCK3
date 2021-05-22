using commonItems;
using System.IO;
using System;

namespace ImperatorToCK3
{
    enum IMPERATOR_DE_JURE { REGIONS = 1, COUNTRIES = 2, NO = 3 };
    class Configuration : Parser
    {
        public string SaveGamePath { get; internal set; } = "";
        public string ImperatorPath { get; internal set; } = "";
        public string ImperatorModsPath { get; internal set; } = "";
        public string Ck3Path { get; internal set; } = "";
        public string Ck3ModsPath { get; internal set; } = "";
        public string OutputModName { get; internal set; } = "";
        public IMPERATOR_DE_JURE ImperatorDeJure { get; internal set; } = IMPERATOR_DE_JURE.NO;
        public bool ConvertBirthAndDeathDates { get; internal set; } = true;

        public Configuration()
        {
            Log.WriteLine(LogLevel.Info, "Reading configuration file");
            RegisterKeys();
            ParseFile("configuration.txt");
            ClearRegisteredDict();
            SetOutputName();
            VerifyImperatorPath();
            VerifyCK3Path();
        }

        void RegisterKeys()
        {
            RegisterKeyword("SaveGame", (StreamReader sr) =>
            {
                SaveGamePath = new SingleString(sr).String;
                Log.WriteLine(LogLevel.Info, "Save game set to: " + SaveGamePath);
            });
            RegisterKeyword("ImperatorDirectory", (StreamReader sr) =>
            {
                ImperatorPath = new SingleString(sr).String;
            });
            RegisterKeyword("ImperatorModsDirectory", (StreamReader sr) =>
            {
                ImperatorModsPath = new SingleString(sr).String;
            });
            RegisterKeyword("CK3directory", (StreamReader sr) =>
            {
                Ck3Path = new SingleString(sr).String;
            });
            RegisterKeyword("CK3ModsDirectory", (StreamReader sr) =>
            {
                Ck3ModsPath = new SingleString(sr).String;
            });
            RegisterKeyword("output_name", (StreamReader sr) =>
            {
                OutputModName = new SingleString(sr).String;
                Log.WriteLine(LogLevel.Info, "Output name set to: " + OutputModName);
            });
            RegisterKeyword("ImperatorDeJure", (StreamReader sr) =>
            {
                var deJureString = new SingleString(sr).String;
                try
                {
                    ImperatorDeJure = (IMPERATOR_DE_JURE)Convert.ToInt32(deJureString);
                    Log.WriteLine(LogLevel.Info, "ImperatorDeJure set to: " + deJureString);
                } catch(Exception e)
                {
                    Log.WriteLine(LogLevel.Error, "Undefined error, ImperatorDeJure value was: " + deJureString + "; Error message: " + e.ToString());
                }
            });
            RegisterKeyword("ConvertCharacterBirthAndDeathDates", (StreamReader sr) =>
            {
                var valStr = new SingleString(sr).String;
                if (valStr == "true")
                {
                    ConvertBirthAndDeathDates = true;
                }
                else if (valStr == "false")
                {
                    ConvertBirthAndDeathDates = false;
                }
                Log.WriteLine(LogLevel.Info, "Conversion of characters' birth and death dates set to: " + ConvertBirthAndDeathDates);
            });
            RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
        }

        void VerifyImperatorPath()
        {
            if (!Directory.Exists(ImperatorPath))
            {
                throw new DirectoryNotFoundException(ImperatorPath + " does not exist!");
            }
            if (!File.Exists(ImperatorPath + "/binaries/imperator.exe"))
            {
                throw new FileNotFoundException(ImperatorPath + "does not contains Imperator: Rome!");
            }
            Log.WriteLine(LogLevel.Info, "\tI:R install path is " + ImperatorPath);
        }

        void VerifyCK3Path()
        {
            if (!Directory.Exists(Ck3Path))
            {
                throw new DirectoryNotFoundException(Ck3Path + " does not exist!");
            }
            if (!File.Exists(Ck3Path + "/binaries/ck3.exe"))
            {
                throw new FileNotFoundException(Ck3Path + " does not contain Crusader Kings III!");
            }
            Log.WriteLine(LogLevel.Info, "\tCK3 install path is " + Ck3Path);
        }

        void SetOutputName()
        {
            if (OutputModName.Length == 0)
            {
                OutputModName = CommonFunctions.TrimPath(SaveGamePath);
            }
            OutputModName = CommonFunctions.TrimExtenstion(OutputModName);
            OutputModName = OutputModName.Replace('-', '_');
            OutputModName = OutputModName.Replace(' ', '_');

            OutputModName = CommonFunctions.NormalizeUTF8Path(OutputModName);
            Log.WriteLine(LogLevel.Info, "Using output name " + OutputModName);
        }
    }
}
