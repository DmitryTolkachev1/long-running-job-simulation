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

    public void UpdateProgress(int cursor, string produced)
    {
        Cursor = cursor;
        Produced = produced;
    }

    public void ResetProgress()
    {
        Cursor = 0;
        Produced = string.Empty;
    }
}
