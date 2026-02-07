namespace LongJobProcessor.Application.Abstractions;

public interface IConnectionManager
{
    void RegisterConnection(string userId, Guid jobId, StreamWriter writer);
    void UnregisterConnection(string userId, Guid jobId);
    StreamWriter? GetConnection(string userId, Guid jobId);

}
