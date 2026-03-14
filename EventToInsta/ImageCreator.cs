using SkiaSharp;
using Topten.RichTextKit;

namespace EventToInsta
{
    internal static class ImageCreator
    {

        private static SKColor _blueColor = new SKColor(0x0B, 0x4E, 0xA2, 255);
        private static SKColor _transparentBlueColor = new SKColor(0x0B,0x4E,0xA2, 0);
        private static SKColor _partialTransparentBlueColor = new SKColor(0x0B, 0x4E, 0xA2, 80);
        private static SKColor _whiteColor = SKColor.Parse("#FFFFFF");


        public static async Task<SKData> CreateSeniorenImage(MatchData match, byte[] image)
        {
            using var bitmap = SKBitmap.Decode(image);
            using var surface = SKSurface.Create(bitmap.Info);
            surface.Canvas.DrawBitmap(bitmap, 0, 0);

            //Gradient
            var bottomAreaHeight = 300;
            var gradientLength = 300;
            var gradientEnd = bitmap.Height - bottomAreaHeight;
            var gradientStart = gradientEnd - gradientLength;
            var gradientShader = SKShader.CreateLinearGradient(new SKPoint(0, gradientStart), new SKPoint(0, gradientEnd), [_transparentBlueColor, _blueColor], SKShaderTileMode.Clamp);
            var paint = new SKPaint { Shader = gradientShader, BlendMode = SKBlendMode.SrcOver };
            surface.Canvas.DrawRect(0, gradientStart, bitmap.Width, gradientEnd - gradientStart, paint);
            surface.Canvas.DrawRect(0, gradientEnd, bitmap.Width, bitmap.Height - gradientEnd, new SKPaint { Color = _blueColor });



            //Spielort
            var ortText = new RichString().FontFamily("Anton").TextColor(_whiteColor).FontSize(40).Add($"Sporthalle Bad Laer — {match.League}");
            ortText.Paint(surface.Canvas, new SKPoint((bitmap.Width - ortText.MeasuredWidth) / 2f, bitmap.Height - ortText.MeasuredHeight - 25), new TextPaintOptions { });

            //Header
            var headerHeight = 270;
            var headerGradientLength = 170;
            var heaederTextTopMargin = 170;
            surface.Canvas.DrawRect(0, 0, bitmap.Width, headerHeight, new SKPaint { Color = _partialTransparentBlueColor });
            var gradientShaderHeader = SKShader.CreateLinearGradient(new SKPoint(0, headerHeight), new SKPoint(0, headerHeight + headerGradientLength), [_partialTransparentBlueColor, _transparentBlueColor], SKShaderTileMode.Clamp);
            surface.Canvas.DrawRect(0, headerHeight, bitmap.Width, headerGradientLength, new SKPaint { Shader = gradientShaderHeader, BlendMode = SKBlendMode.SrcOver });
            var headerText = new RichString().FontFamily("Anton").Bold().TextColor(_whiteColor).FontSize(140).LetterSpacing(20).Add("HEIMSPIEL");
            headerText.Paint(surface.Canvas, new SKPoint((bitmap.Width - headerText.MeasuredWidth) / 2f, heaederTextTopMargin), new TextPaintOptions { });
            
            //Spielzeit
            var spielzeitBottomMargin = 270;
            var timeTextMargin = 10;
            var borderSize = 2;
            var timeText = new RichString().FontFamily("Anton").TextColor(_whiteColor).Bold().FontSize(70).Add(match.TipoffTime.ToString("dddd, dd.MM. - HH:mm") + " Uhr");
            var timeTextX = (bitmap.Width - timeText.MeasuredWidth) / 2f;
            var timeTextY = bitmap.Height - timeText.MeasuredHeight - spielzeitBottomMargin;
            //surface.Canvas.DrawRect(timeTextX - (timeTextMargin + borderSize), timeTextY - (timeTextMargin + borderSize), timeText.MeasuredWidth + 2 * (timeTextMargin + borderSize), timeText.MeasuredHeight + 2 * (timeTextMargin + borderSize), new SKPaint { Color = _whiteColor });
            //surface.Canvas.DrawRect(timeTextX - timeTextMargin, timeTextY - timeTextMargin, timeText.MeasuredWidth + 2 * timeTextMargin, timeText.MeasuredHeight + 2 * timeTextMargin, new SKPaint { Color = _lightBlueColor });
            timeText.Paint(surface.Canvas, new SKPoint(timeTextX, timeTextY), new TextPaintOptions { });

            //Gegner
            var gegnerTextSpacing = 40;
            var opponentText = match.OpponentName.Replace("(ak)", string.Empty, true, culture: null);
            var teamsText = new RichString().Alignment(TextAlignment.Center).Bold().FontFamily("Anton").TextColor(_whiteColor).FontSize(60).Add($"Herren gegen {opponentText}");
            var teamsTextX = (bitmap.Width - teamsText.MeasuredWidth) / 2f;
            if (teamsTextX < 20)
            {
                teamsText = new RichString().Alignment(TextAlignment.Center).Bold().FontFamily("Anton").TextColor(_whiteColor).FontSize(60).Add($"Herren gegen {match.OpponentNameSmall}");
                teamsTextX = (bitmap.Width - teamsText.MeasuredWidth) / 2f;
            }
            var teamsTextY = timeTextY - teamsText.MeasuredHeight - gegnerTextSpacing;
            teamsText.Paint(surface.Canvas, new SKPoint(teamsTextX, teamsTextY), new TextPaintOptions { });

            var opponentLogo = await MatchesLoader.LoadTeamImage(match.OpponentTeamId);
            var logoSize = 250;
            var logoY = teamsTextY - logoSize - 20;
            var dachseLogo = ImageLoader.LoadDachseLogo();
            var logoBitmap = SKBitmap.Decode(dachseLogo);
            if (opponentLogo != null)
            {
                var opponentBitmap = SKBitmap.Decode(opponentLogo);
                var centerOffset = 80;
                surface.Canvas.DrawBitmap(logoBitmap, new SKRect(bitmap.Width / 2 - logoSize - centerOffset, logoY, bitmap.Width / 2 - centerOffset, logoY + logoSize));
                surface.Canvas.DrawBitmap(opponentBitmap, new SKRect(bitmap.Width / 2 + centerOffset, logoY, bitmap.Width / 2 + centerOffset + logoSize, logoY + logoSize));
                var dashText = new RichString().Alignment(TextAlignment.Center).Bold().FontFamily("Anton").TextColor(_whiteColor).FontSize(150).Add("-");
                dashText.Paint(surface.Canvas, new SKPoint((bitmap.Width - dashText.MeasuredWidth) / 2f, logoY + (logoSize - dashText.MeasuredHeight) / 2f), new TextPaintOptions { });
            }
            else
            {
                surface.Canvas.DrawBitmap(logoBitmap, new SKRect((bitmap.Width - logoSize) / 2, logoY, (bitmap.Width + logoSize) / 2, logoY + logoSize));
            }


            surface.Canvas.Flush();
            using var newImage = surface.Snapshot();
            var newImageData = newImage.Encode(SKEncodedImageFormat.Jpeg, 100);
            return newImageData;
        }

        public static async Task<SKData> CreateJugendImage(IEnumerable<MatchData> matches, byte[] image)
        {
            using var bitmap = SKBitmap.Decode(image);
            using var surface = SKSurface.Create(bitmap.Info);
            surface.Canvas.DrawBitmap(bitmap, 0, 0);

            //Gegner
            var matchTexts = matches.OrderBy(match => match.TipoffTime).ToDictionary(match => match, match =>
            {
                var opponentText = match.OpponentName.Replace("(ak)", string.Empty, true, culture: null).Trim();
                var textBlock = new TextBlock() { Alignment = TextAlignment.Center };
                textBlock.AddText($"{match.Age} gegen {opponentText}", new Style { FontFamily = "Anton", FontSize = 60, FontWeight = 600, TextColor = _blueColor });
                var matchTextX = (bitmap.Width - textBlock.MeasuredWidth) / 2f;
                if (matchTextX < 20)
                {
                    textBlock = new TextBlock() { Alignment = TextAlignment.Center };
                    textBlock.AddText($"{match.Age} gegen {match.OpponentNameSmall}", new Style { FontFamily = "Anton", FontSize = 60, FontWeight = 600, TextColor = _blueColor });
                    matchTextX = (bitmap.Width - textBlock.MeasuredWidth) / 2f;
                }
                textBlock.AddText($"\n{match.TipoffTime:HH:mm} Uhr", new Style { FontFamily = "Anton", FontSize = 55, FontWeight = 600, TextColor = _blueColor });
                return textBlock;
            });

            var maxWidth = matchTexts.Values.Max(text => text.MeasuredWidth);
            var textPadding = 25;
            var textMargin = 60;
            var totalHeight = matchTexts.Values.Sum(text => text.MeasuredHeight) + (2 * textPadding + textMargin) * (matchTexts.Count() - 1);
            var textY = (bitmap.Height - totalHeight) / 2 + 100;
            var dachseLogo = ImageLoader.LoadDachseLogo();
            foreach (var (match, text) in matchTexts)
            {
                var drawActions = new List<Action>();
                var textX = (bitmap.Width - text.MeasuredWidth) / 2f;
                var drawTextAction = () => text.Paint(surface.Canvas, new SKPoint(textX, textY), new TextPaintOptions { });
                drawActions.Add(drawTextAction);
                var minTextY = textY - textPadding;
                var maxTextY = textY + text.MeasuredHeight + textPadding;

                var additionalInfoY = textY + text.MeasuredHeight - 30;
                var sponsor = ImageLoader.GetSponsorImage(match.Age);
                var sponsorLogoMargin = 10;
                var sponsorLogoPadding = 15;
                if (sponsor != null)
                {
                    additionalInfoY += 50;
                    var logoBitmap = SKBitmap.Decode(sponsor);
                    var sponsorText = new RichString()
                        .FontFamily("Anton")
                        .FontSize(30)
                        .Bold()
                        .TextColor(_blueColor)
                        .Add($"unterstützt durch");

                    var maxLogoWidth = 190f;
                    var maxLogoHeight = 110f;
                    var rescaleFactor = Math.Max(logoBitmap.Width / maxLogoWidth, logoBitmap.Height / maxLogoHeight);
                    var newLogoWidth = logoBitmap.Width / rescaleFactor;
                    var newLogoHeight = logoBitmap.Height / rescaleFactor;

                    var logoRight = (bitmap.Width + maxWidth) / 2 + textPadding - sponsorLogoMargin;
                    var logoLeft = logoRight - newLogoWidth;

                    var drawSponsorTextAction = () => sponsorText.Paint(surface.Canvas, new SKPoint(logoLeft - sponsorText.MeasuredWidth - sponsorLogoPadding, additionalInfoY), new TextPaintOptions { });
                    drawActions.Add(drawSponsorTextAction);
                    maxTextY = Math.Max(maxTextY, additionalInfoY + sponsorText.MeasuredHeight + textPadding / 2);

                    var logoBottom = maxTextY - sponsorLogoMargin;
                    var logoTop = logoBottom - newLogoHeight;

                    var drawSponsorLogoAction = () => surface.Canvas.DrawBitmap(logoBitmap, new SKRect(0, 0, logoBitmap.Width, logoBitmap.Height), new SKRect(logoLeft, logoTop, logoRight, logoBottom));
                    drawActions.Add(drawSponsorLogoAction);
                }

                if (match.Place != null && match.Place != "Bad Laer")
                {
                    var placeText = new RichString()
                        .FontFamily("Anton")
                        .FontSize(30)
                        .Bold()
                        .TextColor(_blueColor)
                        .Add($"in {match.Place}");
                    //surface.Canvas.DrawRect((bitmap.Width - maxWidth) / 2 - textPadding, textY + text.MeasuredHeight + textPadding, maxWidth + 2 * textPadding, placeText.MeasuredHeight + textPadding - (textPadding - placeTextMargin), new SKPaint { Color = _whiteColor });                    
                    var drawPlaceTextAction = () => placeText.Paint(surface.Canvas, new SKPoint((bitmap.Width - maxWidth) / 2, additionalInfoY), new TextPaintOptions { });
                    drawActions.Add(drawPlaceTextAction);
                    maxTextY = Math.Max(maxTextY, additionalInfoY + placeText.MeasuredHeight + textPadding/2);
                }

                surface.Canvas.DrawRect((bitmap.Width - maxWidth) / 2 - textPadding, minTextY, maxWidth + 2 * textPadding, maxTextY - minTextY, new SKPaint { Color = _whiteColor });
                drawActions.Aggregate((act1, act2) => (Action)Delegate.Combine(act1, act2)).Invoke();

                textY = maxTextY + 2*textPadding + textMargin;
            }

            //Header
            var headerHeight = 300;
            var headerGradientLength = 170;
            surface.Canvas.DrawRect(0, 0, bitmap.Width, headerHeight, new SKPaint { Color = _partialTransparentBlueColor });
            var gradientShaderHeader = SKShader.CreateLinearGradient(new SKPoint(0, headerHeight), new SKPoint(0, headerHeight + headerGradientLength), [_partialTransparentBlueColor, _transparentBlueColor], SKShaderTileMode.Clamp);
            surface.Canvas.DrawRect(0, headerHeight, bitmap.Width, headerGradientLength, new SKPaint { Shader = gradientShaderHeader, BlendMode = SKBlendMode.SrcOver });

            var heaederTextTopMargin = 200;
            var header1 = new RichString()
                .FontFamily("Anton")
                .FontSize(140)
                .Bold()
                .TextColor(_whiteColor)
                .Add("HEIMSPIELE");
            var header2 = new RichString()
                .FontFamily("Anton")
                .FontSize(100)
                .Bold()
                .TextColor(_whiteColor)
                .Add($"{matches.First().TipoffTime.ToString("ddd., dd.MM.")} - Jugend");
            header1.Paint(surface.Canvas, new SKPoint((bitmap.Width - header1.MeasuredWidth) / 2f, heaederTextTopMargin), new TextPaintOptions { });
            header2.Paint(surface.Canvas, new SKPoint((bitmap.Width - header2.MeasuredWidth) / 2f, heaederTextTopMargin + header1.MeasuredHeight - 40), new TextPaintOptions { });

            //DachseLogo
            //var dachseLogoSize = 250;
            //var dachseLogoTopMargin = 400;
            //var dachseLogoRightMargin = 20;
            //var dachseLogo = await ImageLoader.LoadDachseLogo();
            //var logoBitmap = SKBitmap.Decode(dachseLogo);
            //surface.Canvas.DrawBitmap(logoBitmap, new SKRect(bitmap.Width - dachseLogoSize - dachseLogoRightMargin, dachseLogoTopMargin, bitmap.Width - dachseLogoRightMargin, dachseLogoTopMargin + dachseLogoSize));

            //Footer
            var gradientLength = 300;
            var gradientShader = SKShader.CreateLinearGradient(new SKPoint(0, bitmap.Height - gradientLength), new SKPoint(0, bitmap.Height), [_transparentBlueColor, _blueColor], SKShaderTileMode.Clamp);
            var paint = new SKPaint { Shader = gradientShader, BlendMode = SKBlendMode.SrcOver };
            surface.Canvas.DrawRect(0, bitmap.Height - gradientLength, bitmap.Width, bitmap.Height, paint);

            //Spielort
            var ortText = new RichString().FontFamily("Anton").TextColor(_whiteColor).FontSize(40).Add($"Sporthalle Bad Laer");
            ortText.Paint(surface.Canvas, new SKPoint((bitmap.Width - ortText.MeasuredWidth) / 2f, bitmap.Height - ortText.MeasuredHeight - 25), new TextPaintOptions { });

            surface.Canvas.Flush();
            using var newImage = surface.Snapshot();
            var newImageData = newImage.Encode(SKEncodedImageFormat.Jpeg, 100);
            return newImageData;
        }
    }
}
