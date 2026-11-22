using System.Net;
using System.Text;
using Google;
using Google.Cloud.Storage.V1;

namespace EventToInsta.State;

public sealed class GcsPostedStateStore(StorageClient client, string bucket) : IPostedStateStore
{
    private const string ObjectName = "posted.json";

    public async Task<PostedState> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await client.DownloadObjectAsync(bucket, ObjectName, ms, cancellationToken: ct);
            return PostedStateJson.Deserialize(Encoding.UTF8.GetString(ms.ToArray()));
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return PostedState.Empty;
        }
    }

    public async Task SaveAsync(PostedState state, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(PostedStateJson.Serialize(state)));
        await client.UploadObjectAsync(bucket, ObjectName, "application/json", ms, cancellationToken: ct);
    }
}
