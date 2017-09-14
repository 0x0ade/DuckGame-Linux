#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DuckGame {
    internal static class patch_Program {

        public static void LogLine(string line) {
            Console.WriteLine(line);
        }

        public static void WriteToLog(string s) {
            Console.Error.WriteLine(s);
        }

        private static extern void orig_UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e);
        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {
            LogDetailed((Exception) e.ExceptionObject);
            orig_UnhandledExceptionTrapper(sender, e);
        }

        public static void LogDetailed(Exception e, string tag = null) {
            for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                Console.WriteLine(e_.GetType().FullName + ": " + e_.Message + "\n" + e_.StackTrace);
                if (e_ is ReflectionTypeLoadException) {
                    ReflectionTypeLoadException rtle = (ReflectionTypeLoadException) e_;
                    for (int i = 0; i < rtle.Types.Length; i++) {
                        Console.WriteLine("ReflectionTypeLoadException.Types[" + i + "]: " + rtle.Types[i]);
                    }
                    for (int i = 0; i < rtle.LoaderExceptions.Length; i++) {
                        LogDetailed(rtle.LoaderExceptions[i], tag + (tag == null ? "" : ", ") + "rtle:" + i);
                    }
                }
                if (e_ is TypeLoadException) {
                    Console.WriteLine("TypeLoadException.TypeName: " + ((TypeLoadException) e_).TypeName);
                }
                if (e_ is BadImageFormatException) {
                    Console.WriteLine("BadImageFormatException.FileName: " + ((BadImageFormatException) e_).FileName);
                }
            }
        }

        public static void MakeNetLog() {
        }

        [MonoModLinkTo("DuckGame.ModLoader", "System.Void set_modHash(System.String)")]
        private static void _ModLoader_set_modHash(string value) {
        }

        public static extern void orig_Main(string[] args);
        [STAThread]
        public static void Main(string[] args) {
            bool nothreading = true;
            bool nomods = true;

            Queue<string> argq = new Queue<string>(args);
            while (argq.Count > 0) {
                string arg = argq.Dequeue();
                if (arg == "-debug")
                    Debugger.Launch();
                else if (arg == "-yesthreading")
                    nothreading = false;
                else if (arg == "-yesmods")
                    nomods = false;
            }

            List<string> argl = new List<string>(args);
            argl.Add("<");

            if (nothreading)
                argl.Add("-nothreading");

            if (nomods)
                argl.Add("-nomods");

            argl.Add(">");

            args = argl.ToArray();

            // Parse arguments again...
            argq = new Queue<string>(args);
            while (argq.Count > 0) {
                string arg = argq.Dequeue();
                if (arg == "-nomods")
                    // MonoMain.moddingEnabled = false skips ManagedContent.InitializeMods.
                    // InitializeMods calls ModLoader.LoadMods.
                    // LoadMods sets ModLoader.modHash.
                    // -nomods thus leaves ModLoader.modHash == null, which causes issues.
                    patch_ModLoader.DefaultModHash();
            }

            orig_Main(args);
        }

    }
}
