namespace Assets.EvaluationScripts
{
    public class PipeLineModuleInfo
    {
        public string Name { get; set; }
        public double Duration { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
