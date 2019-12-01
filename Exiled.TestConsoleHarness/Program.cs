using Exiled.Core.DataClasses;
using Exiled.DataClasses;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nest.JsonNetSerializer;
using Elasticsearch.Net;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Exiled.TestConsoleHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            // DI
            var serviceCollection = new ServiceCollection()
                .AddScoped<IExileAbilities, ExileAbilities>()
                .BuildServiceProvider();

            var exileAbilities = serviceCollection.GetService<IExileAbilities>();

            Run(exileAbilities);
            Listen(exileAbilities);

            Console.ReadLine();
        }

        static void Run(IExileAbilities exileAbilities) {
            try
            {
                var dirPath = @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile\logs\";
                var fileName = "Client.txt";
                var logFilePath = $"{dirPath}{fileName}";

                var settingsFileLocation = $"{Directory.GetCurrentDirectory()}\\settings\\data.json";
                var mapsFileLocation = $"{Directory.GetCurrentDirectory()}\\settings\\maps.json";
                var townFileLocation = $"{Directory.GetCurrentDirectory()}\\settings\\towns.json";

                Console.WriteLine($"{DateTime.UtcNow}: Settings location found at {settingsFileLocation}");

                var settings = CheckSettings(exileAbilities, settingsFileLocation);

                var maps = GetMaps(exileAbilities, mapsFileLocation);
                var towns = GetTowns(exileAbilities, townFileLocation);

                var updateCount = 0; // will be (updateCount - 1)/2 for real value
                PoELogFile lastChanges = ProcessClientFile(exileAbilities, settingsFileLocation, settings, maps, towns, logFilePath, settings.LastPosition);
                updateCount++;

                var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
                var connectionSettings = new ConnectionSettings(pool).DefaultIndex("default");
                var elasticClient = new ElasticClient(connectionSettings);

                var uniqueTargetIndexes = lastChanges.Lines.GroupBy(x => x.TargetIndex);

                
                foreach (var group in uniqueTargetIndexes) {                    
                    var bulkAll = elasticClient.BulkAll(group, b => b
                        .BackOffRetries(2)
                        .BackOffTime("30s")
                        .RefreshOnCompleted(true)
                        .MaxDegreeOfParallelism(4)
                        .Size(1000)
                        .Index(group.Key)
                    );                    
                    bulkAll.Subscribe(new BulkAllObserver(                        
                        onNext: (b) => { Console.Write($"."); },
                        onError: (e) => { throw e; },
                        onCompleted: () => { Console.Write($""); }
                    ));                         
                }                
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
                return;
            }
        }

        static void Listen(IExileAbilities exileAbilities) {
            var found = false;
            while (!found)
            {
                Thread.Sleep(2000);
                found = ListenForPoEClose(exileAbilities);
            }
        }

        static bool ListenForPoEClose(IExileAbilities exileAbilities) {
            var processes = Process.GetProcesses();
            var poe = processes.FirstOrDefault(x => x.MainWindowTitle == "Path of Exile");
            if (poe != null) {
                Console.WriteLine($"{DateTime.UtcNow}: Detected Path of Exile start");
                poe.EnableRaisingEvents = true;
                poe.Exited += (s, e) => { Poe_Exited(s, e, exileAbilities); };
                return true;
            }
            return false;
        }

        private static void Poe_Exited(object sender, EventArgs e, IExileAbilities exileAbilities)
        {
            Console.WriteLine($"{DateTime.UtcNow}: Detected Path of Exile end");
            Run(exileAbilities);
            Listen(exileAbilities);
        }

        static ExilePDPSettings CheckSettings(IExileAbilities exileAbilities, string settingsFilePath)
        {
            return exileAbilities.GetSettings(settingsFilePath);            
        }

        static List<ExileMap> GetMaps(IExileAbilities exileAbilities, string mapsFilePath) {
            return exileAbilities.GetMaps(mapsFilePath);
        }

        static List<ExileTown> GetTowns(IExileAbilities exileAbilities, string townsFilePath)
        {
            return exileAbilities.GetTowns(townsFilePath);
        }

        static void UpdateSettings(IExileAbilities exileAbilities, string settingsFilePath, ExilePDPSettings settings)
        {
            exileAbilities.UpdateSettings(settingsFilePath, settings);
        }

        static PoELogFile ProcessClientFile(IExileAbilities exileAbilities, string settingsFileLocation, ExilePDPSettings settings, List<ExileMap> maps, List<ExileTown> towns, string logFilePath, long lastPosition) {
            var results = exileAbilities.ProcessClientFile(logFilePath, maps, towns, lastPosition);

            // Only update last position if we actually managed to get it sent to server
            settings.LastPosition = results.LastStreamPosition;
            UpdateSettings(exileAbilities, settingsFileLocation, settings);

            return results;
        }
    }
}


/*
var watcher = exileAbilities.WatchForChange(dirPath, fileName);
// Add event handlers.      
Mutex mutex = new Mutex();
watcher.Changed += (object sender, FileSystemEventArgs e) => {
    // Wait 20 milliseconds to access file if it has changed
    // Don't want multiple events reading at same time
    if (mutex.WaitOne(20)) {                    
        lastChanges = ProcessClientFile(exileAbilities, settingsFileLocation, settings, logFilePath, settings.LastPosition);
        updateCount++;
        // Wait a second - this is a throttle for a lot of changes being saved
        Thread.Sleep(1000);
        mutex.ReleaseMutex();
    } else {
        Console.WriteLine("Could not access file, already being processed");
    }
};
// Start watching
watcher.EnableRaisingEvents = true;
*/
