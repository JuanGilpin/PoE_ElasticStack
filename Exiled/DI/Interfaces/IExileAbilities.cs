using Exiled.Core.DataClasses;
using Exiled.DataClasses;
using System.Collections.Generic;
using System.IO;

namespace Exiled
{
    public interface IExileAbilities {
        PoELogFile ProcessClientFile(string fileLocation, List<ExileMap> maps, List<ExileTown> towns, long lastPosition);
        FileSystemWatcher WatchForChange(string dirLocation, string fileName);
        ExilePDPSettings GetSettings(string fileLocation);
        List<ExileMap> GetMaps(string mapsFilePath);
        List<ExileTown> GetTowns(string townsFilePath);
        void UpdateSettings(string fileLocation, ExilePDPSettings settings);
    }
}
