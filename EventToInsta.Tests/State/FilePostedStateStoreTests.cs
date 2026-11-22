using EventToInsta.State;

namespace EventToInsta.Tests.State;

public class FilePostedStateStoreTests
{
    [Fact]
    public async Task FileStore_RoundTrip_PreservesAllFields()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new FilePostedStateStore(path);
            var state = new PostedState("2026-W48", new DateOnly(2026, 11, 22), new[] { 7, 9 }, true);
            await store.SaveAsync(state);
            var loaded = await store.LoadAsync();
            Assert.Equal(state.PreviewWeek, loaded.PreviewWeek);
            Assert.Equal(state.Date, loaded.Date);
            Assert.Equal(state.HerrenMatchIds, loaded.HerrenMatchIds);
            Assert.Equal(state.JugendPosted, loaded.JugendPosted);
            Assert.Contains("\"date\": \"2026-11-22\"", await File.ReadAllTextAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task FileStore_MissingFile_ReturnsEmpty()
    {
        var store = new FilePostedStateStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
        var loaded = await store.LoadAsync();
        Assert.Null(loaded.PreviewWeek);
        Assert.Null(loaded.Date);
        Assert.Empty(loaded.HerrenMatchIds);
        Assert.False(loaded.JugendPosted);
    }
}
