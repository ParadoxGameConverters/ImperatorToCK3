import ImperatortoCK3Converter
import Configuration

Configuration.readConfigurationFile("configuration.txt")

ImperatorSaveFileName = Configuration.getSavePath()
if ImperatorSaveFileName is not None:
    ImperatortoCK3Converter.ConvertImperatorToCK3(ImperatorSaveFileName)
