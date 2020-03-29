import re  # regular expressions

registeredKeywordStrings = {}  # dict
registeredKeywordRegexes = []  # list
generatedRegexes = []  # list


def peek_char(file):
    pos = file.tell()
    char = file.read(1)
    file.seek(pos)
    return char


def registerKeyword(keyword, function):
    registeredKeywordStrings.update({keyword: function})
    # print(registeredKeywordStrings.items())  # for debug


def registerRegex(keyword, function):
    pair = (keyword, function)
    registeredKeywordRegexes.append(pair)


def clearRegisteredKeywords():
    registeredKeywordStrings.clear()
    registeredKeywordRegexes.clear()


def getNextLexeme(theStream):
    toReturn = ''
    inQuotes = False
    while peek_char(theStream):
        inputChar = peek_char(theStream)
        if not inQuotes and inputChar == '#':
            inputChar = theStream.read(1)
            bitBucket = theStream.readline()
            if toReturn != '':
                break
        elif inputChar == '\n':
            inputChar = theStream.read(1)
            if not inQuotes:
                if toReturn != '':
                    break
            else:
                toReturn += ' '  # fix Paradox' mistake and don't break proper names in half
        elif inputChar == '"' and not inQuotes and toReturn == '':
            inputChar = theStream.read(1)
            inQuotes = True
            toReturn += inputChar
        elif inputChar == '"' and inQuotes:
            inputChar = theStream.read(1)
            toReturn += inputChar
            break
        elif not inQuotes and inputChar.isspace():
            inputChar = theStream.read(1)
            if toReturn != '':
                break
        elif not inQuotes and inputChar == '{':
            if toReturn == '':
                inputChar = theStream.read(1)
                toReturn += inputChar
            break
        elif not inQuotes and inputChar == '}':
            if toReturn == '':
                inputChar = theStream.read(1)
                toReturn += inputChar
            break
        elif not inQuotes and inputChar == '=':
            if toReturn == '':
                inputChar = theStream.read(1)
                toReturn += inputChar
            break
        else:
            inputChar = theStream.read(1)
            toReturn += inputChar
    return toReturn


def getNextToken(theStream):
    toReturn = ''
    gotToken = False
    while not gotToken:
        if not peek_char(theStream):  # I use this instead of eof
            return None
        toReturn = getNextLexeme(theStream)
        matched = False
        pair = (toReturn, registeredKeywordStrings.get(toReturn))
        if pair[1] is not None:
            pair[1](toReturn, theStream)
            matched = True

        if not matched:
            for registration in generatedRegexes:
                if re.match(registration[0], toReturn):
                    registration[1](toReturn, theStream)
                    matched = True
                    break

        if not matched:
            gotToken = True
    if not toReturn == '':
        return toReturn
    return None


def getNextTokenWithoutMatching(theStream):
    toReturn = ''
    gotToken = False
    while not gotToken:
        if not peek_char(theStream):  # I use this instead of eof
            return None
        toReturn = getNextLexeme(theStream)
        gotToken = True
    if not toReturn == '':
        return toReturn
    return None


def parseStream(theStream):
    braceDepth = 0
    for keywordItr in registeredKeywordRegexes:
        pair = (keywordItr[0], keywordItr[1])  # keywordItr[0] is regex, keywordItr[1] is parsingfunction
        generatedRegexes.append(pair)

    while True:
        token = getNextToken(theStream)
        if token:
            if token == '=':
                continue
            if token == '{':
                braceDepth += 1
            elif token == '}':
                braceDepth -= 1
                if braceDepth == 0:
                    break
            else:
                print("Unknown token while parsing stream: " + token)
        else:
            break
    generatedRegexes.clear()


def parseFile(filename):
    try:
        theFile = open(filename, 'r')
    except FileNotFoundError:
        print("Could not open " + filename + " for parsing: File not found!")
        return
    except:
        print("Could not open " + filename + " for parsing.")
        return

    firstChar = peek_char(theFile)
    if firstChar == '\xEF':
        bitBucket = theFile.read(3)

    parseStream(theFile)
    theFile.close()
