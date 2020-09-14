cmake_minimum_required(VERSION 3.5)
#set(CMAKE_VERBOSE_MAKEFILE on)
set(THREADS_PREFER_PTHREAD_FLAG ON)
find_package(Threads REQUIRED)
add_compile_options("-lstdc++fs")
add_compile_options("-pthread")
add_compile_options("-O2")

project(MAGICK++)
SET(CMAKE_CXX_COMPILER g++-9)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++17")

file(GLOB HEADER_FILES *.h)

add_library(${PROJECT_NAME} STATIC 
${HEADER_FILES}

target_link_libraries(${PROJECT_NAME} Threads::Threads)
target_include_directories (${PROJECT_NAME} PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})