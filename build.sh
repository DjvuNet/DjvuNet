#!/usr/bin/env bash

# Prevent WSL from exposing Windows executables (like Strawberry Perl pkg-config) to the Linux build environment
if [ -n "$WSL_DISTRO_NAME" ] || [ -n "$WSL_INTEROP" ]; then
    export PATH=$(echo "$PATH" | tr ':' '\n' | grep -v -iE '^/mnt/[a-z]($|/)' | paste -sd ':' -)
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Ensure Ctrl+C kills the script and all its children immediately
trap 'echo "BUILD: Interrupted by user. Exiting..."; exit 130' INT

# Uncommment set -x to trace execution of the script
# set -x

usage()
{
    echo ""
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo ""
    echo "  -c, -Configuration <config>    Build configuration (Debug, Release, Checked). Default: Debug."
    echo ""
    echo "  -p, -Platform <platform>       Build platform (x64, x86, arm, armel, arm64, anycpu). Default: x64."
    echo ""
    echo "  -t, -Target <target>           MSBuild target (Build, Rebuild, Clean, Pack). Default: Build."
    echo ""
    echo "  -f, -Framework <tfm>           Target framework (net10.0, netstandard2.1, net472). Default: net10.0."
    echo ""
    echo "  -DjvuNet, -BuildDjvuNet        Build the core DjvuNet managed projects. Default: True."
    echo ""
    echo "  -ts, -Tools                    Build the custom DjvuNet build tasks and"
    echo "                                 package them into cross-platform archives."
    echo "                                 Default: False."
    echo ""
    echo "  -bt, -BuildTests               Build the test projects. Default: False."
    echo ""
    echo "  -rt, -RunTests                 Build and run the test projects. Default: False."
    echo ""
    echo "  -Test                          Alias for -RunTests. Build and run the test projects."
    echo ""
    echo "  -sn, -SkipNative               Skip cloning, building, and testing of native components"
    echo "                                 (libdjvulibre) and its managed wrapper (DjvuNet.DjvuLibre)."
    echo "                                 When omitted, native dependencies are processed (SkipNative=False)."
    echo ""
    echo "  -v, -Verbosity <level>         Verbosity (q[uiet], m[inimal], n[ormal], d[etailed], diag[nostic]). Default: normal."
    echo ""
    echo "  -proc, -Processors <count>     Number of build processes. Default: number of logical processors."
    echo ""
    echo "  -OS <os>                       Target OS (Windows, Linux, OSX). Default: Linux."
    echo ""
    echo "  -h, --help                     Show this usage message."
    echo ""
}

run_custom_command() {
    local cmd_name=$1
    shift
    local cmd_array=("$@")

    echo "BUILD: Running: $cmd_name"
    echo "BUILD: Calling: ${cmd_array[*]}"
    "${cmd_array[@]}"
    local exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "BUILD: Error: $cmd_name returned error code $exit_code"
        __FailedCommands+=("$cmd_name")
        if [[ -n "$_FastFail" ]]; then exit 1; fi
        return 1
    else
        __SuccessfulCommands+=("$cmd_name")
        return 0
    fi
}

git_clone_retry()
{
    local url=$1
    local dest=$2
    shift 2
    local extra_args=("$@")

    local max_attempts=3
    local timeout_sec=600
    local attempt=1
    local delay=5

    while [ $attempt -le $max_attempts ]; do
        local actual_dest="$dest"
        if [ -z "$actual_dest" ]; then
            actual_dest=$(basename "$url" .git)
        fi

        if [ -n "$actual_dest" ] && [ "$actual_dest" != "." ] && [ -d "$actual_dest" ]; then
            echo "BUILD: Cleaning up destination directory $actual_dest before cloning..."
            rm -rf "$actual_dest"
        fi

        echo "BUILD: git clone attempt $attempt of $max_attempts for $url..."
        if command -v timeout >/dev/null 2>&1; then
            timeout ${timeout_sec}s git clone --progress "${extra_args[@]}" "$url" "$dest" < /dev/null 2>&1
        else
            git clone --progress "${extra_args[@]}" "$url" "$dest" < /dev/null 2>&1
        fi

        if [ $? -eq 0 ]; then
            __SuccessfulClones+=("$dest")
            return 0
        fi

        echo "BUILD: git clone failed or timed out. Retrying in $delay seconds..."
        sleep $delay
        delay=$((delay * 2))
        attempt=$((attempt + 1))
    done

    echo "BUILD: Error: git clone failed after $max_attempts attempts."
    __FailedClones+=("$dest")
    if [[ -n "$_FastFail" ]]; then exit 1; fi
    return 1
}

download_retry()
{
    local url=$1
    local dest=$2

    local max_attempts=5
    local timeout_sec=120
    local attempt=1
    local delay=5

    while [ $attempt -le $max_attempts ]; do
        echo "BUILD: download attempt $attempt of $max_attempts for $url..."
        curl -L -s -o "$dest" --max-time $timeout_sec "$url"

        if [ $? -eq 0 ]; then
            __SuccessfulCommands+=("download_$dest")
            return 0
        fi

        echo "BUILD: download failed or timed out. Retrying in $delay seconds..."
        sleep $delay
        delay=$((delay * 2))
        attempt=$((attempt + 1))
    done

    echo "BUILD: Error: download failed after $max_attempts attempts."
    __FailedCommands+=("download_$dest")
    if [[ -n "$_FastFail" ]]; then exit 1; fi
    return 1
}

initHostDistroRid()
{
    __HostDistroRid=""
    if [ "$__HostOS" == "linux" ]; then
        if [ -e /etc/os-release ]; then
            source /etc/os-release
            if [[ $ID == "alpine" ]]; then
                # remove the last version digit
                VERSION_ID=${VERSION_ID%.*}
            fi
            __HostDistroRid="$ID.$VERSION_ID-$__HostArch"
        elif [ -e /etc/redhat-release ]; then
            local redhatRelease=$(</etc/redhat-release)
            if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
               __HostDistroRid="rhel.6-$__HostArch"
            fi
        fi
    fi
    if [ "$__HostOS" == "freebsd" ]; then
        __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'.'`
        __HostDistroRid="freebsd.$__freebsd_version-$__HostArch"
    fi

    if [ "$__HostDistroRid" == "" ]; then
        echo "WARNING: Can not determine runtime id for current distro."
    fi
}

initTargetDistroRid()
{
    if [ "$__CrossBuild" == "1" ]; then
        if [ "$__BuildOS" == "linux" ]; then
            if [ ! -e $ROOTFS_DIR/etc/os-release ]; then
                if [ -e $ROOTFS_DIR/android_platform ]; then
                    source $ROOTFS_DIR/android_platform
                    export __DistroRid="$RID"
                else
                    echo "WARNING: Can not determine runtime id for current distro."
                    export __DistroRid=""
                fi
            else
                source $ROOTFS_DIR/etc/os-release
                export __DistroRid="$ID.$VERSION_ID-$__BuildArch"
            fi
        fi
    else
        export __DistroRid="$__HostDistroRid"
    fi

    # Portable builds target the base RID
    if [ $__PortableBuild == 1 ]; then
        if [ "$__BuildOS" == "linux" ]; then
            export __DistroRid="linux-$__BuildArch"
        elif [ "$__BuildOS" == "osx" ]; then
            export __DistroRid="osx-$__BuildArch"
        elif [ "$__BuildOS" == "freebsd" ]; then
            export __DistroRid="freebsd-$__BuildArch"
        fi
    fi
}

setup_dirs()
{
    echo "Setting up directories for build"

    mkdir -p "$__RootBinDir"
    mkdir -p "$__BinDir"
    mkdir -p "$__LogsDir"
    mkdir -p "$__IntermediatesDir"

    if [ "$__CrossBuild" == "1" ]; then
        mkdir -p "$__CrossComponentBinDir"
        mkdir -p "$__CrossCompIntermediatesDir"
    fi
}

# Check the system to ensure the right prereqs are in place

check_prereqs()
{
    echo "Checking prerequisites..."

    success=1

    local __MissingPkgs=""
    local __InstalledPkgs=()
    local __MissingPkgsArray=()

    # Check presence of git on the path
    hash git 2>/dev/null
    if ! [[ $? -eq 0 ]]; then
        success=0;
        __MissingPkgs="$__MissingPkgs git"
        __MissingPkgsArray+=("git")
    else
        __InstalledPkgs+=("git")
    fi

    # Check presence of unzip on the path
     hash unzip 2>/dev/null
     if ! [[ $? -eq 0 ]]; then
        success=0;
        __MissingPkgs="$__MissingPkgs unzip"
        __MissingPkgsArray+=("unzip")
    else
        __InstalledPkgs+=("unzip")
    fi

    # Check presence of vcpkg dependencies on the path
    for tool in curl zip tar cmake pkg-config jq; do
        hash $tool 2>/dev/null
        if ! [[ $? -eq 0 ]]; then
            success=0
            __MissingPkgs="$__MissingPkgs $tool"
            __MissingPkgsArray+=("$tool")
        else
            __InstalledPkgs+=("$tool")
        fi
    done
    if ! command -v ninja >/dev/null 2>&1 && ! command -v ninja-build >/dev/null 2>&1; then
        success=0
        if [ "$__HostOS" == "linux" ]; then
            __MissingPkgs="$__MissingPkgs ninja-build"
            __MissingPkgsArray+=("ninja-build")
        else
            __MissingPkgs="$__MissingPkgs ninja"
            __MissingPkgsArray+=("ninja")
        fi
    else
        __InstalledPkgs+=("ninja")
    fi

    # Check for Autotools suite
    if ! command -v autoreconf >/dev/null 2>&1; then
        success=0
        __MissingPkgs="$__MissingPkgs autoconf"
        __MissingPkgsArray+=("autoconf")
    else
        __InstalledPkgs+=("autoconf")
    fi

    if ! command -v automake >/dev/null 2>&1; then
        success=0
        __MissingPkgs="$__MissingPkgs automake"
        __MissingPkgsArray+=("automake")
    else
        __InstalledPkgs+=("automake")
    fi

    if ! command -v libtoolize >/dev/null 2>&1 && ! command -v libtool >/dev/null 2>&1; then
        success=0
        __MissingPkgs="$__MissingPkgs libtool"
        __MissingPkgsArray+=("libtool")
    else
        __InstalledPkgs+=("libtool")
    fi

    if [ "$__CrossBuild" == "1" ]; then
        for tool in "$CC" "$CXX"; do
            if [ -n "$tool" ]; then
                hash "$tool" 2>/dev/null
                if ! [[ $? -eq 0 ]]; then
                    success=0
                    local pkgName="$tool"
                    if [ "$__HostOS" == "linux" ]; then
                        if [[ "$tool" == *"-gcc" ]]; then
                            pkgName="gcc-${tool%-gcc}"
                        elif [[ "$tool" == *"-g++" ]]; then
                            pkgName="g++-${tool%-g++}"
                        fi
                    fi
                    __MissingPkgs="$__MissingPkgs $pkgName"
                    __MissingPkgsArray+=("$pkgName")
                else
                    __InstalledPkgs+=("$tool")
                fi
            fi
        done
    fi

    # Check presence of libgdiplus
    local has_gdiplus=0
    if [ "$__HostOS" == "osx" ]; then
        # Check standard Homebrew paths for Intel and Apple Silicon
        if [ -f "/usr/local/lib/libgdiplus.dylib" ] || [ -f "/opt/homebrew/lib/libgdiplus.dylib" ]; then
            has_gdiplus=1
        fi
    else
        if command -v ldconfig >/dev/null 2>&1 && ldconfig -p | grep -q libgdiplus; then
            has_gdiplus=1
        fi
    fi

    if [ "$has_gdiplus" -eq 0 ]; then
        success=0;
        if [ "$__HostOS" == "osx" ]; then
            __MissingPkgs="$__MissingPkgs mono-libgdiplus"
            __MissingPkgsArray+=("mono-libgdiplus")
        else
            __MissingPkgs="$__MissingPkgs libgdiplus"
            __MissingPkgsArray+=("libgdiplus")
        fi
    else
        __InstalledPkgs+=("libgdiplus")
    fi

    # Check C/C++ compiler presence and minimum version
    if [ -z "$_SkipNative" ]; then
        local valid_compiler_found=0
        local compiler_errors=""

        if [ "$__CrossBuild" == "1" ]; then
            if command -v "$CC" >/dev/null 2>&1; then
                local compiler_type=""
                local min_ver=11 # default to gcc min version
                if [[ "$CC" == *"clang"* ]]; then
                    compiler_type="clang"
                    min_ver=14
                elif [[ "$CC" == *"gcc"* ]]; then
                    compiler_type="gcc"
                    min_ver=11
                fi

                local compiler_ver=""
                if [ "$compiler_type" == "gcc" ]; then
                    compiler_ver=$($CC -dumpversion | cut -d'.' -f1)
                elif [ "$compiler_type" == "clang" ]; then
                    compiler_ver=$($CC --version | head -n1 | grep -Eo '[0-9]+\.[0-9]+' | head -n1 | cut -d'.' -f1)
                fi

                if [ -n "$compiler_ver" ] && [ "$compiler_ver" -ge "$min_ver" ]; then
                    valid_compiler_found=1
                    __InstalledPkgs+=("$CC (v$compiler_ver)")
                else
                    compiler_errors="[Found cross-compiler $CC v$compiler_ver, but require minimum version $min_ver]"
                fi
            else
                compiler_errors="[Cross-compiler $CC not found]"
            fi
        else
            # Native build: check GCC toolchain
            local has_gcc=0
            local has_gxx=0
            if command -v gcc >/dev/null 2>&1; then has_gcc=1; fi
            if command -v g++ >/dev/null 2>&1; then has_gxx=1; fi

            if [ "$has_gcc" -eq 1 ] && [ "$has_gxx" -eq 1 ]; then
                local gcc_ver=$(gcc -dumpversion | cut -d'.' -f1)
                if [ -n "$gcc_ver" ] && [ "$gcc_ver" -ge 11 ]; then
                    valid_compiler_found=1
                    __InstalledPkgs+=("gcc/g++ (v$gcc_ver)")
                else
                    compiler_errors="$compiler_errors [gcc/g++ found but version $gcc_ver is < 11]"
                fi
            elif [ "$has_gcc" -eq 0 ] && [ "$has_gxx" -eq 0 ]; then
                compiler_errors="$compiler_errors [both gcc and g++ missing]"
            elif [ "$has_gcc" -eq 0 ]; then
                compiler_errors="$compiler_errors [gcc missing]"
            else
                compiler_errors="$compiler_errors [g++ missing]"
            fi

            # Native build: check Clang toolchain
            local has_clang=0
            local has_clangxx=0
            if command -v clang >/dev/null 2>&1; then has_clang=1; fi
            if command -v clang++ >/dev/null 2>&1; then has_clangxx=1; fi

            if [ "$has_clang" -eq 1 ] && [ "$has_clangxx" -eq 1 ]; then
                local clang_ver=$(clang --version | head -n1 | grep -Eo '[0-9]+\.[0-9]+' | head -n1 | cut -d'.' -f1)
                if [ -n "$clang_ver" ] && [ "$clang_ver" -ge 14 ]; then
                    valid_compiler_found=1
                    __InstalledPkgs+=("clang/clang++ (v$clang_ver)")
                else
                    compiler_errors="$compiler_errors [clang/clang++ found but version $clang_ver is < 14]"
                fi
            elif [ "$has_clang" -eq 0 ] && [ "$has_clangxx" -eq 0 ]; then
                compiler_errors="$compiler_errors [both clang and clang++ missing]"
            elif [ "$has_clang" -eq 0 ]; then
                compiler_errors="$compiler_errors [clang missing]"
            else
                compiler_errors="$compiler_errors [clang++ missing]"
            fi
        fi

        if [ "$valid_compiler_found" -eq 0 ]; then
            if [ "$__CrossBuild" == "1" ]; then
                echo >&2 "Please install a valid cross-compiler before running this script."
                echo >&2 "Details: $compiler_errors"
                __MissingPkgs="$__MissingPkgs $CC"
                __MissingPkgsArray+=("cross-compiler ($CC) >= $min_ver")
            else
                echo >&2 "Please install a valid C/C++ compiler before running this script."
                echo >&2 "Details:$compiler_errors"

                # Dynamically build the missing packages list based on what was found
                local missing_msg=""
                local apt_pkgs=""

                if [ "$has_gcc" -eq 1 ] && [ "$has_gxx" -eq 0 ]; then
                    missing_msg="g++ (to complete GCC toolchain)"
                    apt_pkgs="g++"
                elif [ "$has_gcc" -eq 0 ] && [ "$has_gxx" -eq 1 ]; then
                    missing_msg="gcc (to complete GCC toolchain)"
                    apt_pkgs="gcc"
                else
                    # Neither gcc nor clang are fully installed or versions were too old
                    missing_msg="Full C/C++ toolchain (e.g. gcc >= 11 AND g++ >= 11, OR clang >= 14)"
                    apt_pkgs="build-essential"
                fi

                if [ "$__HostOS" == "linux" ]; then
                    __MissingPkgs="$__MissingPkgs $apt_pkgs"
                    __MissingPkgsArray+=("$missing_msg")
                else
                    __MissingPkgs="$__MissingPkgs compiler"
                    __MissingPkgsArray+=("$missing_msg")
                fi
            fi
            success=0;
        fi
    fi

    if [ ${#__InstalledPkgs[@]} -ne 0 ]; then
        echo "BUILD: Installed prerequisites:"
        for p in "${__InstalledPkgs[@]}"; do echo "BUILD:   - $p"; done
    fi

    if [ $success -eq 0 ]; then
        echo ""
        echo "BUILD: ======================================================================"
        echo "BUILD:                      MISSING PREREQUISITES"
        echo "BUILD: ======================================================================"
        for p in "${__MissingPkgsArray[@]}"; do echo "BUILD:   - $p"; done
        echo ""
        if [ -n "$__MissingPkgs" ]; then
            echo "BUILD: To build DjvuNet, please install the missing dependencies:"
            if [ "$__HostOS" == "linux" ]; then
                echo "BUILD:   Ubuntu/Debian: sudo apt-get install$__MissingPkgs"
                echo "BUILD:   Fedora/RHEL:   sudo dnf install$__MissingPkgs"
            elif [ "$__HostOS" == "osx" ]; then
                echo "BUILD:   macOS (Homebrew): brew install$__MissingPkgs"
            elif [ "$__HostOS" == "freebsd" ]; then
                echo "BUILD:   FreeBSD: pkg install$__MissingPkgs"
            fi
        fi
        echo "BUILD: Refer to README.md for complete environment setup instructions."
        echo "BUILD: Aborting build."
        exit 1
    fi

    __GlobalJson="$__ProjectRoot/global.json"
    __SdkVersion=$(jq -r '.sdk.version' "$__GlobalJson")
    __SdkChannel=$(echo "$__SdkVersion" | cut -d'.' -f1,2)
    export __SdkVersion
    export __SdkChannel

    echo "BUILD: Target .NET SDK Channel resolved to $__SdkChannel"

    # Query Microsoft for the absolute latest patch in this channel
    __LatestPatchUrl="https://dotnetcli.blob.core.windows.net/dotnet/Sdk/${__SdkChannel}/latest.version"
    if command -v curl >/dev/null 2>&1; then
        __LatestAvailable=$(curl -sL "$__LatestPatchUrl" | tr -d '\r\n')
    else
        __LatestAvailable=$(wget -qO- "$__LatestPatchUrl" | tr -d '\r\n')
    fi

    # Check if system dotnet exists AND satisfies the latest secure patch
    __UseLocalDotnet=1
    __SystemDotnetVer=""
    if hash dotnet 2>/dev/null; then
        __SystemDotnetVer=$(dotnet --version 2>/dev/null | tr -d '\r\n')
        if [ "$__SystemDotnetVer" == "$__LatestAvailable" ]; then
            __UseLocalDotnet=0
            __DotnetVer="$__SystemDotnetVer"
        fi
    fi

    if [ $__UseLocalDotnet -eq 0 ]; then
        echo "BUILD: Globally installed System .NET SDK is up-to-date with latest secure patch: $__LatestAvailable"
    else
        echo "BUILD: ======================================================================"
        if [ -n "$__SystemDotnetVer" ]; then
            echo "BUILD: WARNING: System .NET SDK ($__SystemDotnetVer) is OUTDATED."
        else
            echo "BUILD: WARNING: System .NET SDK is MISSING."
        fi
        echo "BUILD:          The latest secure patch for channel $__SdkChannel is $__LatestAvailable."
        echo "BUILD:          Falling back to isolated local tools to ensure build security."
        echo "BUILD:"
        echo "BUILD: STATUS:  A secure, isolated .NET SDK [$__LatestAvailable] is fully provisioned"
        echo "BUILD:          within the repository context (Tools/coreclr/dotnetcli)."
        echo "BUILD:          All compilation and tool execution will map to this local instance"
        echo "BUILD:          to maintain hermetic build guarantees and prevent CI state bleed."
        echo "BUILD: ======================================================================"

        __LocalDotNetDir="$__ProjectRoot/Tools/coreclr/dotnetcli/$__OSName/$__Libc/$__ArchName"

        if [ -n "$__LatestAvailable" ] && [ "$__LatestAvailable" != "$__SdkVersion" ]; then
            echo "BUILD: Updating global.json to track target SDK version $__LatestAvailable before initialization"
            jq --arg v "$__LatestAvailable" '.sdk.version = $v' "$__GlobalJson" > "${__GlobalJson}.tmp" && mv "${__GlobalJson}.tmp" "$__GlobalJson"
        fi

        bash "$__ProjectRoot/init-tools.sh" "$__LocalDotNetDir"
        if [ $? -ne 0 ]; then
            echo "BUILD: Error initializing tools."
            exit 1
        fi

        export DOTNET_ROOT="$__LocalDotNetDir"
        export PATH="${__LocalDotNetDir}:$PATH"
        __DotnetVer=$(dotnet --version | tr -d '\r\n')
    fi
    export __DotnetCmd="dotnet"

    echo "dotnet $__DotnetVer installed"
    __MSBuildVer=$(dotnet msbuild /nologo /version)
    if [[ -z "$__MSBuildVer" ]]; then
        echo "dotnet sdk not installed $__MSBuildVer"
        exit 1
    else
        echo "msbuild $__MSBuildVer installed"
    fi
}

build_native()
{
    skipCondition=$1
    platformArch="$2"
    intermediatesForBuild="$3"
    extraCmakeArguments="$4"
    message="$5"

    if [ $skipCondition == 1 ]; then
        echo "Skipping $message build."
        return
    fi

    # All set to commence the build
    echo "Commencing build of $message for $__BuildOS.$__BuildArch.$__BuildType in $intermediatesForBuild"

    generator=""
    buildFile="Makefile"
    buildTool="make"
    if [ $__UseNinja == 1 ]; then
        generator="ninja"
        buildFile="build.ninja"
        if ! buildTool=$(command -v ninja || command -v ninja-build); then
           echo "Unable to locate ninja!" 1>&2
           exit 1
        fi
    fi

    if [ "$__SkipConfigure" == "0" ]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [ $__isMSBuildOnNETCoreSupported == 0 ]; then __SkipGenerateVersion=1; fi
        # Drop version.cpp file
        __versionSourceFile="$intermediatesForBuild/version.cpp"
        if [ $__SkipGenerateVersion == 0 ]; then
            pwd
            "$__ProjectRoot/run.sh" build -Project=$__ProjectDir/build.proj -generateHeaderUnix -NativeVersionSourceFile=$__versionSourceFile $__RunArgs $__UnprocessedBuildArgs
        else
            # Generate the dummy version.cpp, but only if it didn't exist to make sure we don't trigger unnecessary rebuild
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            if [ -e $__versionSourceFile ]; then
                read existingVersionSourceLine < $__versionSourceFile
            fi
            if [ "$__versionSourceLine" != "$existingVersionSourceLine" ]; then
                echo $__versionSourceLine > $__versionSourceFile
            fi
        fi


        pushd "$intermediatesForBuild"
        # Regenerate the CMake solution
        echo "Invoking \"$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh\" \"$__ProjectRoot\" $__ClangMajorVersion $__ClangMinorVersion $platformArch $__BuildType $__CodeCoverage $__IncludeTests $generator $extraCmakeArguments $__cmakeargs"
        "$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh" "$__ProjectRoot" $__ClangMajorVersion $__ClangMinorVersion $platformArch $__BuildType $__CodeCoverage $__IncludeTests $generator "$extraCmakeArguments" "$__cmakeargs"
        popd
    fi

    if [ ! -f "$intermediatesForBuild/$buildFile" ]; then
        echo "Failed to generate $message build project!"
        exit 1
    fi

    # Build
    if [ $__ConfigureOnly == 1 ]; then
        echo "Finish configuration & skipping $message build."
        return
    fi

    # Check that the makefiles were created.
    pushd "$intermediatesForBuild"

    echo "Executing $buildTool install -j $__NumProc"

    $buildTool install -j $__NumProc
    if [ $? != 0 ]; then
        echo "Failed to build $message."
        exit 1
    fi

    popd
}

isMSBuildOnNETCoreSupported()
{
    __isMSBuildOnNETCoreSupported="${__msbuildonunsupportedplatform:-0}"

    if [ "$__isMSBuildOnNETCoreSupported" == "1" ]; then
        return
    fi

    if [ "$__HostArch" == "x64" ]; then
        if [ "$__HostOS" == "linux" ]; then
            __isMSBuildOnNETCoreSupported=1
            # note: the RIDs below can use globbing patterns
            UNSUPPORTED_RIDS=("debian.9-x64" "ubuntu.17.04-x64")
            for UNSUPPORTED_RID in "${UNSUPPORTED_RIDS[@]}"
            do
                if [[ $__HostDistroRid == $UNSUPPORTED_RID ]]; then
                    __isMSBuildOnNETCoreSupported=0
                    break
                fi
            done
        elif [ "$__HostOS" == "osx" ]; then
            __isMSBuildOnNETCoreSupported=1
        fi
    fi
}


build_CoreLib_ni()
{
    if [ $__SkipCrossgen == 1 ]; then
        echo "Skipping generating native image"
        return
    fi

    if [ $__SkipCoreCLR == 0 -a -e $__BinDir/crossgen ]; then
        echo "Generating native image for System.Private.CoreLib."
        echo "$__BinDir/crossgen /Platform_Assemblies_Paths $__BinDir/IL $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__BinDir/IL/System.Private.CoreLib.dll"
        $__BinDir/crossgen /Platform_Assemblies_Paths $__BinDir/IL $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__BinDir/IL/System.Private.CoreLib.dll
        if [ $? -ne 0 ]; then
            echo "Failed to generate native image for System.Private.CoreLib."
            exit 1
        fi

        if [ "$__BuildOS" == "linux" ]; then
            echo "Generating symbol file for System.Private.CoreLib."
            $__BinDir/crossgen /CreatePerfMap $__BinDir $__BinDir/System.Private.CoreLib.dll
            if [ $? -ne 0 ]; then
                echo "Failed to generate symbol file for System.Private.CoreLib."
                exit 1
            fi
        fi
    fi
}

build_CoreLib()
{

    if [ $__isMSBuildOnNETCoreSupported == 0 ]; then
        echo "System.Private.CoreLib.dll build unsupported."
        return
    fi

    if [ $__SkipMSCorLib == 1 ]; then
       echo "Skipping building System.Private.CoreLib."
       return
    fi

    echo "Commencing build of managed components for $__BuildOS.$__BuildArch.$__BuildType"

    # Invoke MSBuild
    __ExtraBuildArgs=""
    if [[ "$__IbcTuning" -eq "" ]]; then
        __ExtraBuildArgs="$__ExtraBuildArgs -OptimizationDataDir=\"$__PackagesDir/optimization.$__BuildOS-$__BuildArch.IBC.CoreCLR/$__IbcOptDataVersion/data/\""
        __ExtraBuildArgs="$__ExtraBuildArgs -EnableProfileGuidedOptimization=true"
    fi
    $__ProjectRoot/run.sh build -Project=$__ProjectDir/build.proj -MsBuildLog="/flp:Verbosity=normal;LogFile=$__LogsDir/System.Private.CoreLib_$__BuildOS__$__BuildArch__$__BuildType.log" -BuildTarget -__IntermediatesDir=$__IntermediatesDir -__RootBinDir=$__RootBinDir -BuildNugetPackage=false -UseSharedCompilation=false $__RunArgs $__ExtraBuildArgs $__UnprocessedBuildArgs

    if [ $? -ne 0 ]; then
        echo "Failed to build managed components."
        exit 1
    fi

    # The cross build generates a crossgen with the target architecture.
    if [ $__CrossBuild != 1 ]; then
       # The architecture of host pc must be same architecture with target.
       if [[ ( "$__HostArch" == "$__BuildArch" ) ]]; then
           build_CoreLib_ni
       elif [[ ( "$__HostArch" == "x64" ) && ( "$__BuildArch" == "x86" ) ]]; then
           build_CoreLib_ni
       elif [[ ( "$__HostArch" == "arm64" ) && ( "$__BuildArch" == "arm" ) ]]; then
           build_CoreLib_ni
       else
           exit 1
       fi
    fi
}

generate_NugetPackages()
{
    # We can only generate nuget package if we also support building mscorlib as part of this build.
    if [ $__isMSBuildOnNETCoreSupported == 0 ]; then
        echo "Nuget package generation unsupported."
        return
    fi

    # Since we can build mscorlib for this OS, did we build the native components as well?
    if [ $__SkipCoreCLR == 1 ]; then
        echo "Unable to generate nuget packages since native components were not built."
        return
    fi

    echo "Generating nuget packages for "$__BuildOS
    echo "DistroRid is "$__DistroRid
    echo "ROOTFS_DIR is "$ROOTFS_DIR
    # Build the packages
    $__ProjectRoot/run.sh build -Project=$__SourceDir/.nuget/packages.builds -MsBuildLog="/flp:Verbosity=normal;LogFile=$__LogsDir/Nuget_$__BuildOS__$__BuildArch__$__BuildType.log" -BuildTarget -__IntermediatesDir=$__IntermediatesDir -__RootBinDir=$__RootBinDir -BuildNugetPackage=false -UseSharedCompilation=false $__RunArgs $__UnprocessedBuildArgs

    if [ $? -ne 0 ]; then
        echo "Failed to generate Nuget packages."
        exit 1
    fi
}

echo ; echo "BUILD: Starting Build of DjvuNet at $(date +"%Y-%m-%d %H:%M:%S.%2N")"; echo "";

# Argument types supported by this script:
#
# Build platform        - valid values are: x64, x86, arm, armel, arm64.
# Build configuration   - valid values are: Debug, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Detect OS Family based on TestPlatforms matrix
__UnameOS=$(uname -s | tr '[:upper:]' '[:lower:]')
case $__UnameOS in
    linux)
        # Differentiate Android from standard Linux
        if [ -n "$ANDROID_STORAGE" ] || [ -d "/system/app" ]; then __OSName="android"; else __OSName="linux"; fi
        ;;
    darwin)
        # Differentiate iOS/tvOS/MacCatalyst from OSX natively if possible, default to osx
        if [ "$TARGET_OS" == "ios" ]; then __OSName="ios";
        elif [ "$TARGET_OS" == "tvos" ]; then __OSName="tvos";
        elif [ "$TARGET_OS" == "maccatalyst" ]; then __OSName="maccatalyst";
        else __OSName="osx"; fi
        ;;
    freebsd) __OSName="freebsd" ;;
    netbsd)  __OSName="netbsd" ;;
    openbsd) __OSName="openbsd" ;;
    sunos)
        # Differentiate illumos from legacy Solaris
        if uname -v | grep -qi "illumos"; then __OSName="illumos"; else __OSName="solaris"; fi
        ;;
    haiku)   __OSName="haiku" ;;
    wasi)    __OSName="wasi" ;;
    *)       __OSName="$__UnameOS" ;;
esac

# Detect Libc Variant (Crucial for Linux permutations)
__Libc="gnu"
if [ "$__OSName" == "linux" ]; then
    if [ -f /etc/alpine-release ] || (command -v ldd >/dev/null && ldd --version 2>&1 | grep -q "musl"); then
        __Libc="musl"
    elif [ -n "$ANDROID_STORAGE" ] || (command -v ldd >/dev/null && ldd --version 2>&1 | grep -q "bionic"); then
        __Libc="bionic"
    fi
elif [[ "$__OSName" == "osx" || "$__OSName" == "ios" || "$__OSName" == "tvos" || "$__OSName" == "maccatalyst" ]]; then
    __Libc="darwin"
elif [[ "$__OSName" == *"bsd" ]]; then
    __Libc="bsd"
elif [[ "$__OSName" == "solaris" || "$__OSName" == "illumos" ]]; then
    __Libc="sun"
elif [ "$__OSName" == "android" ]; then
    __Libc="bionic"
fi

# Detect Architecture
__UnameArch=$(uname -m | tr '[:upper:]' '[:lower:]')
case $__UnameArch in
    x86_64|amd64)    __ArchName="x64" ;;
    aarch64|arm64)   __ArchName="arm64" ;;
    armv7l|armv8l|armhf) __ArchName="arm" ;;
    i386|i486|i586|i686) __ArchName="x86" ;;
    s390x)           __ArchName="s390x" ;;
    ppc64le)         __ArchName="ppc64le" ;;
    riscv64)         __ArchName="riscv64" ;;
    wasm32)          __ArchName="wasm" ;;
    *)               __ArchName="$__UnameArch" ;;
esac

__BuildOS=$__OSName
__HostOS=$__OSName
__BuildArch=$__ArchName
__HostArch=$__ArchName

# Get the number of physical processors available to the scheduler
if [ `uname` = "FreeBSD" ]; then
  __NumProc=`sysctl hw.ncpu | awk '{ print $2 }'`
elif [ `uname` = "NetBSD" ]; then
  __NumProc=$(getconf NPROCESSORS_ONLN)
elif [ `uname` = "Darwin" ]; then
  __NumProc=`sysctl hw.physicalcpu | awk '{ print $2 }'`
else
  if command -v lscpu >/dev/null 2>&1; then
    __NumProc=$(lscpu -p=CORE | grep -v '#' | sort -u | wc -l)
  else
    __NumProc=$(nproc --all)
  fi
fi

# Set default values
_MSB_Target="Build"
_MSB_Configuration="Debug"
_MSB_Platform="x64"
_Verbosity="normal"
_Processors=$__NumProc
_OS="Linux"
_SkipNative=""
_BuildDjvuNet="1"
_BuildTools=""
_BuildTests=""
_RunTests=""
_Test=""
_Pack=""
_FastFail=""
__FailedRestores=()
__FailedBuilds=()
__FailedPublishes=()
__FailedTests=()
__FailedClones=()
__FailedCommands=()
__SuccessfulRestores=()
__SuccessfulBuilds=()
__SuccessfulPublishes=()
__SuccessfulTests=()
__SuccessfulClones=()
__SuccessfulCommands=()
_DefaultNetCoreApp="net10.0"
_NetCoreAppId=".NETCoreApp"
_NetCoreAppTFM=".NETCoreApp,Version=v10.0"
_DefaultNetStandard="netstandard2.1"
_NetStandardId=".NETStandard"
_NetStandardTFM=".NETStandard,Version=v2.1"
_Framework="$_DefaultNetCoreApp"
__ArtifactsReleaseTag="v0.9.26135.0"
__GithubDjvuNetReleaseUri="https://github.com/DjvuNet/artifacts/releases/download/${__ArtifactsReleaseTag}/"
__ArtifactsTestDataUri="https://github.com/DjvuNet/artifacts/archive/refs/tags/${__ArtifactsReleaseTag}.tar.gz"
__ArtifactsDirName="artifacts-${__ArtifactsReleaseTag#v}"
__LibGit2SharpRepoUri="https://github.com/4creators/libgit2sharp"

# Parse command line
while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        -Configuration|-c)
        _MSB_Configuration="$2"; shift 2 ;;
        -Platform|-p)
        _MSB_Platform="$2"; shift 2 ;;
        -Target|-t)
        _MSB_Target="$2"; shift 2 ;;
        -BuildDjvuNet|-DjvuNet)
        _BuildDjvuNet=1; shift 1 ;;
        -Tools|-ts)
        _BuildTools=1; shift 1 ;;
        -BuildTests|-bt)
        _BuildTests=1; shift 1 ;;
        -RunTests|-rt)
        _RunTests=1; shift 1 ;;
        -Test)
        _Test=1; shift 1 ;;
        -FastFail|-ff)
        _FastFail=1; shift 1 ;;
        -Framework|-f)
        _Framework="$2"; shift 2 ;;
        -SkipNative|-sn)
        _SkipNative=1; shift 1 ;;
        -Verbosity|-v)
        _Verbosity="$2"; shift 2 ;;
        -Processors|-proc)
        _Processors="$2"; shift 2 ;;
        -OS)
        _OS="$2"; shift 2 ;;
        -h|--help)
        usage
        exit 0 ;;
        *)
        echo "Unknown command line parameter: $1"; usage; exit 1 ;;
    esac
done

# check_params
_MSB_ConfigurationLower=$(echo "$_MSB_Configuration" | tr '[:upper:]' '[:lower:]')
if [[ "$_MSB_ConfigurationLower" == "debug" ]]; then
    _MSB_Configuration="Debug"
elif [[ "$_MSB_ConfigurationLower" == "release" ]]; then
    _MSB_Configuration="Release"
else
    echo "Invalid command line parameter -c/-Configuration: $_MSB_Configuration"; usage; exit 1
fi

_MSB_Platform=$(echo "$_MSB_Platform" | tr '[:upper:]' '[:lower:]')

if [[ "$_MSB_Platform" == "arm" || "$_MSB_Platform" == "arm64" || "$_MSB_Platform" == "armel" ]]; then
    __ManagedPlatform="AnyCPU"
    __BuildArch="$_MSB_Platform"
    if [[ "$__HostArch" != "$__BuildArch" ]]; then __CrossBuild=1; fi
    if [[ "$__HostArch" == "x64" ]]; then __SkipNativeTests=1; fi
elif [[ "$_MSB_Platform" == "anycpu" ]]; then
    _MSB_Platform="x64"
    __ManagedPlatform="AnyCPU"
    __BuildArch="x64"
    if [[ "$__HostArch" != "$__BuildArch" ]]; then __CrossBuild=1; fi
elif [[ "$_MSB_Platform" == "x64" ]]; then
    _MSB_Platform="x64"
    __ManagedPlatform="x64"
    __BuildArch="x64"
    if [[ "$__HostArch" != "$__BuildArch" ]]; then __CrossBuild=1; fi
elif [[ "$_MSB_Platform" == "x86" ]]; then
    _MSB_Platform="x86"
    __ManagedPlatform="x86"
    __BuildArch="x86"
    if [[ "$__HostArch" != "$__BuildArch" ]]; then __CrossBuild=1; fi
else
    echo "Invalid command line parameter -p/-Platform: $_MSB_Platform"; usage; exit 1
fi

if [[ -z "$__ManagedPlatform" ]]; then __ManagedPlatform="$_MSB_Platform"; fi
_MSB_TargetLower=$(echo "$_MSB_Target" | tr '[:upper:]' '[:lower:]')
if [[ "$_MSB_TargetLower" == "clean" ]]; then __SkipPublish=1; fi

# Accepted Framework values
_FrameworkLower=$(echo "$_Framework" | tr '[:upper:]' '[:lower:]')
_DefaultNetCoreAppLower=$(echo "$_DefaultNetCoreApp" | tr '[:upper:]' '[:lower:]')
_DefaultNetStandardLower=$(echo "$_DefaultNetStandard" | tr '[:upper:]' '[:lower:]')
if [[ "$_FrameworkLower" == "netcoreapp" || "$_FrameworkLower" == "$_DefaultNetCoreAppLower" ]]; then
    _Framework="$_DefaultNetCoreApp"
    __TargetFrameworkMoniker="$_NetCoreAppTFM"
elif [[ "$_FrameworkLower" == "netstandard" || "$_FrameworkLower" == "$_DefaultNetStandardLower" ]]; then
    _Framework="$_DefaultNetStandard"
    TargetFrameworkIdentifier=".NETStandard"
    __TargetFrameworkMoniker="$_NetStandardTFM"
else
    echo "Invalid command line parameter -f/-Framework: $_Framework"; usage; exit 1
fi

export TargetFramework="$_Framework"

if [[ -n "$_Test" ]]; then _BuildDjvuNet=1; _BuildTests=1; _RunTests=1; fi

if [[ -z "$_BuildDjvuNet" ]]; then
    if [[ -n "$_BuildTests" ]]; then _BuildDjvuNet=1; fi
fi

# init the host distro name
initHostDistroRid

# Set the remaining variables based upon the determined build configuration
__isMSBuildOnNETCoreSupported=0

# Init if MSBuild for .NET Core is supported for this platform
isMSBuildOnNETCoreSupported

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to set.
# This is needed by CLI to function.
if [ -z "$HOME" ]; then
    if [ ! -d "$__ProjectDir/temp_home" ]; then
        mkdir temp_home
    fi
    export HOME=$__ProjectDir/temp_home
    echo "HOME not defined; setting it to $HOME"
fi

# Configure environment if we are doing a cross compile.
if [ "$__CrossBuild" == "1" ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__ProjectRoot/cross/rootfs/$__BuildArch"
    fi

    if [ -z "${CC:-}" ] || [ -z "${CXX:-}" ]; then
        if [ "$__BuildArch" == "arm64" ]; then
            export CC="aarch64-linux-gnu-gcc"
            export CXX="aarch64-linux-gnu-g++"
        elif [ "$__BuildArch" == "arm" ] || [ "$__BuildArch" == "armel" ]; then
            export CC="arm-linux-gnueabihf-gcc"
            export CXX="arm-linux-gnueabihf-g++"
        elif [ "$__BuildArch" == "x64" ]; then
            export CC="x86_64-linux-gnu-gcc"
            export CXX="x86_64-linux-gnu-g++"
        elif [ "$__BuildArch" == "x86" ]; then
            export CC="i686-linux-gnu-gcc"
            export CXX="i686-linux-gnu-g++"
        fi
    fi
fi

# Check prerequisites
check_prereqs

__RootBuildDir="${__ProjectRoot}/build/bin/"
if [ "$__HostOS" == "osx" ]; then
    __RuntimeIdentifier="osx-${_MSB_Platform}"
elif [ "$__HostOS" == "freebsd" ]; then
    __RuntimeIdentifier="freebsd-${_MSB_Platform}"
else
    __RuntimeIdentifier="linux-${_MSB_Platform}"
fi

__BuildToolsUri="${__GithubDjvuNetReleaseUri}Tools.tar.gz"
if [ ! -f "Tools.tar.gz" ] || [ ! -d "Tools" ]; then
    echo "BUILD: Downloading DjvuNet.Build.Tools from ${__BuildToolsUri}"
    download_retry "$__BuildToolsUri" "Tools.tar.gz"
    if [ $? -eq 0 ]; then
        mkdir -p Tools
        tar -xzf Tools.tar.gz -C Tools
        chmod -R a+rX Tools
        echo "BUILD: Extracted DjvuNet.Build.Tools successfully"
    else
        echo "BUILD: Error: Failed to download DjvuNet.Build.Tools from $__BuildToolsUri"
        __FailedCommands+=("download_Tools.tar.gz")
        if [[ -n "$_FastFail" ]]; then exit 1; fi
    fi
else
    echo "BUILD: DjvuNet.Build.Tools already restored"
fi

__SystemAttrProj="System.Attributes/System.Attributes.csproj"
__LibGit2SharpProj="eng/tools/libgit2sharp/LibGit2Sharp/LibGit2Sharp.csproj"
__DjvuNetGitTasksProj="eng/tools/DjvuNet.Build.Tasks/DjvuNet.Build.Tasks.csproj"
__DjvuNetProj="DjvuNet/DjvuNet.csproj"
__DjvuNetDjvuLibreProj="DjvuNet.DjvuLibre/DjvuNet.DjvuLibre.csproj"

if [[ -n "$_BuildTools" ]]; then
    if [ ! -f "${__ProjectRoot}/${__LibGit2SharpProj}" ]; then
        echo "BUILD: Setting up libgit2sharp"
        # __Lg2sArchiveUrl="${__LibGit2SharpRepoUri}/archive/refs/tags/${__ArtifactsReleaseTag}.tar.gz"
        # echo "BUILD: Downloading release archive of libgit2sharp for tag ${__ArtifactsReleaseTag}"
        # download_retry "$__Lg2sArchiveUrl" "libgit2sharp.tar.gz"
        # if [ $? -eq 0 ]; then
        #     echo "BUILD: Extracting libgit2sharp archive"
        #     mkdir -p "${__ProjectRoot}/eng/tools/libgit2sharp"
        #     tar -xzf libgit2sharp.tar.gz -C "${__ProjectRoot}/eng/tools/libgit2sharp" --strip-components=1
        #     rm libgit2sharp.tar.gz
        # else
            echo "BUILD: Download failed, falling back to git clone"
            git_clone_retry "${__LibGit2SharpRepoUri}.git" "eng/tools/libgit2sharp" "--depth 1 -c core.autocrlf=false"
        # fi
    fi
fi

__OutputDir="${__RootBuildDir}${_OS}.${__ManagedPlatform}.${_MSB_Configuration}/binaries/${_Framework}/"
__PublishDir="${__OutputDir}${__RuntimeIdentifier}/publish/"
__LogsDir="${__RootBuildDir}${_OS}.${_MSB_Platform}.${_MSB_Configuration}/logs/${_Framework}/"

echo "BUILD: __OutputDir [${__OutputDir}]"
echo "BUILD: __PublishDir [${__PublishDir}]"

__BuildCommandArgs=("-p:Configuration=${_MSB_Configuration}" "-p:Platform=${__ManagedPlatform}" "-p:TargetFramework=${_Framework}" "-p:RuntimeIdentifier=${__RuntimeIdentifier}" "-p:PublishDir=${__PublishDir}" "-v:${_Verbosity}" "-m:${_Processors}" "-nologo" "-nr:false")
__RestoreCmdArgs=("${__BuildCommandArgs[@]}")

mkdir -p "$__LogsDir"

restore_dotnet_proj() {
    local __DjvuTargetProject=$1
    local __DjvuTargetProjectName=$2

    local __RestoreLogRootName="${__DjvuTargetProjectName}.Restore"
    local __RestoreLog="${__LogsDir}${__RestoreLogRootName}.log"
    local __RestoreWrn="${__LogsDir}${__RestoreLogRootName}.wrn"
    local __RestoreErr="${__LogsDir}${__RestoreLogRootName}.err"
    local __MsbuildLogging=("/flp:Verbosity=diag;LogFile=${__RestoreLog}" "/flp1:WarningsOnly;LogFile=${__RestoreWrn}" "/flp2:ErrorsOnly;LogFile=${__RestoreErr}")

    echo "BUILD: Restoring $__DjvuTargetProject"
    echo "BUILD: Calling: $__DotnetCmd msbuild /t:Restore ${__RestoreCmdArgs[@]} -tl ${__MsbuildLogging[@]} $__DjvuTargetProject"
    "$__DotnetCmd" msbuild /t:Restore "${__RestoreCmdArgs[@]}" -tl "${__MsbuildLogging[@]}" "$__DjvuTargetProject"
    if [[ $? -ne 0 ]]; then
        echo "BUILD: Error: nuget restore of $__DjvuTargetProject returned error"
        __FailedRestores+=("$__DjvuTargetProject")
        if [[ -n "$_FastFail" ]]; then exit 1; fi
    else
        echo "BUILD: Success: nuget restore of $__DjvuTargetProject finished"
        __SuccessfulRestores+=("$__DjvuTargetProject")
    fi
}

build_dotnet_proj() {
    local __BuildProj=$1
    local __BuildProjName=$2

    local __BuildLogRootName="${__BuildProjName}.${_MSB_Target}"
    local __BuildLog="${__LogsDir}${__BuildLogRootName}.log"
    local __BuildWrn="${__LogsDir}${__BuildLogRootName}.wrn"
    local __BuildErr="${__LogsDir}${__BuildLogRootName}.err"
    local __MsbuildLogging=("/flp:Verbosity=diag;LogFile=${__BuildLog}" "/flp1:WarningsOnly;LogFile=${__BuildWrn}" "/flp2:ErrorsOnly;LogFile=${__BuildErr}")

    echo ""
    echo "BUILD: Building $__BuildProj"
    echo "BUILD: calling $__DotnetCmd msbuild ${__BuildCommandArgs[@]} -t:${_MSB_Target} ${__MsbuildLogging[@]} ${__ProjectRoot}/${__BuildProj}"
    "$__DotnetCmd" msbuild "${__BuildCommandArgs[@]}" -t:${_MSB_Target} "${__MsbuildLogging[@]}" "${__ProjectRoot}/${__BuildProj}"
    if [[ $? -ne 0 ]]; then
        echo "BUILD: Error: $__BuildProj build failed. Refer to the build log files for details:"
        echo "    $__BuildLog"
        echo "    $__BuildWrn"
        echo "    $__BuildErr"
        __FailedBuilds+=("$__BuildProjName")
        if [[ -n "$_FastFail" ]]; then exit 1; fi
    else
        __SuccessfulBuilds+=("$__BuildProjName")
    fi

    if [ -z "$__SkipPublish" ]; then
        local __PublishLogRootName="${__BuildProjName}.Publish"
        local __PublishLog="${__LogsDir}${__PublishLogRootName}.log"
        local __PublishWrn="${__LogsDir}${__PublishLogRootName}.wrn"
        local __PublishErr="${__LogsDir}${__PublishLogRootName}.err"
        local __MsbuildPubLogging=("/flp:Verbosity=diag;LogFile=${__PublishLog}" "/flp1:WarningsOnly;LogFile=${__PublishWrn}" "/flp2:ErrorsOnly;LogFile=${__PublishErr}")

        echo ""
        echo "BUILD: Publishing $__BuildProj"
        echo "BUILD: calling $__DotnetCmd msbuild ${__BuildCommandArgs[@]} -t:Publish ${__MsbuildPubLogging[@]} ${__ProjectRoot}/${__BuildProj}"
        "$__DotnetCmd" msbuild "${__BuildCommandArgs[@]}" -t:Publish "${__MsbuildPubLogging[@]}" "${__ProjectRoot}/${__BuildProj}"
        if [[ $? -ne 0 ]]; then
            echo "BUILD: Error: $__BuildProj publish failed. Refer to the publish log files for details:"
            echo "    $__PublishLog"
            echo "    $__PublishWrn"
            echo "    $__PublishErr"
            __FailedPublishes+=("$__BuildProjName")
            if [[ -n "$_FastFail" ]]; then exit 1; fi
        else
            __SuccessfulPublishes+=("$__BuildProjName")
        fi
    fi
}

run_dotnet_test() {
    local __DjvuTargetTestExe=$1
    local __DjvuTargetTestName=$2

    if [ "$__HostOS" == "osx" ]; then
        export DYLD_FALLBACK_LIBRARY_PATH="/opt/homebrew/lib:/usr/local/lib:${DYLD_FALLBACK_LIBRARY_PATH:-}"
    fi

    echo ""
    if [ ! -f "$__DjvuTargetTestExe" ]; then
        echo "BUILD: Skipping $__DjvuTargetTestName because assembly is missing."
        __FailedTests+=("$__DjvuTargetTestName")
    else
        echo "BUILD: Running tests from $__DjvuTargetTestName assembly"
        echo "BUILD: calling: \"$__DotnetCmd\" \"$__DjvuTargetTestExe\" $_Test_Options -${__TestOutputFormat} \"${__TestResOutputDir}${__DjvuTargetTestName}.${__TestOutputFormat}\""
        if ! "$__DotnetCmd" "$__DjvuTargetTestExe" $_Test_Options -${__TestOutputFormat} "${__TestResOutputDir}${__DjvuTargetTestName}.${__TestOutputFormat}"; then
            __FailedTests+=("$__DjvuTargetTestName")
            if [[ -n "$_FastFail" ]]; then exit 1; fi
        else
            __SuccessfulTests+=("$__DjvuTargetTestName")
        fi
    fi
}


__NativeDepsSemaphore="deps/{87E5AD66-912F-477C-BDA5-52F7785AE705}"
if [ ! -f "$__NativeDepsSemaphore" ]; then
    if [ -d "deps" ]; then
        echo "BUILD: Not found semaphore file: $__NativeDepsSemaphore"
        rm -rf deps
        echo "BUILD: Deleted deps directory"
    fi
    echo "BUILD: Downloading custom System.Drawing.Common dependencies from ${__GithubDjvuNetReleaseUri}deps.tar.gz"
    download_retry "${__GithubDjvuNetReleaseUri}deps.tar.gz" "deps.tar.gz"
    if [ $? -eq 0 ]; then
        mkdir -p deps
        tar -xzf deps.tar.gz -C deps
        chmod -R a+rX deps
        rm deps.tar.gz
        touch "$__NativeDepsSemaphore"
        echo "BUILD: Created custom System.Drawing.Common dependencies semaphore $__NativeDepsSemaphore"
    else
        echo "BUILD: Error: Failed to download custom System.Drawing.Common dependencies from ${__GithubDjvuNetReleaseUri}deps.tar.gz"
        _SkipNative=1
    fi
else
    echo "BUILD: Custom System.Drawing.Common dependencies already restored"
fi

if [ -z "$_SkipNative" ]; then
    __DjvuLibreDir="djvulibre"

    if [ ! -f "$__ProjectRoot/$__DjvuLibreDir/autogen.sh" ]; then
        echo "BUILD: Setting up DjVuLibre"
        __ArchiveUrl="https://github.com/DjvuNet/DjVuLibre/archive/refs/tags/${__ArtifactsReleaseTag}.tar.gz"
        echo "BUILD: Downloading release archive of DjVuLibre for tag ${__ArtifactsReleaseTag}"
        download_retry "$__ArchiveUrl" "djvulibre.tar.gz"
        if [ $? -eq 0 ]; then
            echo "BUILD: Extracting DjVuLibre archive"
            mkdir -p "$__DjvuLibreDir"
            tar -xzf djvulibre.tar.gz -C "$__DjvuLibreDir" --strip-components=1
            rm djvulibre.tar.gz
        else
            echo "BUILD: Download failed, falling back to git clone"
            git_clone_retry "https://github.com/DjvuNet/DjVuLibre.git" "$__DjvuLibreDir" --depth 1 -c core.autocrlf=false
        fi
        if [ $? -ne 0 ]; then _SkipNative=1; fi
    else
        echo "BUILD: DjvuLibre already cloned"
    fi

    if [ -z "$_SkipNative" ]; then
        __VcpkgBaseline=$(awk -F'"' '/"builtin-baseline"[ \t]*:/ {print $4}' "$__ProjectRoot/$__DjvuLibreDir/vcpkg.json" 2>/dev/null || true)

        is_valid_vcpkg() {
            local dir=$1
            if [ ! -f "$dir/vcpkg" ] && [ ! -f "$dir/vcpkg.exe" ]; then return 1; fi
            if [ ! -d "$dir/.git" ]; then return 1; fi
            if [ -n "$__VcpkgBaseline" ]; then
                if ! git -C "$dir" cat-file -e -- "${__VcpkgBaseline}^{commit}" >/dev/null 2>&1; then
                    return 1
                fi
            fi
            return 0
        }

        __GlobalVcpkgRoot=""
        if [ -n "${VCPKG_ROOT:-}" ] && is_valid_vcpkg "$VCPKG_ROOT"; then
            __GlobalVcpkgRoot="$VCPKG_ROOT"
        elif command -v vcpkg >/dev/null 2>&1 && is_valid_vcpkg "$(dirname "$(command -v vcpkg)")"; then
            __GlobalVcpkgRoot=$(dirname "$(command -v vcpkg)")
        else
            __GlobalVcpkgRoot="$__ProjectRoot/vcpkg"
        fi
        __GlobalVcpkgRoot="${__GlobalVcpkgRoot%/}"

        if [ "$__GlobalVcpkgRoot" == "$__ProjectRoot/vcpkg" ]; then

            clone_valid=0
            if [ -d "$__GlobalVcpkgRoot/.git" ] && [ -f "$__GlobalVcpkgRoot/bootstrap-vcpkg.sh" ] && [ -f "$__GlobalVcpkgRoot/vcpkg" ]; then
                if [ -n "$__VcpkgBaseline" ]; then
                    if git -C "$__GlobalVcpkgRoot" cat-file -e -- "${__VcpkgBaseline}^{commit}" >/dev/null 2>&1; then
                        clone_valid=1
                    fi
                else
                    clone_valid=1
                fi
            fi

            if [ $clone_valid -eq 0 ]; then
                if [ -d "$__GlobalVcpkgRoot" ]; then
                    echo "BUILD: Removing broken vcpkg baseline at $__GlobalVcpkgRoot"
                    rm -rf "$__GlobalVcpkgRoot"
                fi

                echo "BUILD: Cloning local Microsoft vcpkg baseline"

                git_clone_retry "https://github.com/Microsoft/vcpkg.git" "vcpkg" -c core.autocrlf=false
                if [ $? -ne 0 ]; then
                    _SkipNative=1
                fi
            fi

            if [ -z "$_SkipNative" ] && [ ! -f "$__GlobalVcpkgRoot/vcpkg" ]; then
                echo "BUILD: Bootstrapping vcpkg"
                run_custom_command "vcpkg_bootstrap" "$__GlobalVcpkgRoot/bootstrap-vcpkg.sh" "-disableMetrics"

                if [ $? -ne 0 ]; then
                    _SkipNative=1
                fi
            fi

        fi
    fi

    if [ -z "$_SkipNative" ]; then
        __VcpkgOS=$(echo "$_OS" | tr '[:upper:]' '[:lower:]')
        if [ "$__VcpkgOS" == "windows_nt" ]; then __VcpkgOS="windows"; fi
        if [ "$__VcpkgOS" == "osx" ]; then __VcpkgOS="osx"; fi
        if [ "$__VcpkgOS" == "macos" ]; then __VcpkgOS="osx"; fi
        __VcpkgTriplet="${_MSB_Platform}-${__VcpkgOS}"

        echo "BUILD: Building native libdjvulibre via Autotools ($__VcpkgTriplet)"
        if [ -n "$WSL_DISTRO_NAME" ] || [ -n "$WSL_INTEROP" ]; then
            echo "BUILD: Diagnostic - Original PATH: $PATH"
            export PATH=$(echo "$PATH" | tr ':' '\n' | grep -v -iE '^/mnt/[a-z]($|/)' | paste -sd ':' -)
            echo "BUILD: Diagnostic - Filtered PATH: $PATH"
        fi
        export PKG_CONFIG=$(command -v pkg-config)
        echo "BUILD: Diagnostic - PKG_CONFIG resolved to: $PKG_CONFIG"
        export PKG_CONFIG_PATH="/usr/lib/pkgconfig:/usr/share/pkgconfig:/usr/lib/x86_64-linux-gnu/pkgconfig:${PKG_CONFIG_PATH:-}"
        run_custom_command "vcpkg_install" "$__GlobalVcpkgRoot/vcpkg" "install" "--x-manifest-root=$__ProjectRoot/$__DjvuLibreDir" "--triplet" "$__VcpkgTriplet"

        if [ $? -eq 0 ]; then
            cd "$__ProjectRoot/$__DjvuLibreDir" || exit 1

            cp_flags="-I$__ProjectRoot/$__DjvuLibreDir/vcpkg_installed/$__VcpkgTriplet/include"
            ld_flags="-L$__ProjectRoot/$__DjvuLibreDir/vcpkg_installed/$__VcpkgTriplet/lib"
            pkg_cfg="$__ProjectRoot/$__DjvuLibreDir/vcpkg_installed/$__VcpkgTriplet/lib/pkgconfig"

            __ConfigureArgs=()
            if [ "$_MSB_Platform" != "$__HostArch" ]; then
                gnu_arch="$_MSB_Platform"
                if [ "$_MSB_Platform" == "arm64" ]; then
                    gnu_arch="aarch64"
                elif [ "$_MSB_Platform" == "arm" ]; then
                    gnu_arch="arm"
                elif [ "$_MSB_Platform" == "x86" ]; then
                    gnu_arch="i686"
                fi

                gnu_os="$__VcpkgOS"
                gnu_host="${gnu_arch}-${gnu_os}-${__Libc}"

                if [ "$gnu_host" == "arm-linux-gnu" ]; then
                    gnu_host="arm-linux-gnueabihf"
                fi

                __ConfigureArgs+=("--host=$gnu_host")
                echo "BUILD: Cross-compiling detected. GNU Host Triplet: $gnu_host"
            fi

            _last_triplet=""
            if [ -f ".lastbuildtriplet" ]; then
                _last_triplet=$(cat .lastbuildtriplet)
            fi

            _MSB_TargetLower=$(echo "$_MSB_Target" | tr '[:upper:]' '[:lower:]')

            if [ "$_last_triplet" != "$__VcpkgTriplet" ] || [ "$_MSB_TargetLower" == "rebuild" ] || [ "$_MSB_TargetLower" == "clean" ]; then
                if [ -f "Makefile" ]; then
                    run_custom_command "djvulibre_clean" "make" "clean"
                fi
                echo "$__VcpkgTriplet" > .lastbuildtriplet
            fi

            export NOCONFIGURE=1
            run_custom_command "djvulibre_autogen" "./autogen.sh"
            unset NOCONFIGURE
            if [ $? -eq 0 ]; then
                run_custom_command "djvulibre_configure" "./configure" "${__ConfigureArgs[@]}" "CPPFLAGS=$cp_flags" "LDFLAGS=$ld_flags" "PKG_CONFIG_PATH=$pkg_cfg"
                if [ $? -eq 0 ]; then
                    run_custom_command "djvulibre_make" "make" "-j${_Processors:-1}"
                    if [ $? -eq 0 ]; then
                        mkdir -p "$__OutputDir"
                        __ParentPublishDir="${__OutputDir}${__RuntimeIdentifier}"
                        mkdir -p "$__ParentPublishDir"
                        if [ "$__VcpkgOS" == "osx" ]; then
                            cp -Pf libdjvu/.libs/libdjvulibre*.dylib "$__OutputDir"
                            ln -sf "../libdjvulibre.dylib" "$__ParentPublishDir/libdjvulibre.dylib"
                        else
                            cp -Pf libdjvu/.libs/libdjvulibre.so* "$__OutputDir"
                            ln -sf "../libdjvulibre.so" "$__ParentPublishDir/libdjvulibre.so"
                        fi
                    fi
                fi
            fi
            cd "$__ProjectRoot" || exit 1
        fi

        _NativeFailed=0
        for cmd in "${__FailedCommands[@]}"; do
            case "$cmd" in
                vcpkg_bootstrap|vcpkg_install|djvulibre_clean|djvulibre_autogen|djvulibre_configure|djvulibre_make)
                    _NativeFailed=1
                    ;;
            esac
        done

        if [ "$_NativeFailed" == "1" ]; then
            __FailedBuilds+=("libdjvulibre")
        else
            __SuccessfulBuilds+=("libdjvulibre")
        fi

        if [ ${#__FailedCommands[@]} -gt 0 ] || [ ${#__FailedClones[@]} -gt 0 ]; then
            _SkipNative=1
        fi
    fi
fi

if [ -n "$_BuildDjvuNet" ]; then
    # Build core projects
    restore_dotnet_proj "$__SystemAttrProj" "System.Attributes.csproj"
    
    if [[ -n "$_BuildTools" ]]; then
        restore_dotnet_proj "$__LibGit2SharpProj" "LibGit2Sharp.csproj"
        restore_dotnet_proj "$__DjvuNetGitTasksProj" "DjvuNet.Build.Tasks.csproj"
    fi
    
    restore_dotnet_proj "$__DjvuNetProj" "DjvuNet.csproj"
    if [ -z "$_SkipNative" ]; then
        restore_dotnet_proj "$__DjvuNetDjvuLibreProj" "DjvuNet.DjvuLibre.csproj"
    elif [ "$_NativeFailed" == "1" ]; then
        __FailedRestores+=("DjvuNet.DjvuLibre.csproj")
    fi

    build_dotnet_proj "$__SystemAttrProj" "System.Attributes.csproj"
    
    if [[ -n "$_BuildTools" ]]; then
        build_dotnet_proj "$__LibGit2SharpProj" "LibGit2Sharp.csproj"
        build_dotnet_proj "$__DjvuNetGitTasksProj" "DjvuNet.Build.Tasks.csproj"
        
        # Only package tools if DjvuNet.Build.Tasks.csproj succeeded both Build and Publish phases
        local __TasksBuildFailed=0
        for failed in "${__FailedBuilds[@]}"; do
            if [[ "$failed" == "DjvuNet.Build.Tasks.csproj" ]]; then __TasksBuildFailed=1; fi
        done
        local __TasksPublishFailed=0
        for failed in "${__FailedPublishes[@]}"; do
            if [[ "$failed" == "DjvuNet.Build.Tasks.csproj" ]]; then __TasksPublishFailed=1; fi
        done

        if [[ "$__TasksBuildFailed" == "0" && "$__TasksPublishFailed" == "0" ]]; then
            run_custom_command "PackageTools.ps1" "pwsh" "-NoProfile" "-ExecutionPolicy" "Bypass" "-File" "${__ProjectRoot}/eng/scripts/PackageTools.ps1" "-RepoRoot" "${__ProjectRoot}"
        fi
    fi
    
    build_dotnet_proj "$__DjvuNetProj" "DjvuNet.csproj"
    if [ -z "$_SkipNative" ]; then
        build_dotnet_proj "$__DjvuNetDjvuLibreProj" "DjvuNet.DjvuLibre.csproj"
    elif [ "$_NativeFailed" == "1" ]; then
        __FailedBuilds+=("DjvuNet.DjvuLibre.csproj")
    fi
fi

if [ -n "$_BuildTests" ]; then
    # Clone test data
    if [ ! -f "./artifacts/test001C.djvu" ]; then
        echo ""
        echo "BUILD: Downloading release archive of artifacts for tag ${__ArtifactsReleaseTag}"
        download_retry "${__ArtifactsTestDataUri}" "artifacts.tar.gz"
        if [ $? -eq 0 ]; then
            rm -rf artifacts
            mkdir artifacts
            tar -xzf artifacts.tar.gz -C artifacts --strip-components=1
            rm artifacts.tar.gz
        fi
    fi

    # Build test projects
    __DjvuNetTestsProj="DjvuNet.Tests/DjvuNet.Tests.csproj"
    __DjvuNetWaveletTestsProj="DjvuNet.Wavelet.Tests/DjvuNet.Wavelet.Tests.csproj"
    __DjvuNetTestExeProj="DjvuNetTest/DjvuNetTest.csproj"
    __DjvuNetDjvuLibreTestsProj="DjvuNet.DjvuLibre.Tests/DjvuNet.DjvuLibre.Tests.csproj"

    restore_dotnet_proj "$__DjvuNetTestsProj" "DjvuNet.Tests.csproj"
    restore_dotnet_proj "$__DjvuNetWaveletTestsProj" "DjvuNet.Wavelet.Tests.csproj"
    restore_dotnet_proj "$__DjvuNetTestExeProj" "DjvuNetTest.csproj"
    if [ -z "$_SkipNative" ]; then
        restore_dotnet_proj "$__DjvuNetDjvuLibreTestsProj" "DjvuNet.DjvuLibre.Tests.csproj"
    elif [ "$_NativeFailed" == "1" ]; then
        __FailedRestores+=("DjvuNet.DjvuLibre.Tests.csproj")
    fi

    build_dotnet_proj "$__DjvuNetTestsProj" "DjvuNet.Tests.csproj"
    build_dotnet_proj "$__DjvuNetWaveletTestsProj" "DjvuNet.Wavelet.Tests.csproj"
    build_dotnet_proj "$__DjvuNetTestExeProj" "DjvuNetTest.csproj"
    if [ -z "$_SkipNative" ]; then
        build_dotnet_proj "$__DjvuNetDjvuLibreTestsProj" "DjvuNet.DjvuLibre.Tests.csproj"
    elif [ "$_NativeFailed" == "1" ]; then
        __FailedBuilds+=("DjvuNet.DjvuLibre.Tests.csproj")
    fi
fi

print_build_summary() {
    echo ""
    echo "BUILD: ======================================================================"
    echo "BUILD:                            BUILD SUMMARY"
    echo "BUILD: ======================================================================"
    if [ ${#__SuccessfulClones[@]} -ne 0 ]; then
        echo "BUILD: Successfully cloned:"
        for p in "${__SuccessfulClones[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__SuccessfulCommands[@]} -ne 0 ]; then
        echo "BUILD: Successfully executed commands:"
        for p in "${__SuccessfulCommands[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__SuccessfulRestores[@]} -ne 0 ]; then
        echo "BUILD: Successfully restored:"
        for p in "${__SuccessfulRestores[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__SuccessfulBuilds[@]} -ne 0 ]; then
        echo "BUILD: Successfully built:"
        for p in "${__SuccessfulBuilds[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__SuccessfulPublishes[@]} -ne 0 ]; then
        echo "BUILD: Successfully published:"
        for p in "${__SuccessfulPublishes[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__FailedClones[@]} -ne 0 ]; then
        echo "BUILD: Failed to clone:"
        for p in "${__FailedClones[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__FailedCommands[@]} -ne 0 ]; then
        echo "BUILD: Failed to execute commands:"
        for p in "${__FailedCommands[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__FailedRestores[@]} -ne 0 ]; then
        echo "BUILD: Failed to restore:"
        for p in "${__FailedRestores[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__FailedBuilds[@]} -ne 0 ]; then
        echo "BUILD: Failed to build:"
        for p in "${__FailedBuilds[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__FailedPublishes[@]} -ne 0 ]; then
        echo "BUILD: Failed to publish:"
        for p in "${__FailedPublishes[@]}"; do echo "BUILD:   - $p"; done
    fi
}

print_full_summary() {
    print_build_summary
    if [ ${#__SuccessfulTests[@]} -ne 0 ]; then
        echo "BUILD: Successfully tested:"
        for p in "${__SuccessfulTests[@]}"; do echo "BUILD:   - $p"; done
    fi
    if [ ${#__FailedTests[@]} -ne 0 ]; then
        echo "BUILD: Failed tests:"
        for p in "${__FailedTests[@]}"; do echo "BUILD:   - $p"; done
    fi
    echo ""
}

if [ -n "$_RunTests" ]; then
    print_build_summary
    echo ""
    echo "BUILD: ======================================================================"
    echo "BUILD:                            STARTING TESTS"
    echo "BUILD: ======================================================================"
    echo ""

    # Run tests
    __TestOutputDir="$__PublishDir"
    _DjvuNet_Tests="${__TestOutputDir}DjvuNet.Tests.dll"
    _DjvuNet_DjvuLibre_Tests="${__TestOutputDir}DjvuNet.DjvuLibre.Tests.dll"
    _DjvuNet_Wavelet_Tests="${__TestOutputDir}DjvuNet.Wavelet.Tests.dll"

    __TestResOutputDir="TestResults/${_Framework}/"
    mkdir -p "$__TestResOutputDir"

    if [[ "$_Framework" == "$_DefaultNetCoreApp" ]]; then
        _Test_Options="-trait- Category=skip-netcoreapp"
        __TestOutputFormat="xml"
    elif [[ "$_Framework" == "$_DefaultNetStandard" ]]; then
        _Test_Options="-trait- Category=skip-netcoreapp"
        __TestOutputFormat="xml"
    fi

    _VerbosityLower=$(echo "$_Verbosity" | tr '[:upper:]' '[:lower:]')
    if [[ "$_VerbosityLower" == "d" || "$_VerbosityLower" == "detailed" ]]; then
        _Test_Options="$_Test_Options -verbose"
    fi
    if [[ "$_VerbosityLower" == "diag" || "$_VerbosityLower" == "diagnostic" ]]; then
        _Test_Options="$_Test_Options -verbose -internaldiagnostics"
    fi
    _Test_Options="$_Test_Options -trait- Category=Skip -nologo -nocolor"

    _DjvuNet_Tests_Error="false"

    run_dotnet_test "$_DjvuNet_Tests" "DjvuNet.Tests"

    if [ -z "$_SkipNative" ] && [ -z "$__SkipNativeTests" ]; then
        run_dotnet_test "$_DjvuNet_DjvuLibre_Tests" "DjvuNet.DjvuLibre.Tests"
    fi

    run_dotnet_test "$_DjvuNet_Wavelet_Tests" "DjvuNet.Wavelet.Tests"
fi

if [ ${#__FailedRestores[@]} -ne 0 ] || [ ${#__FailedBuilds[@]} -ne 0 ] || [ ${#__FailedPublishes[@]} -ne 0 ] || [ ${#__FailedTests[@]} -ne 0 ] || [ ${#__FailedClones[@]} -ne 0 ] || [ ${#__FailedCommands[@]} -ne 0 ]; then
    echo ""
    echo "BUILD: Error: Build Failed at $(date +"%Y-%m-%d %H:%M:%S.%2N")"
    print_full_summary
    exit 1
fi

echo ""
echo "BUILD: Success: Build and tests passed at $(date +"%Y-%m-%d %H:%M:%S.%2N")"
print_full_summary
exit 0
