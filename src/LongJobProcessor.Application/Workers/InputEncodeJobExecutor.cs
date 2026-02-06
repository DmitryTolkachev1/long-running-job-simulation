using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using System.Text;

namespace LongJobProcessor.Application.Workers;

public sealed class InputEncodeJobExecutor : IJobExecutor
{
    private readonly Random _random = new();

    public JobType JobType => JobType.Encode;

    public async Task ExecuteAsync(Job job, Func<object, Task> progressCallback, CancellationToken cancellationToken)
    {
        if (job is not InputEncodeJob inputEncodeJob)
        {
            throw new ArgumentException(nameof(job));
        }

        var processed = BuildEncoded(inputEncodeJob.Input);

        foreach (var character in processed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await progressCallback(character);

            var delayInSeconds = _random.Next(1, 6);
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), cancellationToken);
        }
    }

    private static string BuildEncoded(string input)
    {
        var counts = new Dictionary<char, int>();
        foreach (var character in input)
        {
            if(character == ' ')
            {
                continue;
            }

            if(!counts.TryAdd(character, 1))
            {
                counts[character]++;
            }
        }

        var sorted = counts.Keys.OrderBy(c => c).ToList();

        var prefix = new StringBuilder();
        foreach (var character in sorted)
        {
            prefix.Append(counts[character]);
            prefix.Append(character);
        }

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

        return prefix.Append('/').Append(base64).ToString();
    }
}
