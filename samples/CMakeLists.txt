cmake_minimum_required(VERSION 3.16)
project(cvedix_sdk_samples CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ── SDK Location ──
if(NOT DEFINED CVEDIX_SDK_DIR)
    get_filename_component(CVEDIX_SDK_DIR "${CMAKE_CURRENT_LIST_DIR}/.." ABSOLUTE)
endif()

message(STATUS "CVEDIX SDK: ${CVEDIX_SDK_DIR}")

# ── Find SDK ──
list(APPEND CMAKE_PREFIX_PATH "${CVEDIX_SDK_DIR}/cmake")
find_package(cvedix REQUIRED)

# ── Find OpenCV ──
find_package(OpenCV REQUIRED)

# ── Common include/link ──
include_directories(
    ${CVEDIX_INCLUDE_DIRS}
    ${OpenCV_INCLUDE_DIRS}
)

link_directories(${CVEDIX_LIB_DIR})

set(COMMON_LIBS
    ${CVEDIX_LIBRARIES}
    ${OpenCV_LIBS}
    pthread
)

# ── Output directory ──
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

# ── Samples ──
add_executable(01_basic_pipeline 01_basic_pipeline/main.cpp)
target_link_libraries(01_basic_pipeline ${COMMON_LIBS})

add_executable(02_face_detection_blur 02_face_detection_blur/main.cpp)
target_link_libraries(02_face_detection_blur ${COMMON_LIBS})

add_executable(03_multi_branch 03_multi_branch/main.cpp)
target_link_libraries(03_multi_branch ${COMMON_LIBS})

add_executable(04_hera_face_lite 04_hera_face_lite/main.cpp)
target_link_libraries(04_hera_face_lite ${COMMON_LIBS})
