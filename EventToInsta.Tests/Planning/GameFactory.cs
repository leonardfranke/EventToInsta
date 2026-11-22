using EventToInsta.Domain;

namespace EventToInsta.Tests.Planning;

public static class GameFactory
{
    public const int Club = 621;

    public static Game Make(int id, DateTime kickoff, bool herren = false, bool home = true, Score? result = null)
    {
        var us = new Team(1, "SV Bad Laer", "Bad Laer", Club);
        var them = new Team(2, "Gegner " + id, "Gegner", 999);
        return new Game(id, herren ? "Senioren" : "U14", "Liga", kickoff,
            home ? us : them, home ? them : us, null, result, Club);
    }
}
