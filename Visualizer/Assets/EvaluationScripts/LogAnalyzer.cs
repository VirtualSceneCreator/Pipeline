using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System;

namespace Assets.EvaluationScripts
{
    public class LogAnalyzer
    {
        public static PipeLineInfo AnalyzeLog(string content)
        {
            var tsMatches = Regex.Matches(content, @"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3})\]");
            var timestamps = new List<DateTime>();
            foreach (Match m in tsMatches)
            {
                if (DateTime.TryParseExact(
                        m.Groups[1].Value,
                        "yyyy-MM-dd HH:mm:ss,fff",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime dt))
                {
                    timestamps.Add(dt);
                }
            }

            TimeSpan totalDuration = TimeSpan.MinValue;
            if (timestamps.Count >= 2)
            {
                timestamps.Sort();
                totalDuration = timestamps[^1] - timestamps[0];
            }

            var modulePattern = new Regex(
                @"INFO:\s+(?<module>\w+)\s+\|\s+Request finished in\s+(?<duration>[\d.]+)\s+s\s+\|\s+Tokens prompt=(?<prompt>\d+),\s+completion=(?<completion>\d+),\s+total=(?<total>\d+)",
                RegexOptions.Compiled);

            var modules = new List<PipeLineModuleInfo>();
            foreach (Match m in modulePattern.Matches(content))
            {
                var info = new PipeLineModuleInfo
                {
                    Name = m.Groups["module"].Value,
                    Duration = double.Parse(m.Groups["duration"].Value, CultureInfo.InvariantCulture),
                    PromptTokens = int.Parse(m.Groups["prompt"].Value, CultureInfo.InvariantCulture),
                    CompletionTokens = int.Parse(m.Groups["completion"].Value, CultureInfo.InvariantCulture),
                    TotalTokens = int.Parse(m.Groups["total"].Value, CultureInfo.InvariantCulture)
                };
                modules.Add(info);
            }


            PipeLineInfo pipeLineInfo = new PipeLineInfo(totalDuration: totalDuration.TotalSeconds, modules: modules);

            return pipeLineInfo;
        }
    }
}