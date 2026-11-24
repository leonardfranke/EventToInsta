using EventToInsta.Data;
using EventToInsta.Domain;
using EventToInsta.Planning;
using SkiaSharp;

namespace EventToInsta.Rendering;

public record RenderedStory(string FileName, byte[] Jpeg);

public sealed class StoryRenderer(AssetStore assets, ILogoSource logos) : IStoryRenderer
{
    public async Task<RenderedStory> RenderAsync(StoryRequest request, CancellationToken ct = default)
    {
        switch (request)
        {
            case HerrenPreview p:
            {
                using var opponentLogo = await LoadLogoAsync(p.Game.Opponent, ct);
                using var club = assets.ClubLogo();
                return Render(request, c => HerrenStory.DrawPreview(c, p, club, opponentLogo));
            }
            case HerrenResult r:
            {
                using var opponentLogo = await LoadLogoAsync(r.Game.Opponent, ct);
                using var club = assets.ClubLogo();
                return Render(request, c => HerrenStory.DrawResult(c, r, club, opponentLogo));
            }
            case JugendPreview jp:
                return Render(request, c => JugendStory.DrawPreview(c, jp, assets.SponsorLogo));
            case JugendResult jr:
                // Sponsoren vorerst nur in der Vorschau (Entscheidung 24.11.2026); DrawResult kann sie weiterhin zeichnen.
                return Render(request, c => JugendStory.DrawResult(c, jr, _ => null));
            default:
                throw new NotSupportedException($"Unbekannte Story: {request.GetType().Name}");
        }
    }

    private static RenderedStory Render(StoryRequest request, Action<SKCanvas> draw)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Theme.Width, Theme.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Zeichenfläche konnte nicht erstellt werden");
        draw(surface.Canvas);
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
            // Fehlendes oder kaputtes Logo darf die Story nicht verhindern: dann zeigt die Story das Badge.
            return null;
        }
    }
}
