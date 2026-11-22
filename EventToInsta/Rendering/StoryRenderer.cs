using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using SkiaSharp;

namespace EventToInsta.Rendering;

using Team = EventToInsta.Domain.Team;

public record RenderedStory(string FileName, byte[] Jpeg);

public sealed class StoryRenderer(AssetStore assets, ILogoSource logos, string homePlace) : IStoryRenderer
{
    public async Task<RenderedStory> RenderAsync(StoryRequest request, CancellationToken ct = default)
    {
        switch (request)
        {
            case HerrenPreview p:
            {
                using var opponentLogo = await LoadLogoAsync(p.Game.Opponent, ct);
                using var club = assets.ClubLogo();
                return Render(request, assets.RandomHerrenPhoto, (c, photo) =>
                    HerrenStory.DrawPreview(c, p, photo, club, opponentLogo, homePlace));
            }
            case HerrenResult r:
            {
                using var opponentLogo = await LoadLogoAsync(r.Game.Opponent, ct);
                using var club = assets.ClubLogo();
                return Render(request, assets.RandomHerrenPhoto, (c, photo) =>
                    HerrenStory.DrawResult(c, r, photo, club, opponentLogo));
            }
            case JugendPreview jp:
                return Render(request, assets.RandomJugendPhoto, (c, photo) =>
                    JugendStory.DrawPreview(c, jp, photo, assets.SponsorLogo, homePlace));
            case JugendResult jr:
                return Render(request, assets.RandomJugendPhoto, (c, photo) =>
                    JugendStory.DrawResult(c, jr, photo));
            default:
                throw new NotSupportedException($"Unbekannte Story: {request.GetType().Name}");
        }
    }

    private static RenderedStory Render(StoryRequest request, Func<SKBitmap> loadPhoto, Action<SKCanvas, SKBitmap> draw)
    {
        using var photo = loadPhoto();
        using var surface = SKSurface.Create(new SKImageInfo(Theme.Width, Theme.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Zeichenfläche konnte nicht erstellt werden");
        draw(surface.Canvas, photo);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, Theme.JpegQuality);
        return new RenderedStory(request.FileName, data.ToArray());
    }

    private async Task<SKBitmap?> LoadLogoAsync(Team team, CancellationToken ct)
    {
        try
        {
            var bytes = await logos.LoadTeamLogoAsync(team.PermanentId, ct);
            return bytes is { Length: > 0 } ? SKBitmap.Decode(bytes) : null;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Fehlendes oder kaputtes Logo darf die Story nicht verhindern: dann zeigt der Fuß das Badge.
            return null;
        }
    }
}
