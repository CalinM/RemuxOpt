namespace RemuxOpt
{
    public class WorkerPayload
    {
        public List<MkvFileInfo> Files { get; set; } = new();
        public MkvRemuxHelper RemuxHelper { get; set; } = default!;
    }
}