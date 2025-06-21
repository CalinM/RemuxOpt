namespace RemuxOpt
{
    public class WorkerResult
    {
        public List<MkvFileInfo> Files { get; set; } = [];
        public BackgroundTaskType TaskType { get; set; }
    }
}