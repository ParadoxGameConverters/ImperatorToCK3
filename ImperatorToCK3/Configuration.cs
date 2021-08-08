using System.IO;
using System;
using commonItems;

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
            Logger.Log(LogLevel.Info, "Reading configuration file");
            RegisterKeys();
            ParseFile("configuration.txt");
            ClearRegisteredRules();
            SetOutputName();
            VerifyImperatorPath();
            VerifyCK3Path();
        }

        void RegisterKeys()
        {
            RegisterKeyword("SaveGame", (sr) =>
            {
                SaveGamePath = new SingleString(sr).String;
                Logger.Log(LogLevel.Info, "Save game set to: " + SaveGamePath);
            });
            RegisterKeyword("ImperatorDirectory", (sr) =>
            {
                ImperatorPath = new SingleString(sr).String;
            });
            RegisterKeyword("ImperatorModsDirectory", (sr) =>
            {
                ImperatorModsPath = new SingleString(sr).String;
            });
            RegisterKeyword("CK3directory", (sr) =>
            {
                Ck3Path = new SingleString(sr).String;
            });
            RegisterKeyword("CK3ModsDirectory", (sr) =>
            {
                Ck3ModsPath = new SingleString(sr).String;
            });
            RegisterKeyword("output_name", (sr) =>
            {
                OutputModName = new SingleString(sr).String;
                Logger.Log(LogLevel.Info, "Output name set to: " + OutputModName);
            });
            RegisterKeyword("ImperatorDeJure", (sr) =>
            {
                var deJureString = new SingleString(sr).String;
                try
                {
                    ImperatorDeJure = (IMPERATOR_DE_JURE)Convert.ToInt32(deJureString);
                    Logger.Log(LogLevel.Info, "ImperatorDeJure set to: " + deJureString);
                } catch(Exception e)
                {
                    Logger.Log(LogLevel.Error, "Undefined error, ImperatorDeJure value was: " + deJureString + "; Error message: " + e.ToString());
                }
            });
            RegisterKeyword("ConvertCharacterBirthAndDeathDates", (sr) =>
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
                Logger.Log(LogLevel.Info, "Conversion of characters' birth and death dates set to: " + ConvertBirthAndDeathDates);
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
            Logger.Log(LogLevel.Info, "\tI:R install path is " + ImperatorPath);
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
            Logger.Log(LogLevel.Info, "\tCK3 install path is " + Ck3Path);
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
            Logger.Log(LogLevel.Info, "Using output name " + OutputModName);
        }
    }
}
