using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Exiled.DataClasses
{
    public class PoELogLine
    {
        public DateTime DateTimeLocalMachine { get; set; }
        public string IdLikeThing { get; set; }
        public string EventType { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public long LineNumber { get; set; }
        public string FullLine { get; set; }        
        public string SubType { get; set; }
        public string Zone { get; set; }        
        public string CharacterName { get; set; }
        public int CharacterLevel { get; set; }
        public string CharacterType { get; set; }
        public string SkillId { get; set; }
        public string SkillName { get; set; }
        public string HideoutName { get; set; }
        public string MapName { get; set; }
        public int MapTier { get; set; }
        public double ZoneRunTime { get; set; }
        public bool ZoneCompleteEvent { get; set; }
        public bool IsTown { get; set; } = false;

        public string TargetIndex => $"poepdp-{DateTimeLocalMachine.ToUniversalTime().ToString("yyyy-MM-dd")}";
    }
}
