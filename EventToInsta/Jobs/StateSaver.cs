using EventToInsta.State;

namespace EventToInsta.Jobs;

/// <summary>
/// Der Beitrag ist online, aber <c>posted.json</c> wurde nicht geschrieben. Ein erneuter Lauf würde ihn
/// doppelt posten (sofern der Idempotency-Key nicht mehr greift), deshalb eine eigene, auffällige Meldung.
/// </summary>
public sealed class StateNotSavedException(string key, Exception inner)
    : Exception($"{StateSaver.NotSavedMessage(key)}: {inner.Message}", inner)
{
    public string Key { get; } = key;
}

/// <summary>Speichert den Zustand nach einem erfolgreichen Publish mit wenigen Wiederholungen.</summary>
internal static class StateSaver
{
    public const int Attempts = 3;
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(1);

    public static string NotSavedMessage(string key) =>
        $"VERÖFFENTLICHT, Zustand NICHT gespeichert – {key} – nicht erneut auslösen";

    /// <summary>
    /// Läuft bewusst mit <see cref="CancellationToken.None"/>: Nach dem Publish darf die Buchführung nicht
    /// abbrechen. Scheitern alle Versuche, wird die Meldung geloggt und <see cref="StateNotSavedException"/> geworfen.
    /// </summary>
    public static async Task SaveAfterPublishAsync(
        IPostedStateStore store, PostedState state, string key, TimeSpan delay, Action<string> log)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await store.SaveAsync(state, CancellationToken.None);
                return;
            }
            catch (Exception e) when (attempt < Attempts)
            {
                log($"Zustand für {key} nicht gespeichert (Versuch {attempt}/{Attempts}): {e.Message}");
                if (delay > TimeSpan.Zero) await Task.Delay(delay, CancellationToken.None);
            }
            catch (Exception e)
            {
                var error = new StateNotSavedException(key, e);
                log(error.Message);
                throw error;
            }
        }
    }
}
