cmake_minimum_required(VERSION 3.10)
set_cmake_folder_context(TARGET_FOLDER_NAME types)
project(CoreTypes VERSION ${OPENDAQ_CORE_VERSION} LANGUAGES CXX)

add_subdirectory(src)

if (OPENDAQ_ENABLE_TESTS)
    add_subdirectory(mocks)
    add_subdirectory(tests)
endif()

if (OPENDAQ_GENERATE_BINDINGS)
    add_subdirectory(bindings)
endif()
