cmake_minimum_required(VERSION 3.10)
set_cmake_folder_context(TARGET_FOLDER_NAME shared)
project(OpcUaShared CXX C)

add_subdirectory(src)

if (OPENDAQ_ENABLE_TESTS)
    add_subdirectory(tests)
endif()
