cmake_minimum_required(VERSION 3.5)
#set(CMAKE_VERBOSE_MAKEFILE on)

project(ImperatorToCK3)
#SET(CMAKE_CXX_COMPILER g++-9)
set(CMAKE_BINARY_DIR ${CMAKE_SOURCE_DIR}/Source)
set(PROJECT_SOURCE_DIR ${CMAKE_SOURCE_DIR}/Source)
set(EXECUTABLE_OUTPUT_PATH ${CMAKE_SOURCE_DIR}/Release-Linux)
set(LIBRARY_OUTPUT_PATH ${CMAKE_SOURCE_DIR}/Release-Linux)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++20 -DMAGICKCORE_HDRI_ENABLE=0 -DMAGICKCORE_QUANTUM_DEPTH=8")

add_compile_options("-lstdc++fs")
add_compile_options("-O2")
add_compile_options("-pthread")
add_link_options(-no-pie)

include_directories("../commonItems")
include_directories("../commonItems/compile-time-regular-expressions/single-header")
include_directories("../ZipLib")
include_directories("/usr/local/include/ImageMagick-7/")
include_directories("../cpp-base64")
include_directories("${PROJECT_SOURCE_DIR}")
link_directories(${CMAKE_SOURCE_DIR}/Resources /usr/local/lib)
add_subdirectory(../ZipLib [binary_dir])

file(GLOB MAIN_SOURCES "${PROJECT_SOURCE_DIR}/*.cpp")
file(GLOB CONFIGURATION_SOURCES "${PROJECT_SOURCE_DIR}/Configuration/*.cpp")
file(GLOB IMPWORLD_SOURCES "${PROJECT_SOURCE_DIR}/Imperator/*.cpp")
file(GLOB IMPWORLD_SUBDIRS_SOURCES "${PROJECT_SOURCE_DIR}/Imperator/*/*.cpp")
file(GLOB MAPPERS_SOURCES "${PROJECT_SOURCE_DIR}/Mappers/*/*.cpp")
file(GLOB CK3WORLD_SOURCES "${PROJECT_SOURCE_DIR}/CK3/*.cpp")
file(GLOB CK3WORLD_SUBDIRS_SOURCES "${PROJECT_SOURCE_DIR}/CK3/*/*.cpp")
file(GLOB CK3OUTPUTTER_SOURCES "${PROJECT_SOURCE_DIR}/CK3Outputter/*.cpp")
set(BASE64_SOURCES "../cpp-base64/base64.cpp")
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
	${IMPWORLD_SUBDIRS_SOURCES}
	${MAPPERS_SOURCES}
	${CK3WORLD_SOURCES}
	${CK3WORLD_SUBDIRS_SOURCES}
	${CK3OUTPUTTER_SOURCES}
	${COMMON_SOURCES}
	${BASE64_SOURCES}
)

target_link_libraries(ImperatorToCK3 LINK_PUBLIC rakaly ZIPLIB stdc++fs
MagickWand-7.Q8
MagickCore-7.Q8
Magick++-7.Q8)

add_custom_command(TARGET ImperatorToCK3 POST_BUILD WORKING_DIRECTORY ${CMAKE_SOURCE_DIR} COMMAND chmod u+x Copy_Files.sh)
add_custom_command(TARGET ImperatorToCK3 POST_BUILD WORKING_DIRECTORY ${CMAKE_SOURCE_DIR} COMMAND ./Copy_Files.sh)