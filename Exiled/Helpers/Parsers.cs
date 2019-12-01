using Exiled.Core.DataClasses;
using Exiled.DataClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Exiled.Helpers
{
    public class Parsers
    {
        public static PoESession ParseSessionLine(string line, long lineNumber)
        {
            var pattern = @"(\d{4}\/\d{2}\/\d{2} \d{2}:\d{2}:\d{2})";

            var match = RegularExpressions.RegexRun(pattern, line, lineNumber);
            var localDate = ParseLocalDate(match.Groups[1].Value, lineNumber);

            return new PoESession
            {
                StartDateTimeLocalMachine = localDate,
                LineNumber = lineNumber
            };
        }

        public static PoELogLine ParseLogLine(string line, List<ExileMap> maps, List<ExileTown> towns, long lineNumber)
        {
            var poeLine = new PoELogLine();
            var pattern = @"(\d{4}\/\d{2}\/\d{2} \d{2}:\d{2}:\d{2}) ([^\s]+) (.{2,3}) (\[.+]) (.+)";

            var match = RegularExpressions.RegexRun(pattern, line, lineNumber);
            var localDate = ParseLocalDate(match.Groups[1].Value, lineNumber);
            var message = match.Groups[5].Value;

            poeLine.LineNumber = lineNumber;
            poeLine.DateTimeLocalMachine = localDate;
            poeLine.IdLikeThing = match.Groups[2].Value;
            poeLine.EventType = match.Groups[3].Value;
            poeLine.Level = match.Groups[4].Value;
            poeLine.Message = message;
            poeLine.FullLine = line;
            
            poeLine.SubType = ProcessSubType(message);

            if (poeLine.SubType == "SystemMessage") {
                ProcessSystemMessageContents(poeLine, maps, towns);
            } else if (poeLine.SubType == "None") {
                AttemptSkillPointAssigned(poeLine);
            } else if (poeLine.SubType == "ChatMessageOut") {
                var purchasePattern = @"@To .+: Hi, I would like to buy your.+";
                var purchaseMatch = RegularExpressions.RegexRun(purchasePattern, message, lineNumber);
                if (purchaseMatch.Success)
                {
                    poeLine.SubType = "Purchase";
                }
                var salePattern = @"@From .+: Hi, I would like to buy your.+";
                var saleMatch = RegularExpressions.RegexRun(salePattern, message, lineNumber);
                if (saleMatch.Success)
                {
                    poeLine.SubType = "Sale";
                }
            }
            return poeLine;
        }        

        public static DateTime ParseLocalDate(string dateString, long lineNumber)
        {
            if (DateTime.TryParseExact(dateString, "yyyy/MM/dd HH:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTime logLineDate))
            {
                return logLineDate;
            }
            else
            {
                throw new Exception($"WARN: Could not parse date on line {lineNumber}. It contained:\r\n{dateString}");
            }
        }

        private static string ProcessSubType(string message) {
            var subType = "None";
            if (message.StartsWith("#"))
            {
                subType = "ChatMessageIn";
            }
            else if (message.StartsWith(":"))
            {
                subType = "SystemMessage";
            }
            else if (message.StartsWith("@"))
            {
                subType = "ChatMessageOut";
            }
            return subType;
        }

        private static void ProcessSystemMessageContents(PoELogLine currentLine, List<ExileMap> maps, List<ExileTown> towns) {
            AttemptLocationMessage(currentLine, maps, towns);
            AttemptLevelUpMessage(currentLine);
            AttemptSlain(currentLine);
            AttemptHideout(currentLine);
        }

        private static void AttemptLocationMessage(PoELogLine currentLine, List<ExileMap> maps, List<ExileTown> towns) {
            var locationEntryPattern = @"You have entered (.+)\.";
            var locationMatch = RegularExpressions.RegexRun(locationEntryPattern, currentLine.Message, currentLine.LineNumber);
            if (locationMatch.Success)
            {
                // Check against maps list
                var exileMap = maps.FirstOrDefault(x => x.Name == locationMatch.Groups[1].Value);
                if (exileMap != null) {
                    currentLine.MapName = exileMap.Name;
                    currentLine.MapTier = exileMap.Tier;
                }
                // Check against towns list
                var exileTown = towns.FirstOrDefault(x => x.Name == locationMatch.Groups[1].Value);
                if (exileTown != null)
                {
                    currentLine.IsTown = true;
                }

                currentLine.Zone = locationMatch.Groups[1].Value;
                currentLine.SubType = "ZoneChange";                
            }
        }

        private static void AttemptLevelUpMessage(PoELogLine currentLine) {
            var levelUpPattern = @": (.+) \((.+)\) is now level (.+)";
            var levelUpMatch = RegularExpressions.RegexRun(levelUpPattern, currentLine.Message, currentLine.LineNumber);
            if (levelUpMatch.Success)
            {
                currentLine.CharacterName = levelUpMatch.Groups[1].Value;
                currentLine.CharacterType = levelUpMatch.Groups[2].Value;
                if (int.TryParse(levelUpMatch.Groups[3].Value, out var i)) {
                    currentLine.CharacterLevel = i;
                }
                currentLine.SubType = "LevelUp";
            }
        }

        private static void AttemptSkillPointAssigned(PoELogLine currentLine)
        {
            var skillPointPattern = @"Successfully allocated passive skill id: (.+), name: (.+)";
            var skillPointMatch = RegularExpressions.RegexRun(skillPointPattern, currentLine.Message, currentLine.LineNumber);
            if (skillPointMatch.Success)
            {
                currentLine.SkillId = skillPointMatch.Groups[1].Value;
                currentLine.SkillName = skillPointMatch.Groups[2].Value;
                currentLine.SubType = "SkillAssigned";
            }
        }
        
        private static void AttemptSlain(PoELogLine currentLine) {
            var slainPattern = @": (.+) has been slain\.";
            var slainMatch = RegularExpressions.RegexRun(slainPattern, currentLine.Message, currentLine.LineNumber);
            if (slainMatch.Success)
            {
                currentLine.CharacterName = slainMatch.Groups[1].Value;
                currentLine.SubType = "Slain";
            }
        }
                
        private static void AttemptHideout(PoELogLine currentLine)
        {
            var hideoutPattern = @": You have entered ([^Syndicate].+ Hideout)\.";
            var hideoutMatch = RegularExpressions.RegexRun(hideoutPattern, currentLine.Message, currentLine.LineNumber);
            if (hideoutMatch.Success)
            {
                currentLine.HideoutName = hideoutMatch.Groups[1].Value;
                currentLine.SubType = "Hideout";
            }
        }
    }
}
