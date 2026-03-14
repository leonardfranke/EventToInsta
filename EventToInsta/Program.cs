using EventToInsta;
using System.Net.Http.Headers;

var outputPath = Path.Combine(AppContext.BaseDirectory, "Output");
Directory.CreateDirectory(outputPath);

var matches = await MatchesLoader.LoadMatches();

var seniorenMatches = matches.Where(match => match.Age == "Senioren");
foreach (var match in seniorenMatches)
{
    var background = ImageLoader.LoadSenioren();
    var image = await ImageCreator.CreateSeniorenImage(match, background);

    await using var output = File.OpenWrite(Path.Combine(outputPath, "output.png"));
    image.SaveTo(output);

    image.Dispose();
}
var jugendMatches = matches
    .Where(match => match.Age != "Senioren")
    .OrderBy(match => match.TipoffTime)
    .GroupBy(match => match.TipoffTime.DayOfYear)
    .OrderBy(group => group.Key);
foreach (var matchGroup in jugendMatches)
{
    var background = ImageLoader.LoadSenioren();
    var image = await ImageCreator.CreateJugendImage(matchGroup, background);

    var fileName = $"output_{matchGroup.Key}.png";
    await using var output = File.OpenWrite(Path.Combine(outputPath, fileName));
    image.SaveTo(output);
    image.Dispose();
}

return;
var outputFiles = Directory.GetFiles(outputPath);
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", Environment.GetEnvironmentVariable("upload_posts_api_key"));
var form = new MultipartFormDataContent();
foreach(var outputfile in outputFiles)
    form.Add(new StringContent("photos[]"), outputfile);
form.Add(new StringContent("user"), "Dachse");
form.Add(new StringContent("platform[]"), "instagram");
form.Add(new StringContent("media_type"), "STORIES");

var uploadURl = "https://api.upload-post.com/api/upload";
var request = await httpClient.PostAsync(uploadURl, form);
var response = await request.Content.ReadAsStringAsync();
Console.WriteLine(response);