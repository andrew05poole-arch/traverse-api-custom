using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TRAVERSE.Web.API
{
    internal sealed class ApiAssemblyHelper
    {
        #region Constructors
        private ApiAssemblyHelper()
        { }
        #endregion Constructors

        #region Methods
        public static Dictionary<string, AssemblyDetailInfo> LoadAssemblyInfo(Assembly assembly)
        {
            ApiAssemblyHelper helper = new ApiAssemblyHelper();
            return helper.RetrieveReferencedAssemblies(assembly);
        }

        private Dictionary<string, AssemblyDetailInfo> RetrieveReferencedAssemblies(Assembly assembly)
        {
            object obj = new object();

            lock (obj)
            {
                DateTime assemblyTime;
                Version version;

                _dependentAssemblyList = new Dictionary<string, AssemblyDetailInfo>();
                _missingAssemblyList = new List<MissingAssembly>();

                InternalGetDependentAssembliesRecursive(assembly);

                //Include assemblies that are not necessarily referenced
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (_dependentAssemblyList.ContainsKey(CleanName(asm.FullName)) ||
                        asm.IsDynamic ||
                        asm.GlobalAssemblyCache ||
                        ((version = asm.GetName().Version).Major == 0 && version.Minor == 0 && version.Revision == 0 && version.Build == 0) ||
                        (assemblyTime = RetrieveAssemblyBuildTime(asm)) == DateTime.MinValue)
                        continue;

                    _dependentAssemblyList[CleanName(asm.FullName)] = new AssemblyDetailInfo(CleanName(asm.FullName), VersionInfo(asm), RetrieveAssemblyBuildTime(asm).ToString("yyyy-MM-dd HH:mm:ss")); ;

                    InternalGetDependentAssembliesRecursive(asm);
                }

                return _dependentAssemblyList;
            }
        }

        private DateTime RetrieveAssemblyBuildTime(Assembly assembly)
        {
            var path = assembly.GetName().CodeBase;

            if (!File.Exists(path))
            {
                path = assembly.Location;
            }

            if (File.Exists(path))
            {
                var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    fileStream.Position = 0x3C;
                    fileStream.Read(buffer, 0, 4);
                    fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset
                    fileStream.Read(buffer, 0, 4); // "PE\0\0"
                    fileStream.Read(buffer, 0, buffer.Length);
                }
                var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));

                    return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(coffHeader.TimeDateStamp * TimeSpan.TicksPerSecond));
                }
                finally
                {
                    pinnedBuffer.Free();
                }
            }
            return new DateTime();
        }

        private string VersionInfo(Assembly assembly)
        {
            if (assembly == null)
                return string.Empty;

            return assembly.GetName().Version.ToString();
        }

        private void InternalGetDependentAssembliesRecursive(Assembly assembly)
        {
            // Load assemblies with newest versions first. Omitting the ordering results in false positives on
            // _missingAssemblyList.
            var referencedAssemblies = assembly.GetReferencedAssemblies().OrderByDescending(o => o.Version);
            DateTime assemblyTime;

            foreach (var r in referencedAssemblies)
            {
                if ((r.Version.Major == 0 && r.Version.Minor == 0 && r.Version.Revision == 0 && r.Version.Build == 0) || String.IsNullOrEmpty(assembly.FullName) || assembly.IsDynamic)
                {
                    continue;
                }

                if (!_dependentAssemblyList.ContainsKey(CleanName(r.FullName)))
                {
                    try
                    {
                        var a = Assembly.ReflectionOnlyLoad(r.FullName);

                        try
                        {
                            if (a.IsDynamic || a.GlobalAssemblyCache || (assemblyTime = RetrieveAssemblyBuildTime(a)) == DateTime.MinValue)
                                continue;

                            _dependentAssemblyList[CleanName(a.FullName)] = new AssemblyDetailInfo(CleanName(a.FullName), VersionInfo(a), assemblyTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            InternalGetDependentAssembliesRecursive(a);
                        }
                        catch
                        { }
                    }
                    catch (Exception)
                    {
                        _missingAssemblyList.Add(new MissingAssembly(r.FullName.Split(',')[0], CleanName(assembly.FullName)));
                    }
                }
            }
        }

        private string CleanName(string fullName)
        {
            return fullName.Split(',')[0];
        }
        #endregion Methods

        #region Fields
        private static Dictionary<string, AssemblyDetailInfo> _dependentAssemblyList;
        private static List<MissingAssembly> _missingAssemblyList;
        #endregion Fields

        #region Supporting Information
        struct _IMAGE_FILE_HEADER
        {
#pragma warning disable 0649
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
#pragma warning restore 0649
        };

        internal class MissingAssembly
        {
            public MissingAssembly(string missingAssemblyName, string missingAssemblyNameParent)
            {
                MissingAssemblyName = missingAssemblyName;
                MissingAssemblyNameParent = missingAssemblyNameParent;
            }

            public string MissingAssemblyName { get; set; }
            public string MissingAssemblyNameParent { get; set; }
        }

        internal class AssemblyDetailInfo
        {
            public AssemblyDetailInfo(string name, string version, string date)
            {
                AssemblyName = name;
                AssemblyVersion = version;
                AssemblyDate = date;
            }

            public string AssemblyName { get; private set; }
            public string AssemblyVersion { get; private set; }
            public string AssemblyDate { get; private set; }
        }
        #endregion Supporting Information

        #region Original Source
        /*
         * The original source pasted below is extracted from https://stackoverflow.com/questions/383686/how-do-you-loop-through-currently-loaded-assemblies on 27 December 2017
         * 
         /// <summary>
    ///     Intent: Get referenced assemblies, either recursively or flat. Not thread safe, if running in a multi
    ///     threaded environment must use locks.
    /// </summary>
    public static class GetReferencedAssemblies
    {
        static void Demo()
        {
            var referencedAssemblies = Assembly.GetEntryAssembly().MyGetReferencedAssembliesRecursive();
            var missingAssemblies = Assembly.GetEntryAssembly().MyGetMissingAssembliesRecursive();
            // Can use this within a class.
            //var referencedAssemblies = this.MyGetReferencedAssembliesRecursive();
        }

        public class MissingAssembly
        {
            public MissingAssembly(string missingAssemblyName, string missingAssemblyNameParent)
            {
                MissingAssemblyName = missingAssemblyName;
                MissingAssemblyNameParent = missingAssemblyNameParent;
            }

            public string MissingAssemblyName { get; set; }
            public string MissingAssemblyNameParent { get; set; }
        }

        private static Dictionary<string, Assembly> _dependentAssemblyList;
        private static List<MissingAssembly> _missingAssemblyList;

        /// <summary>
        ///     Intent: Get assemblies referenced by entry assembly. Not recursive.
        /// </summary>
        public static List<string> MyGetReferencedAssembliesFlat(this Type type)
        {
            var results = type.Assembly.GetReferencedAssemblies();
            return results.Select(o => o.FullName).OrderBy(o => o).ToList();
        }

        /// <summary>
        ///     Intent: Get assemblies currently dependent on entry assembly. Recursive.
        /// </summary>
        public static Dictionary<string, Assembly> MyGetReferencedAssembliesRecursive(this Assembly assembly)
        {
            _dependentAssemblyList = new Dictionary<string, Assembly>();
            _missingAssemblyList = new List<MissingAssembly>();

            InternalGetDependentAssembliesRecursive(assembly);

            // Only include assemblies that we wrote ourselves (ignore ones from GAC).
            var keysToRemove = _dependentAssemblyList.Values.Where(
                o => o.GlobalAssemblyCache == true).ToList();

            foreach (var k in keysToRemove)
            {
                _dependentAssemblyList.Remove(k.FullName.MyToName());
            }

            return _dependentAssemblyList;
        }

        /// <summary>
        ///     Intent: Get missing assemblies.
        /// </summary>
        public static List<MissingAssembly> MyGetMissingAssembliesRecursive(this Assembly assembly)
        {
            _dependentAssemblyList = new Dictionary<string, Assembly>();
            _missingAssemblyList = new List<MissingAssembly>();
            InternalGetDependentAssembliesRecursive(assembly);

            return _missingAssemblyList;
        }

        /// <summary>
        ///     Intent: Internal recursive class to get all dependent assemblies, and all dependent assemblies of
        ///     dependent assemblies, etc.
        /// </summary>
        private static void InternalGetDependentAssembliesRecursive(Assembly assembly)
        {
            // Load assemblies with newest versions first. Omitting the ordering results in false positives on
            // _missingAssemblyList.
            var referencedAssemblies = assembly.GetReferencedAssemblies()
                .OrderByDescending(o => o.Version);

            foreach (var r in referencedAssemblies)
            {
                if (String.IsNullOrEmpty(assembly.FullName))
                {
                    continue;
                }

                if (_dependentAssemblyList.ContainsKey(r.FullName.MyToName()) == false)
                {
                    try
                    {
                        var a = Assembly.ReflectionOnlyLoad(r.FullName);
                        _dependentAssemblyList[a.FullName.MyToName()] = a;
                        InternalGetDependentAssembliesRecursive(a);
                    }
                    catch (Exception ex)
                    {
                        _missingAssemblyList.Add(new MissingAssembly(r.FullName.Split(',')[0], assembly.FullName.MyToName()));
                    }
                }
            }
        }

        private static string MyToName(this string fullName)
        {
            return fullName.Split(',')[0];
        }
    } 
         */
        #endregion Original Source
    }
}