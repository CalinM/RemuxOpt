namespace RemuxOpt
{
    public class ProgressMessage
    {
        public string StatusText { get; set; }
        public string RemuxLog { get; set; }

        public ProgressMessage(string statusText, string logChunk)
        {
            StatusText = statusText;
            RemuxLog = logChunk;
        }
    }
}
