using EventToInsta;


var matches = await MatchesLoader.LoadMatches();

var seniorenMatches = matches.Where(match => match.Age == "Senioren");
foreach(var match in seniorenMatches)
{
    var background = await ImageLoader.LoadSenioren();
    var image = await ImageCreator.CreateSeniorenImage(match, background);

    await using var output = File.OpenWrite("C:\\Users\\leona\\Downloads\\output.png");
    image.SaveTo(output);

    image.Dispose();

    //var heimspielText = new RichString().Alignment(TextAlignment.Left).FontFamily("Anton").TextColor(blueColor).FontSize(50).Add("Heimspiel");
    //heimspielText.Paint(surface.Canvas, new SKPoint(20, 20), new TextPaintOptions { });



    //teamsText.Paint(surface.Canvas, new SKPoint(teamsTextx1, teamsTexty1), new TextPaintOptions { });

}

return;
/*var account_id = "17841458488770584";// Environment.GetEnvironmentVariable("Instagram_Account_Id");
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
Console.WriteLine(response);*/