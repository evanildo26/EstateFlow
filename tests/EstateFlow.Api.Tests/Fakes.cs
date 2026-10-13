using System.Collections.Concurrent;
using EstateFlow.Api.Services.Notifications;
using EstateFlow.Api.Services.Storage;

namespace EstateFlow.Api.Tests;

public record SentEmail(string To, string Subject, string Body);

public class FakeNotificationService : INotificationService
{
    public ConcurrentQueue<SentEmail> Sent { get; } = new();

    public Task NotifyAsync(string recipientEmail, string subject, string body)
    {
        Sent.Enqueue(new SentEmail(recipientEmail, subject, body));
        return Task.CompletedTask;
    }
}

public class InMemoryBlobStorage : IBlobStorageService
{
    public ConcurrentDictionary<string, byte[]> Blobs { get; } = new();

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Blobs[fileName] = copy.ToArray();
        return fileName;
    }

    public Task<Stream> DownloadAsync(string blobReference) =>
        Blobs.TryGetValue(blobReference, out var data)
            ? Task.FromResult<Stream>(new MemoryStream(data))
            : throw new FileNotFoundException(blobReference);

    public Task DeleteAsync(string blobReference)
    {
        Blobs.TryRemove(blobReference, out _);
        return Task.CompletedTask;
    }
}
