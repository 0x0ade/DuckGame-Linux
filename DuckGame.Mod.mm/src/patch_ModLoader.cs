#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuckGame {
    class patch_ModLoader {

        [MonoModIgnore]
        internal static string modHash { get; private set; }

        internal static void DefaultModHash() {
            if (modHash == null)
                modHash = "nomods";
        }

    }
}
