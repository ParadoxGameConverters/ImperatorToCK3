import datetime

logFileCreated = False
logFile = None
levelDict = {'error': '[ERROR]', 'warning': '[WARNING]', 'info': '[INFO]', 'debug': '[DEBUG]'}


def initiate():
    global logFile, logFileCreated
    logFile = open("log.txt", "w")
    logFileCreated = True


def endLog():
    global logFile
    logFile.close()


def WriteToConsole(message):
    print(message)


def WriteToFile(level, message):
    global logFile
    dateTime = datetime.datetime.now().strftime("%Y-%m-%d %X")
    logFile.write(dateTime + '  ' + levelDict.get(level) + '\t' + message + '\n')


def Log(level, message):
    WriteToConsole(message)
    WriteToFile(level, message)
