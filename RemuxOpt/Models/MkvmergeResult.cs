namespace RemuxOpt
{
    public class MkvmergeResult
    {
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public int ExitCode { get; set; }
        public string ResultType { get; set; } = "Unknown";
    }
}
