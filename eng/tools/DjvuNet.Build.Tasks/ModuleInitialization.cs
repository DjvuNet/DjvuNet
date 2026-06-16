using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using System.Runtime.CompilerServices;

#if !NETCOREAPP
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
#endif

namespace DjvuNet.Build.Tasks
{
    public static class Init
    {
        // 0 = false, 1 = true
#pragma warning disable CS0414
        private static int _initialized = 0;
#pragma warning restore CS0414

        private static bool _resolverRegistered = false;
        private static List<(bool isWarning, string message)> _deferredLogs = new List<(bool, string)>();

#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            Initialize(null);
        }

        public static void Initialize(IBuildEngine loggingEngine)
        {
            // Drain deferred logs if we now have an engine
            if (loggingEngine != null)
            {
                lock (_deferredLogs)
                {
                    foreach (var log in _deferredLogs)
                    {
                        if (log.isWarning)
                        {
                            loggingEngine.LogWarningEvent(new BuildWarningEventArgs(
                                subcategory: "DjvuNetTaskFactory", code: "DTF02", file: "", lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                                message: log.message, helpKeyword: "Help", senderName: "DjvuNetTaskFactory"));
                        }
                        else
                        {
                            loggingEngine.LogMessageEvent(new BuildMessageEventArgs(
                                log.message, "Help", "DjvuNetTaskFactory", MessageImportance.Low));
                        }
                    }
                    _deferredLogs.Clear();
                }
            }

            if (_resolverRegistered) return;

#if NETCOREAPP
            try
            {
                NativeLibrary.SetDllImportResolver(typeof(LibGit2Sharp.Repository).Assembly, (libraryName, assembly, searchPath) =>
                {
                    // LibGit2Sharp usually requests "git2-3f4182d"
                    if (libraryName.Contains("git2"))
                    {
                        string rid = GetRuntimeIdentifier();
                        string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" :
                                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib" : ".so";

                        // Ensure libraryName doesn't already have extension and has correct prefix
                        string fileName = libraryName;
                        if (!fileName.EndsWith(extension)) fileName += extension;

                        // Linux/macOS usually need the 'lib' prefix if not present
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !fileName.StartsWith("lib"))
                            fileName = "lib" + fileName;

                        string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                        string nativePath = Path.Combine(assemblyDir, "runtimes", rid, "native", fileName);

                        if (File.Exists(nativePath))
                        {
                            if (File.Exists(nativePath))
                            {
                                IntPtr handle = IntPtr.Zero;
                                try
                                {
                                    handle = NativeLibrary.Load(nativePath);
                                    if (handle != IntPtr.Zero)
                                    {
                                        // Atomically check and set the initialized flag
                                        if (Interlocked.Exchange(ref _initialized, 1) == 0)
                                        {
                                            TaskLogger.Current?.LogMessage(MessageImportance.Low, $"DjvuNet.Build.Tasks : message : Successfully loaded native library {fileName}.");
                                        }
                                        else
                                        {
                                            TaskLogger.Current?.LogMessage(MessageImportance.Low, $"DjvuNet.Build.Tasks : message : Superfluous load attempt for native library {fileName}. Accepting anyway.");
                                        }
                                    }
                                    else
                                    {
                                        TaskLogger.Current?.LogWarning($"DjvuNet.Build.Tasks : warning : Failed to load native library {fileName} from path {nativePath}.");
                                    }
                                }
                                catch(DllNotFoundException dnfe)
                                {
                                    TaskLogger.Current?.LogError($"DjvuNet.Build.Tasks : error : DllNotFoundException occurred while loading native library {fileName} from path {nativePath}. {dnfe.Message}");
                                }
                                catch (BadImageFormatException bife)
                                {
                                    TaskLogger.Current?.LogError($"DjvuNet.Build.Tasks : error : BadImageFormatException occurred while loading native library {fileName} from path {nativePath}. {bife.Message}");
                                }
                                catch (Exception ex)
                                {
                                    TaskLogger.Current?.LogWarning($"DjvuNet.Build.Tasks : warning : Exception occurred while loading native library {fileName} from path {nativePath}. Exception: {ex}");
                                }
                                return handle;
                            }

                        }
                    }
                    return IntPtr.Zero;
                });

                _resolverRegistered = true;
                LogOrDefer(loggingEngine, false, "DjvuNet.Build.Tasks : message : Successfully registered NativeLibrary DllImportResolver for DjvuNet.Build.Tasks native dependencies.");
            }
            catch (InvalidOperationException)
            {
                LogOrDefer(loggingEngine, true, "DjvuNet.Build.Tasks : warning : Failed to set NativeLibrary DllImportResolver for DjvuNet.Build.Tasks native dependencies. Another resolver may already be registered.");
            }
#endif
        }

        private static void LogOrDefer(IBuildEngine loggingEngine, bool isWarning, string message)
        {
            if (loggingEngine != null)
            {
                if (isWarning)
                {
                    loggingEngine.LogWarningEvent(new BuildWarningEventArgs(
                        subcategory: "DjvuNetTaskFactory", code: "DTF02", file: "", lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                        message: message, helpKeyword: "Help", senderName: "DjvuNetTaskFactory"));
                }
                else
                {
                    loggingEngine.LogMessageEvent(new BuildMessageEventArgs(
                        message, "Help", "DjvuNetTaskFactory", MessageImportance.Low));
                }
            }
            else
            {
                lock (_deferredLogs)
                {
                    _deferredLogs.Add((isWarning, message));
                }
            }
        }


#if NETCOREAPP
        private static string GetRuntimeIdentifier()
        {
            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) ? "freebsd" : "unknown";

            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => RuntimeInformation.ProcessArchitecture.ToString().ToLower()
            };

            return $"{os}-{arch}";
        }
#endif
    }
}