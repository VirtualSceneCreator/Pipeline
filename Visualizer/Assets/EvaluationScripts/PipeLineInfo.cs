using System.Collections.Generic;

namespace Assets.EvaluationScripts
{
    public class PipeLineInfo
    {
        private List<PipeLineModuleInfo> _modules;

        public List<PipeLineModuleInfo> Modules => _modules ??= new List<PipeLineModuleInfo>();
        public double TotalDuration { get; set; }

        public PipeLineInfo(List<PipeLineModuleInfo> modules, double totalDuration)
        {
            _modules = modules;
            TotalDuration = totalDuration;
        }
    }
}