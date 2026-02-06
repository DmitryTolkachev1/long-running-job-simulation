namespace LongJobProcessor.Domain.Entities.Jobs.InputEncode;

public sealed class InputEncodeJob : Job
{
    public InputEncodeJob(string userId)
        : base(userId)
    {
    }

    public required string Input { get; set; }
    public int Cursor { get; set; }
    public string? Produced { get; set; }
    public int TotalLength { get => Input.Length; }
}
