#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuckGame {
    class patch_DuckFile {

        [MonoModIgnore]
        private static string _saveRoot;
        [MonoModIgnore]
        private static string _saveDirectory;
        [MonoModIgnore]
        private static List<string> _allPaths;

        public static extern void orig_Initialize();
        public static void Initialize() {
            orig_Initialize();
            string prefix = _saveRoot + _saveDirectory;
            foreach (string path in _allPaths) {
                if (path.StartsWith(prefix))
                    Directory.CreateDirectory(path);
                else
                    Directory.CreateDirectory(prefix + path);
            }
        }

    }
}
