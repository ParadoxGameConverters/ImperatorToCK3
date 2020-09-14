cmake_minimum_required(VERSION 3.5)
#set(CMAKE_VERBOSE_MAKEFILE on)

project(ImperatorToCK3)
#SET(CMAKE_CXX_COMPILER g++-9)
set(CMAKE_BINARY_DIR ${CMAKE_SOURCE_DIR}/Source)
set(PROJECT_SOURCE_DIR ${CMAKE_SOURCE_DIR}/Source)
set(EXECUTABLE_OUTPUT_PATH ${CMAKE_SOURCE_DIR}/Release-Linux)
set(LIBRARY_OUTPUT_PATH ${CMAKE_SOURCE_DIR}/Release-Linux)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++17")

add_compile_options("-lstdc++fs")
add_compile_options("-O2")
add_compile_options("-pthread")
add_link_options(-no-pie)

include_directories("../commonItems")
include_directories("../ZipLib")
include_directories("../cpp-base64")
include_directories("../ImageMagick/include")
include_directories("${PROJECT_SOURCE_DIR}")

link_directories("../ImageMagick/lib")

add_subdirectory(../ZipLib [binary_dir])

file(GLOB MAIN_SOURCES "${PROJECT_SOURCE_DIR}/*.cpp")
file(GLOB CONFIGURATION_SOURCES "${PROJECT_SOURCE_DIR}/Configuration/*.cpp")
file(GLOB IMPWORLD_SOURCES "${PROJECT_SOURCE_DIR}/Imperator/*.cpp")
file(GLOB IMPWORLD_CHARACTERS_SOURCES "${PROJECT_SOURCE_DIR}/Imperator/Characters/*.cpp")
file(GLOB IMPWORLD_FAMILIES_SOURCES "${PROJECT_SOURCE_DIR}/Imperator/Families/*.cpp")
file(GLOB IMPWORLD_PROVINCES_SOURCES "${PROJECT_SOURCE_DIR}/Imperator/Provinces/*.cpp")
file(GLOB MAPPERS_VERSIONPARSER_SOURCES "${PROJECT_SOURCE_DIR}/Mappers/VersionParser/*.cpp")
file(GLOB CK3WORLD_SOURCES "${PROJECT_SOURCE_DIR}/CK3/*.cpp")
file(GLOB CK3OUTPUTTER_SOURCES "${PROJECT_SOURCE_DIR}/CK3Outputter/*.cpp")
set(COMMON_SOURCES "../commonItems/CommonFunctions.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Color.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/GameVersion.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Date.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/LinuxUtils.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Log.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/OSCommonLayer.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Parser.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/ParserHelpers.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/StringUtils.cpp")

add_executable(ImperatorToCK3 
	${MAIN_SOURCES}
	${CONFIGURATION_SOURCES}
	${IMPWORLD_SOURCES}
	${IMPWORLD_CHARACTERS_SOURCES}
	${IMPWORLD_FAMILIES_SOURCES}
	${IMPWORLD_PROVINCES_SOURCES}
	${MAPPERS_VERSIONPARSER_SOURCES}
	${CK3WORLD_SOURCES}
	${CK3OUTPUTTER_SOURCES}
	${COMMON_SOURCES}
)

target_link_libraries(ImperatorToCK3 LINK_PUBLIC ZIPLIB stdc++fs)
target_link_libraries(ImperatorToCK3 LINK_PUBLIC CORE_RL_Magick++_.lib)
target_link_libraries(ImperatorToCK3 LINK_PUBLIC CORE_RL_MagickCore_.lib)
target_link_libraries(ImperatorToCK3 LINK_PUBLIC CORE_RL_MagickWand_.lib)

add_custom_command(TARGET ImperatorToCK3 POST_BUILD WORKING_DIRECTORY ${CMAKE_SOURCE_DIR} COMMAND chmod u+x Copy_Files.sh)
add_custom_command(TARGET ImperatorToCK3 POST_BUILD WORKING_DIRECTORY ${CMAKE_SOURCE_DIR} COMMAND ./Copy_Files.sh)
