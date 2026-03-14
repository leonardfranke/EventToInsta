using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

namespace EventToInsta
{
    internal class CloudFunction : IHttpFunction
    {
        public async Task HandleAsync(HttpContext context)
        {
            var matches = await MatchesLoader.LoadMatches();

            var seniorenMatches = matches.Where(match => match.Age == "Senioren");
            foreach (var match in seniorenMatches)
            {
                var background = ImageLoader.LoadSenioren();
                var image = await ImageCreator.CreateSeniorenImage(match, background);

                await using var output = File.OpenWrite("C:\\Users\\leona\\Downloads\\output.png");
                image.SaveTo(output);

                image.Dispose();
            }
        }
    }
}
