#!/usr/bin/env bash

# Uncommment set -x to trace execution of the script
# set -x

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [-verbose] [-coverage] [-cross] [-clangx.y] [-ninja] [-skipnative] [-skiptests] [-bindir]"
    echo "BuildArch can be: -x64, -x86, -arm, -armel, -arm64"
    echo "BuildType can be: -debug, -release"
    echo "-clangx.y - optional argument to build using clang version x.y."
    echo "-cross - optional argument to signify cross compilation,"
    echo "       - will use ROOTFS_DIR environment variable if set."
    echo "-skipnative - do not build native components."
    echo "-skiptests - skip the tests in the 'tests' subdirectory."
    echo "-skipcrossgen - skip native image generation"
    echo "-verbose - optional argument to enable verbose build output."
    echo "-skiprestore: skip restoring packages ^(default: packages are restored during build^)."
    echo "-Rebuild: passes /t:rebuild to the build projects."
    echo "-skipgenerateversion - disable version generation even if MSBuild is supported."
    echo "-ignorewarnings - do not treat warnings as errors - default true"
    echo "-bindir - output directory (defaults to $__ProjectRoot/bin)"
    echo "-numproc - set the number of build processes."
    exit 1
}

initHostDistroRid()
{
    __HostDistroRid=""
    if [ "$__HostOS" == "Linux" ]; then
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
    if [ "$__HostOS" == "FreeBSD" ]; then
        __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'.'`
        __HostDistroRid="freebsd.$__freebsd_version-$__HostArch"
    fi

    if [ "$__HostDistroRid" == "" ]; then
        echo "WARNING: Can not determine runtime id for current distro."
    fi
}

initTargetDistroRid()
{
    if [ $__CrossBuild == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
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
        if [ "$__BuildOS" == "Linux" ]; then
            export __DistroRid="linux-$__BuildArch"
        elif [ "$__BuildOS" == "OSX" ]; then
            export __DistroRid="osx-$__BuildArch"
        elif [ "$__BuildOS" == "FreeBSD" ]; then
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

    if [ $__CrossBuild == 1 ]; then
        mkdir -p "$__CrossComponentBinDir"
        mkdir -p "$__CrossCompIntermediatesDir"
    fi
}

# Check the system to ensure the right prereqs are in place

check_prereqs()
{
    echo "Checking prerequisites..."

    success=1

    # Check presence of dotnet sdk at least 2.0
    hash dotnet 2>/dev/null
    if ! [[ $? -eq 0 ]]; then
        echo "dotnet not installed"; 
        success=0;
    else
        __DotnetVer=$(dotnet --version)
        printf -v int '%d\n' $__DotnetVer 2>/dev/null
        if [[ $int -ge 2 ]]; then
            echo "dotnet $__DotnetVer installed"
            __MSBuildVer=$(dotnet msbuild /nologo /version)
            if [[ -z $__MSBuildVer ]]; then
                echo "dotnet sdk not installed $__MSBuildVer"
                success=0
            else
                versionLines=$(echo $__MSBuildVer | tr "." "\n")
                index=0
                for d in $versionLines
                do
                    verDigits[$index]=$d
                    index=$(( $index + 1 ))
                done

                if [[ ( ${verDigits[0]} -eq 15 && ${verDigits[1]} -ge 3 ) || ${verDigits[0]} -gt 15 ]]; then
                    echo "msbuild $__MSBuildVer installed"
                else
                    echo "Please install newer dotnet sdk. Current MSBuild version $__MSBuildVer."
                    success=0
                fi
            fi
        else
            echo "Please install dotnet sdk version 2.0.0 or newer before running this script."
            echo "Current dotnet version: $__DotnetVer"
            success=0
        fi
    fi  
    
    # Check presence of git on the path
    hash git 2>/dev/null
    if ! [[ $? -eq 0 ]]; then
        echo "git not installed"; 
        success=0;
    else
        echo "git installed"
    fi 

    # Check presence of unzip on the path
     hash unzip 2>/dev/null
     if ! [[ $? -eq 0 ]]; then
        echo >&2 "unzip not installed"; 
        success=0;
    else
        echo "unzip installed"
    fi

    # Check presence of libgdiplus
    ldconfig -p | grep libgdiplus >/dev/null
    if [[ $? -ne 0 ]]; then 
        echo "libgdiplus not installed"; 
        success=0;
    else
        echo "libgdiplus installed"
    fi

    # Minimum required version of clang is version 3.9 for arm/armel cross build
    if [[ $__CrossBuild == 1 && ("$__BuildArch" == "arm" || "$__BuildArch" == "armel") ]]; then
        if ! [[ "$__ClangMajorVersion" -gt "3" || ( $__ClangMajorVersion == 3 && $__ClangMinorVersion == 9 ) ]]; then
            echo "Please install clang3.9 or newer for arm/armel cross build"; 
            success=0;
        fi
    fi

    # Check for clang
    hash clang-$__ClangMajorVersion.$__ClangMinorVersion 2>/dev/null
    if ! [[ $? -eq 0 ]]; then
        hash clang$__ClangMajorVersion$__ClangMinorVersion 2>/dev/null
        if ! [[ $? -eq 0 ]]; then
            hash clang 2>/dev/null
            if ! [[ $? -eq 0 ]]; then 
                echo >&2 "Please install clang-$__ClangMajorVersion.$__ClangMinorVersion before running this script"; 
                success=0; 
            fi
        fi
    fi

    if [ $success -eq 0 ]; then exit 1; fi 
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

    if [ $__SkipConfigure == 0 ]; then
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
    __isMSBuildOnNETCoreSupported=$__msbuildonunsupportedplatform

    if [ $__isMSBuildOnNETCoreSupported == 1 ]; then
        return
    fi

    if [ "$__HostArch" == "x64" ]; then
        if [ "$__HostOS" == "Linux" ]; then
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
        elif [ "$__HostOS" == "OSX" ]; then
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

        if [ "$__BuildOS" == "Linux" ]; then
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

echo ; echo "Commencing DjvuNet Repo build"; date; echo "";

# Argument types supported by this script:
#
# Build platform        - valid values are: x64, x86, arm, armel, arm64.
# Build configuration   - valid values are: Debug, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Use uname to determine what the CPU is.
CPUName=$(uname -p)
# Some Linux platforms report unknown for platform, but the arch for machine.
if [ "$CPUName" == "unknown" ]; then
    CPUName=$(uname -m)
fi

case $CPUName in
    i686)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=x86
        __HostArch=x86
        ;;

    x86_64)
        __BuildArch=x64
        __HostArch=x64
        ;;

    armv7l)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=arm
        __HostArch=arm
        ;;

    aarch64)
        __BuildArch=arm64
        __HostArch=arm64
        ;;

    amd64)
        __BuildArch=x64
        __HostArch=x64
        ;;
    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __BuildArch=x64
        __HostArch=x64
        ;;
esac

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        __HostOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        __HostOS=Linux
        ;;
esac

__BuildType=Debug
__CodeCoverage=
__IncludeTests=Include_Tests
__IgnoreWarnings=0

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
__SourceDir="$__ProjectDir/src"
__PackagesDir="$__ProjectDir/packages"
__RootBinDir="$__ProjectDir/bin"
__UnprocessedBuildArgs=
__RunArgs=
__MSBCleanBuildArgs=
__UseNinja=0
__VerboseBuild=0
__PgoInstrument=0
__PgoOptimize=1
__IbcTuning=""
__ConfigureOnly=0
__SkipConfigure=0
__SkipRestore=""
__SkipNuget=0
__SkipCoreCLR=0
__SkipMSCorLib=0
__SkipRestoreOptData=0
__SkipCrossgen=0
__CrossBuild=0
__ClangMajorVersion=0
__ClangMinorVersion=0
__NuGetPath="$__PackagesDir/NuGet.exe"
__HostDistroRid=""
__DistroRid=""
__cmakeargs=""
__SkipGenerateVersion=0
__DoCrossArchBuild=0
__PortableBuild=1
__msbuildonunsupportedplatform=0
__PgoOptDataVersion=""
__IbcOptDataVersion=""

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
if [ `uname` = "FreeBSD" ]; then
  __NumProc=`sysctl hw.ncpu | awk '{ print $2+1 }'`
elif [ `uname` = "NetBSD" ]; then
  __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
elif [ `uname` = "Darwin" ]; then
  __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
else
  __NumProc=$(nproc --all)
fi

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        x86|-x86)
            __BuildArch=x86
            ;;

        x64|-x64)
            __BuildArch=x64
            ;;

        arm|-arm)
            __BuildArch=arm
            ;;

        armel|-armel)
            __BuildArch=armel
            ;;

        arm64|-arm64)
            __BuildArch=arm64
            ;;

        debug|-debug)
            __BuildType=Debug
            ;;

        checked|-checked)
            __BuildType=Checked
            ;;

        release|-release)
            __BuildType=Release
            ;;

        coverage|-coverage)
            __CodeCoverage=Coverage
            ;;

        cross|-cross)
            __CrossBuild=1
            ;;

        -portablebuild=false)
            __PortableBuild=0
            ;;

        verbose|-verbose)
            __VerboseBuild=1
            ;;

        stripsymbols|-stripsymbols)
            __cmakeargs="$__cmakeargs -DSTRIP_SYMBOLS=true"
            ;;

        clang3.5|-clang3.5)
            __ClangMajorVersion=3
            __ClangMinorVersion=5
            ;;

        clang3.6|-clang3.6)
            __ClangMajorVersion=3
            __ClangMinorVersion=6
            ;;

        clang3.7|-clang3.7)
            __ClangMajorVersion=3
            __ClangMinorVersion=7
            ;;

        clang3.8|-clang3.8)
            __ClangMajorVersion=3
            __ClangMinorVersion=8
            ;;

        clang3.9|-clang3.9)
            __ClangMajorVersion=3
            __ClangMinorVersion=9
            ;;

        clang4.0|-clang4.0)
            __ClangMajorVersion=4
            __ClangMinorVersion=0
            ;;

        clang5.0|-clang5.0)
            __ClangMajorVersion=5
            __ClangMinorVersion=0
            ;;        

        ninja|-ninja)
            __UseNinja=1
            ;;

        pgoinstrument|-pgoinstrument)
            __PgoInstrument=1
            ;;

        nopgooptimize|-nopgooptimize)
            __PgoOptimize=0
            __SkipRestoreOptData=1
            ;;

        ibcinstrument|-ibcinstrument)
            __IbcTuning="/Tuning"
            ;;

        configureonly|-configureonly)
            __ConfigureOnly=1
            __SkipMSCorLib=1
            __SkipNuget=1
            ;;

        skipconfigure|-skipconfigure)
            __SkipConfigure=1
            ;;

        skipnative|-skipnative)
            # Use "skipnative" to use the same option name as build.cmd.
            __SkipCoreCLR=1
            ;;

        skipcoreclr|-skipcoreclr)
            # Accept "skipcoreclr" for backwards-compatibility.
            __SkipCoreCLR=1
            ;;

        crosscomponent|-crosscomponent)
            __DoCrossArchBuild=1
            ;;

        skipmscorlib|-skipmscorlib)
            __SkipMSCorLib=1
            ;;

        skipgenerateversion|-skipgenerateversion)
            __SkipGenerateVersion=1
            ;;

        skiprestoreoptdata|-skiprestoreoptdata)
            __SkipRestoreOptData=1
            ;;

        skipcrossgen|-skipcrossgen)
            __SkipCrossgen=1
            ;;

        includetests|-includetests)
            ;;

        skiptests|-skiptests)
            __IncludeTests=
            ;;

        skipnuget|-skipnuget)
            __SkipNuget=1
            ;;

        ignorewarnings|-ignorewarnings)
            __IgnoreWarnings=1
            __cmakeargs="$__cmakeargs -DCLR_CMAKE_WARNINGS_ARE_ERRORS=OFF"
            ;;

        cmakeargs|-cmakeargs)
            if [ -n "$2" ]; then
                __cmakeargs="$__cmakeargs $2"
                shift
            else
                echo "ERROR: 'cmakeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;

        bindir|-bindir)
            if [ -n "$2" ]; then
                __RootBinDir="$2"
                if [ ! -d $__RootBinDir ]; then
                    mkdir $__RootBinDir
                fi
                __RootBinParent=$(dirname $__RootBinDir)
                __RootBinName=${__RootBinDir##*/}
                __RootBinDir="$(cd $__RootBinParent &>/dev/null && printf %s/%s $PWD $__RootBinName)"
                shift
            else
                echo "ERROR: 'bindir' requires a non-empty option argument"
                exit 1
            fi
            ;;
        buildstandalonegc|-buildstandalonegc)
            __cmakeargs="$__cmakeargs -DFEATURE_STANDALONE_GC=1 -DFEATURE_STANDALONE_GC_ONLY=1"
            ;;
        msbuildonunsupportedplatform|-msbuildonunsupportedplatform)
            __msbuildonunsupportedplatform=1
            ;;
        numproc|-numproc)
            if [ -n "$2" ]; then
              __NumProc="$2"
              shift
            else
              echo "ERROR: 'numproc' requires a non-empty option argument"
              exit 1
            fi
            ;;
        osgroup|-osgroup)
            if [ -n "$2" ]; then
              __BuildOS="$2"
              shift
            else
              echo "ERROR: 'osgroup' requires a non-empty option argument"
              exit 1
            fi
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

__RunArgs="-BuildArch=$__BuildArch -BuildType=$__BuildType -BuildOS=$__BuildOS"

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
	__RunArgs="$__RunArgs -verbose"
fi

# Set default clang version
if [[ $__ClangMajorVersion == 0 && $__ClangMinorVersion == 0 ]]; then
    __ClangMajorVersion=3
    __ClangMinorVersion=9
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
if [ $__CrossBuild == 1 ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__ProjectRoot/cross/rootfs/$__BuildArch"
    fi
fi

# Check prereqs.
check_prereqs


# Build the libdjvulibre (native) components.
# build_native 


# Build cross-architecture components
if [[ $__CrossBuild == 1 && $__DoCrossArchBuild == 1 ]]; then
    build_cross_arch_component
fi

# Build complete

echo "Repo successfully built."
exit 0

