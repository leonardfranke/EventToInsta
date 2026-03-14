using EventToInsta;


var matches = await MatchesLoader.LoadMatches();

var seniorenMatches = matches.Where(match => match.Age == "Senioren");
//foreach (var match in seniorenMatches)
//{
//    var background = await ImageLoader.LoadSenioren();
//    var image = await ImageCreator.CreateSeniorenImage(match, background);

//    await using var output = File.OpenWrite("C:\\Users\\leona\\Downloads\\output.png");
//    image.SaveTo(output);

//    image.Dispose();
//}
var jugendMatches = matches
    .Where(match => match.Age != "Senioren")
    .OrderBy(match => match.TipoffTime)
    .GroupBy(match => 0)
    .OrderBy(group => group.Key);
foreach (var matchGroup in jugendMatches)
{
    var background = ImageLoader.LoadSenioren();
    var image = await ImageCreator.CreateJugendImage(matchGroup, background);

    var fileName = $"output_{matchGroup.Key}.png";
    await using var output = File.OpenWrite($"C:\\Users\\leona\\Downloads\\{fileName}");
    image.SaveTo(output);
    image.Dispose();
}

return;
var account_id = "17841458488770584";// Environment.GetEnvironmentVariable("Instagram_Account_Id");
var instagram_key = Environment.GetEnvironmentVariable("Instagram_Key");

var requestUrl = $"https://graph.instagram.com/v24.0/{account_id}/media?image_url={image_url}";
var data = new
{
    media_type = "STORIES"
};
var content = JsonContent.Create(data);
using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", instagram_key);
var res = await httpClient.SendAsync(request);
var response = await res.Content.ReadAsStringAsync();
Console.WriteLine(response);