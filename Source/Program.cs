using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace SlnToCsv
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileEnding = "packages.config";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("*************************************");
            Console.WriteLine("Export 'packages.config' packages");
            Console.WriteLine("*************************************");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Search directory: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            var dir = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Output file: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            var output = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Notes: ");
            Console.WriteLine("* Export as CSV");
            Console.WriteLine("* Columns separated by ':'");
            Console.WriteLine("* First column indicates column content.");

            var files = Search(dir, fileEnding);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(files.Count + " " + fileEnding + " found.");
            
            var packages = new Dictionary<string, PackageInformation>();

            foreach (var file in files)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Resolving file: " + file);
                Resolve(file, packages);
            }

            using (var fs = new FileStream(output, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine("Package:Versions:Target Frameworks:Assemblies");

                foreach (var packageInformation in packages.Values)
                {
                    sw.WriteLine(packageInformation.ToString());
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done.");

            Console.ReadKey();
        }

        static ISet<string> Search(string dir, string fileEnding)
        {
            var files = new HashSet<string>();

            try
            {
                foreach (var directory in Directory.GetDirectories(dir))
                {
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        if (file.EndsWith(fileEnding))
                        {
                            files.Add(file);
                        }
                    }

                    var result = Search(directory, fileEnding);

                    foreach (var foundFile in result)
                    {
                        files.Add(foundFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return files;
        }

        static void Resolve(string file, IDictionary<string, PackageInformation> packages)
        {
            var dir = Path.GetDirectoryName(file);
            var filesInDir = Directory.GetFiles(dir);

            var csproj = filesInDir.Single(x => x.EndsWith(".csproj"));
            var assemblyPattern = @"<AssemblyName>(.*)<\/AssemblyName>";
            var csprojFile = File.ReadAllText(csproj);

            var assemblyName = Regex.Match(csprojFile, assemblyPattern).Groups[1].Value;

            var packageXml = new XmlDocument();
            packageXml.Load(file);

            var packagesElement = packageXml.GetElementsByTagName("package");

            foreach (XmlNode package in packagesElement)
            {
                var packageId = package.Attributes["id"].Value;
                var packageVersion = package.Attributes["version"].Value;
                var targetFramework = package.Attributes["targetFramework"].Value;
                
                packages.TryGetValue(packageId, out var addedPackage);

                if (addedPackage == null)
                {
                    addedPackage = new PackageInformation(packageId);

                    packages.Add(packageId, addedPackage);
                }

                addedPackage.Versions.Add(packageVersion);
                addedPackage.TargetFrameworks.Add(targetFramework);
                addedPackage.Assemblies.Add(assemblyName);
            }
        }

        class PackageInformation
        {
            private readonly string packageId;

            public PackageInformation(string packageId)
            {
                this.Versions = new HashSet<string>();
                this.Assemblies = new HashSet<string>();
                this.TargetFrameworks = new HashSet<string>();
                this.packageId = packageId;
            }
            
            public ISet<string> Versions { get; }
            public ISet<string> TargetFrameworks { get; }
            public ISet<string> Assemblies { get; }

            public override string ToString()
            {
                var versions = string.Join(", ", this.Versions);
                var assemblies = string.Join(", ", this.Assemblies);
                var targetFrameworks = string.Join(", ", this.TargetFrameworks);

                var formatted = string.Format("{0}:{1}:{2}:{3}", packageId, versions, targetFrameworks, assemblies);

                return formatted;
            }
        }
    }
}
