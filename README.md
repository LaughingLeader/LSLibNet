## Work-in-Progress  

This is essentially a stripped-down fork of [lslib](https://github.com/Norbyte/lslib), with the native part replaced with C# libraries, and the applications removed, for use in [BG3ModManager](https://github.com/LaughingLeader/BG3ModManager)'s [Avalonia](https://github.com/AvaloniaUI/Avalonia/) port (or other projects).

The granny-related code was moved to a separate project (`LSGranny`), to allow compiling LSLib without it, but `LSGranny` won't compile currently without the previous LSLibNative.

Requirements
============

To build the tools you'll need to get the following dependencies:

 - Download GPLex 1.2.2 [from here](https://s3.eu-central-1.amazonaws.com/nb-stor/dos-legacy/ExportTool/gplex-distro-1_2_2.zip) and extract it to the `External\gplex\` directory
 - Download GPPG 1.5.2 [from here](https://s3.eu-central-1.amazonaws.com/nb-stor/dos-legacy/ExportTool/gppg-distro-1_5_2.zip) and extract it to the `External\gppg\` directory
 - Protocol Buffers 3.6.1 compiler [from here](https://github.com/protocolbuffers/protobuf/releases/download/v3.6.1/protoc-3.6.1-win32.zip) and extract it to the `External\protoc\` directory

## Links  

* [lslib](https://github.com/Norbyte/lslib)
