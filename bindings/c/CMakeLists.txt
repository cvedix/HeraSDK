cmake_minimum_required(VERSION 3.10)
project(cvedix_c_binding_sample C)

# SDK root (chứa include/ và lib/<arch>/)
if(NOT DEFINED CVEDIX_SDK_DIR)
    get_filename_component(CVEDIX_SDK_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../.." ABSOLUTE)
endif()

execute_process(COMMAND uname -m OUTPUT_VARIABLE ARCH OUTPUT_STRIP_TRAILING_WHITESPACE)

add_executable(c_pipeline_sample sample/main.c)
target_include_directories(c_pipeline_sample PRIVATE "${CVEDIX_SDK_DIR}/include/cvedix/capi")
target_link_directories(c_pipeline_sample PRIVATE "${CVEDIX_SDK_DIR}/lib/${ARCH}")
target_link_libraries(c_pipeline_sample PRIVATE cvedix_capi)
