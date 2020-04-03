import ImperatortoCK3Converter
import Configuration
import Log

Configuration.readConfigurationFile("configuration.txt")
Log.initiate()

ImperatorSaveFileName = Configuration.getSavePath()
if ImperatorSaveFileName is not None:
    ImperatortoCK3Converter.ConvertImperatorToCK3(ImperatorSaveFileName)
