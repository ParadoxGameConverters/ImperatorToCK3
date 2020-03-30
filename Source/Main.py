import ImperatortoCK3Converter
import Configuration

ConfigurationFileName = input("Insert configuration filename: ")
Configuration.readConfigurationFile(ConfigurationFileName)

ImperatorSaveFileName = input("Insert save filename: ")
ImperatortoCK3Converter.ConvertImperatorToCK3(ImperatorSaveFileName)
