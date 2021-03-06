
cmake_minimum_required(VERSION 3.1)

project(Engine)

if ( IOS )
        set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fembed-bitcode")
        set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fembed-bitcode")
endif ()

option ( GC64 "using gc64" OFF )
option ( COMPATIBLE_FORMAT "compatible format" OFF )

find_path(XECS_PROJECT_DIR NAMES SConstruct
    PATHS 
    ${CMAKE_SOURCE_DIR}
    NO_DEFAULT_PATH
    )

MARK_AS_ADVANCED(XECS_PROJECT_DIR)

set(CMAKE_CXX_STANDARD 17)

set(SRC_PATH Engine)

set ( LUA_IDSIZE 120 CACHE STRING "gives the maximum size for the description of the source." )


include_directories(
    ${CMAKE_SOURCE_DIR}
	${SRC_PATH/Component}
    ${SRC_PATH/Entitas}
    ${SRC_PATH/System}
    ${SRC_PATH/Util}
    ${SRC_PATH}
	${CMAKE_CURRENT_BINARY_DIR}
)


file(GLOB_RECURSE CORE_ECS ${SRC_PATH}/*.cpp)


macro(source_group_by_dir proj_dir source_files)
    if(MSVC OR APPLE)
        get_filename_component(sgbd_cur_dir ${proj_dir} ABSOLUTE)
        foreach(sgbd_file ${${source_files}})
			get_filename_component(sgbd_abs_file ${sgbd_file} ABSOLUTE)
            file(RELATIVE_PATH sgbd_fpath ${sgbd_cur_dir} ${sgbd_abs_file})
            string(REGEX REPLACE "\(.*\)/.*" \\1 sgbd_group_name ${sgbd_fpath})
            string(COMPARE EQUAL ${sgbd_fpath} ${sgbd_group_name} sgbd_nogroup)
            string(REPLACE "/" "\\" sgbd_group_name ${sgbd_group_name})
            if(sgbd_nogroup)
                set(sgbd_group_name "\\")
            endif(sgbd_nogroup)
            source_group(${sgbd_group_name} FILES ${sgbd_file})
        endforeach(sgbd_file)
    endif(MSVC OR APPLE)
endmacro(source_group_by_dir)

source_group_by_dir(${CMAKE_CURRENT_SOURCE_DIR} CORE_ECS)


if (APPLE)
    if (IOS)
        set(CMAKE_OSX_ARCHITECTURES "$(ARCHS_STANDARD)")
        add_library(Engine STATIC
           ${CORE_ECS}
        )
		set_xcode_property (Engine IPHONEOS_DEPLOYMENT_TARGET "7.0" "all")
    else ()
        set(CMAKE_OSX_ARCHITECTURES "$(ARCHS_STANDARD_64_BIT)")
        add_library(Engine MODULE
            ${CORE_ECS}
        )
        set_target_properties ( Engine PROPERTIES BUNDLE TRUE )
    endif ()
elseif ("${CMAKE_SYSTEM_NAME}" STREQUAL "Switch")
    add_library(Engine STATIC
        ${CORE_ECS}
    )
    target_compile_options(Engine PRIVATE -m64 -mcpu=cortex-a57+fp+simd+crypto+crc -fno-common -fno-short-enums -ffunction-sections -fdata-sections -fPIC -fms-extensions)
else ( )
    add_library(Engine SHARED
        ${CORE_ECS}
    )
endif ( )


target_compile_definitions (Engine PRIVATE COMPATIBLE_FORMAT)

if(UINT_ESPECIALLY)
    ADD_DEFINITIONS(-DUINT_ESPECIALLY)
endif()
	
