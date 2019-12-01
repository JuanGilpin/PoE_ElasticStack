using Exiled.Core.DataClasses;
using Exiled.Core.Helpers;
using Exiled.DataClasses;
using Exiled.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Exiled
{
    public class ExileAbilities: IExileAbilities {
        public FileSystemWatcher WatchForChange(string dirLocation, string fileName)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = dirLocation;
            
            watcher.NotifyFilter = NotifyFilters.Size; //| NotifyFilters.LastWrite notifies twice (cause last updated time change + content updated);

            // Only watch text files.
            watcher.Filter = fileName;
                       
            return watcher;
        }

        public void UpdateSettings(string fileLocation, ExilePDPSettings settings) {
            File.WriteAllText(fileLocation, JsonConvert.SerializeObject(settings));
        }

        public ExilePDPSettings GetSettings(string fileLocation)
        {
            ExilePDPSettings settings;
            if (File.Exists(fileLocation))
            {
                settings = JsonConvert.DeserializeObject<ExilePDPSettings>(File.ReadAllText(fileLocation));
            } else {
                FileInfo file = new FileInfo(fileLocation);
                file.Directory.Create();
                settings = new ExilePDPSettings { LastPosition = 0 };
                File.WriteAllText(fileLocation, JsonConvert.SerializeObject(settings));
            }
            return settings;
        }               

        public List<ExileMap> GetMaps(string mapsFilePath)
        {
            List<ExileMap> maps = new List<ExileMap>();
            if (File.Exists(mapsFilePath))
            {
                maps = JsonConvert.DeserializeObject<List<ExileMap>>(File.ReadAllText(mapsFilePath));
            }
            else
            {
                throw new Exception("MapFileNotFound");
            }
            return maps;
        }

        public List<ExileTown> GetTowns(string townsFilePath) {
            List<ExileTown> maps = new List<ExileTown>();
            if (File.Exists(townsFilePath))
            {
                maps = JsonConvert.DeserializeObject<List<ExileTown>>(File.ReadAllText(townsFilePath));
            }
            else
            {
                throw new Exception("TownFileNotFound");
            }
            return maps;
        }

        public PoELogFile ProcessClientFile(string fileLocation, List<ExileMap> maps, List<ExileTown> towns, long lastPosition)
        {
            PoELogLine zoneEnteredLine = null;
            var logContent = new PoELogFile { Lines = new List<PoELogLine>(), Sessions = new List<PoESession>() };
            var fs = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (fs)
            {
                int index = 0;
                int skip = 0;
                using (StreamReader sr = new StreamReader(fs))
                {
                    sr.BaseStream.Seek(lastPosition, SeekOrigin.Begin);
                    while (sr.Peek() >= 0)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                if (skip > 0)
                                {
                                    skip--;
                                } else if (line.Contains("***** LOG FILE OPENING *****")){
                                    skip = 1;                                    
                                    UpdateZoneRuntime(zoneEnteredLine, logContent.Lines[logContent.Lines.Count - 1]);
                                    continue;
                                } else {
                                    var lineObj = Parsers.ParseLogLine(line, maps, towns, index + 1);
                                    logContent.Lines.Add(lineObj);
                                    if (lineObj.Message.Contains("Instant/Triggered action"))
                                    {
                                        // Skip 5
                                        skip = 5;
                                        continue;
                                    } else if (lineObj.SubType == "Slain") {
                                        UpdateZoneRuntime(zoneEnteredLine, lineObj);
                                        continue;
                                    }

                                    // Need townZones.json
                                    // Need labZones.json
                                        
                                    // Need to aggregate some times together, especially if deaths/re-entries + lab runs

                                    // Log File opened needs to get previous line and stamp as end time

                                    // ZoneChange or Hideout == start
                                    else if (lineObj.SubType == "ZoneChange" || lineObj.SubType == "Hideout") {
                                        zoneEnteredLine = lineObj;
                                    }
                                    // ce6 == stop timer
                                    else if (lineObj.Message == "Got Instance Details from login server") {
                                        UpdateZoneRuntime(zoneEnteredLine, lineObj);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // This is now random quotes over multiple lines
                                // Console.WriteLine(ex.Message);
                            }
                        }
                        index++;                                                                 
                    }
                    logContent.LastStreamPosition = sr.BaseStream.Position;
                }
            }
            UpdateZoneRuntime(zoneEnteredLine, logContent.Lines[logContent.Lines.Count - 1]);

            return logContent;
        }

        private void UpdateZoneRuntime(PoELogLine zoneEnteredLine, PoELogLine finalEvent) {            
            if (zoneEnteredLine != null && zoneEnteredLine.ZoneRunTime == 0) {
                var timeDiffInMins = finalEvent.DateTimeLocalMachine - zoneEnteredLine.DateTimeLocalMachine;
                // Memory pointer means it will update object in logContent lines
                zoneEnteredLine.ZoneRunTime = Math.Round(timeDiffInMins.TotalMinutes, 2);
                zoneEnteredLine.ZoneCompleteEvent = true;            
            }
        }
    }
}
