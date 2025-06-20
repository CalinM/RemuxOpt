namespace RemuxOpt
{
    public class WorkerPayload
    {
        public List<string> Files { get; set; } = new();
        public MkvRemuxHelper RemuxHelper { get; set; } = default!;
    }

}