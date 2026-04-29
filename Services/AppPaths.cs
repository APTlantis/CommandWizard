using System;
using System.IO;

namespace CommandWizard.Services
{
    public static class AppPaths
    {
        public static string ResolveDataRoot()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root = Path.Combine(local, "Aptlantis", "CommandWizard");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
