import ImperatortoCK3Converter
import Configuration

ConfigurationFileName = input("Insert configuration filename: ")
Configuration.readConfigurationFile(ConfigurationFileName)

ImperatorSaveFileName = Configuration.getSavePath()
if ImperatorSaveFileName is not None:
    ImperatortoCK3Converter.ConvertImperatorToCK3(ImperatorSaveFileName)
