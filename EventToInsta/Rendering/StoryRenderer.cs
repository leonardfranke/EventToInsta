using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using SkiaSharp;

namespace EventToInsta.Rendering;

using Team = EventToInsta.Domain.Team;

public record RenderedStory(string FileName, byte[] Jpeg);

public sealed class StoryRenderer(AssetStore assets, ILogoSource logos, string homePlace)
{
    public async Task<RenderedStory> RenderAsync(StoryRequest request, CancellationToken ct = default)
    {
        switch (request)
        {
            case HerrenPreview p:
            {
                using var opponentLogo = await LoadLogoAsync(p.Game.Opponent, ct);
                return Render(request, (c, photo, club) =>
                    HerrenStory.DrawPreview(c, p, photo, club, opponentLogo, homePlace));
            }
            case HerrenResult r:
            {
                using var opponentLogo = await LoadLogoAsync(r.Game.Opponent, ct);
                return Render(request, (c, photo, club) =>
                    HerrenStory.DrawResult(c, r, photo, club, opponentLogo));
            }
            case JugendPreview or JugendResult:
                throw new NotSupportedException("Jugend-Stories folgen in Task 7.");
            default:
                throw new NotSupportedException($"Unbekannte Story: {request.GetType().Name}");
        }
    }

    private RenderedStory Render(StoryRequest request, Action<SKCanvas, SKBitmap, SKBitmap> draw)
    {
        using var photo = assets.RandomHerrenPhoto();
        using var club = assets.ClubLogo();
        using var surface = SKSurface.Create(new SKImageInfo(Theme.Width, Theme.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Zeichenfläche konnte nicht erstellt werden");
        draw(surface.Canvas, photo, club);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, Theme.JpegQuality);
        return new RenderedStory(request.FileName, data.ToArray());
    }

    private async Task<SKBitmap?> LoadLogoAsync(Team team, CancellationToken ct)
    {
        var bytes = await logos.LoadTeamLogoAsync(team.PermanentId, ct);
        return bytes is { Length: > 0 } ? SKBitmap.Decode(bytes) : null;
    }
}
