namespace PokeFilterBot.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    public static class AssemblyUtils
    {
        public static string AssemblyName
        {
            get { return GetVersionInfo(AssemblyPath).ProductName; }
        }

        public static string AssemblyVersion
        {
            get { return GetVersionInfo(AssemblyPath).ProductVersion; }
        }

        public static string CompanyName
        {
            get { return GetVersionInfo(AssemblyPath).CompanyName; }
        }

        public static string Copyright
        {
            get { return GetVersionInfo(AssemblyPath).LegalCopyright; }
        }

        public static string AssemblyPath
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        private static FileVersionInfo GetVersionInfo(string filePath)
        {
            return FileVersionInfo.GetVersionInfo(filePath);
        }
    }
}