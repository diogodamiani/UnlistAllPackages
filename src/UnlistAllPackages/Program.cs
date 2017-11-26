using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace UnlistAllPackages
{
    public class Logger : ILogger
    {
        public void LogDebug(string data) { }
        public void LogVerbose(string data) { }
        public void LogInformation(string data) { }
        public void LogMinimal(string data) { }
        public void LogWarning(string data) { }
        public void LogError(string data) { }
        public void LogSummary(string data) { }
        public void LogInformationSummary(string data) { }
        public void LogErrorSummary(string data) { }
    }

    class Program
    {
        const string NUGET_API_SERVICE_INDEX = "https://api.nuget.org/v3/index.json";

        static ILogger logger = new Logger(); 

        private static async Task<IEnumerable<IPackageSearchMetadata>> GetListedPackages(string packageID)
        {
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support

            PackageSource packageSource = new PackageSource(NUGET_API_SERVICE_INDEX);

            SourceRepository sourceRepository = new SourceRepository(packageSource, providers);

            PackageMetadataResource packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
            IEnumerable<IPackageSearchMetadata> searchMetadata = await packageMetadataResource.GetMetadataAsync(packageID, true, true, logger, CancellationToken.None);

            return searchMetadata;           
        }

        private static string UnlistPackage(IPackageSearchMetadata package, string apiKey)
        {
            var arguments = $"delete {package.Identity.Id} {package.Identity.Version} -Source {NUGET_API_SERVICE_INDEX} -ApiKey {apiKey} -NonInteractive";
            var psi = new ProcessStartInfo("nuget.exe", arguments)
            {
                RedirectStandardOutput = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false
            };
            var process = Process.Start(psi);
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd();
        }

        static void Main(string[] args)
        {
            var packageID = string.Empty;
            var apiKey = string.Empty;

            if (args.Length <= 0)
            {
                ShowError("You should input -packageId and -apiKey parameters.");
                return;
            }

            if (args.Length == 2)
            {
                packageID = args[0];
                apiKey = args[1];
            }

            if (args.Length == 4)
            {
                packageID = args[1];
                apiKey = args[3];
            }

            if (string.IsNullOrWhiteSpace(packageID) || string.IsNullOrWhiteSpace(apiKey))
            {
                ShowError("You should input -packageId and -apiKey parameters.");
                return;
            }

            var packages = GetListedPackages(packageID);

            foreach (var package in packages.Result)
            {
                Console.WriteLine($"Unlisting package { package.Identity.Id } {package.Identity.Version}");
                var output = UnlistPackage(package, apiKey);
                Console.WriteLine(output);
            }
            Console.Write("Completed. Press ENTER to quit.");
            Console.ReadLine();
        }

        static void ShowError(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press ENTER to quit.");
            Console.ReadLine();
        }
    }
}
