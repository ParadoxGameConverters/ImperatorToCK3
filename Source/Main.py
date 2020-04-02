import ImperatortoCK3Converter
import Configuration

ConfigurationFileName = input("Insert configuration filename: ")
Configuration.readConfigurationFile(ConfigurationFileName)

ImperatorSaveFileName = Configuration.getSavePath()
ImperatortoCK3Converter.ConvertImperatorToCK3(ImperatorSaveFileName)
