using SkiaSharp;
using System.Threading.Tasks;
using Topten.RichTextKit;

namespace EventToInsta
{
    internal static class ImageCreator
    {

        private static SKColor _blueColor = new SKColor(0x0B, 0x4E, 0xA2, 255);
        private static SKColor _transparentBlueColor = new SKColor(0x0B,0x4E,0xA2, 0);
        private static SKColor _partialTransparentBlueColor = new SKColor(0x0B, 0x4E, 0xA2, 80);
        private static SKColor _whiteColor = SKColor.Parse("#FFFFFF");

        public static async Task<SKData> CreateSeniorenImage(MatchInfo match, byte[] image)
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
            surface.Canvas.DrawRect(0, gradientStart, bitmap.Width, gradientEnd- gradientStart, paint);
            surface.Canvas.DrawRect(0, gradientStart, bitmap.Width, gradientEnd - gradientStart, paint);
            surface.Canvas.DrawRect(0, gradientEnd, bitmap.Width, bitmap.Height - gradientEnd, new SKPaint { Color = _blueColor });

            //Spielzeit
            var spielzeitBottomMargin = 150;
            var timeTextMargin = 10;
            var borderSize = 2;
            var timeText = new RichString().FontFamily("Anton").TextColor(_whiteColor).Bold().FontSize(70).Add(match.TipoffTime.ToString("dddd, dd.MM. - HH:mm") + " Uhr");
            var timeTextX = (bitmap.Width - timeText.MeasuredWidth) / 2f;
            var timeTextY = bitmap.Height - timeText.MeasuredHeight - spielzeitBottomMargin;
            //surface.Canvas.DrawRect(timeTextX - (timeTextMargin + borderSize), timeTextY - (timeTextMargin + borderSize), timeText.MeasuredWidth + 2 * (timeTextMargin + borderSize), timeText.MeasuredHeight + 2 * (timeTextMargin + borderSize), new SKPaint { Color = _whiteColor });
            //surface.Canvas.DrawRect(timeTextX - timeTextMargin, timeTextY - timeTextMargin, timeText.MeasuredWidth + 2 * timeTextMargin, timeText.MeasuredHeight + 2 * timeTextMargin, new SKPaint { Color = _lightBlueColor });
            timeText.Paint(surface.Canvas, new SKPoint(timeTextX, timeTextY), new TextPaintOptions {  });

            //Gegner
            var gegnerTextSpacing = 40;
            var teamsText = new RichString().Alignment(TextAlignment.Center).Bold().FontFamily("Anton").TextColor(_whiteColor).FontSize(60).Add($"Herren gegen {match.OpponentName}");
            var teamsTextX = (bitmap.Width - teamsText.MeasuredWidth) / 2f;
            if(teamsTextX < 20)
            {
                teamsText = new RichString().Alignment(TextAlignment.Center).Bold().FontFamily("Anton").TextColor(_whiteColor).FontSize(60).Add($"Herren gegen {match.OpponentNameSmall}");
                teamsTextX = (bitmap.Width - teamsText.MeasuredWidth) / 2f;
            }
            teamsText.Paint(surface.Canvas, new SKPoint(teamsTextX, timeTextY - teamsText.MeasuredHeight - gegnerTextSpacing), new TextPaintOptions { });

            //Spielort
            var ortText = new RichString().FontFamily("Anton").TextColor(_whiteColor).FontSize(40).Add("Sporthalle Bad Laer — Kreisliga");
            ortText.Paint(surface.Canvas, new SKPoint((bitmap.Width - ortText.MeasuredWidth) / 2f, bitmap.Height - ortText.MeasuredHeight - 20), new TextPaintOptions { });

            //Header
            var headerHeight = 250;
            var headerGradientLength = 170;
            var heaederTextTopMargin = 200;
            surface.Canvas.DrawRect(0, 0, bitmap.Width, headerHeight, new SKPaint { Color = _partialTransparentBlueColor });
            var gradientShaderHeader = SKShader.CreateLinearGradient(new SKPoint(0, headerHeight), new SKPoint(0, headerHeight + headerGradientLength), [_partialTransparentBlueColor, _transparentBlueColor], SKShaderTileMode.Clamp);
            surface.Canvas.DrawRect(0, headerHeight, bitmap.Width, headerGradientLength, new SKPaint { Shader = gradientShaderHeader, BlendMode = SKBlendMode.SrcOver });
            var headerText = new RichString().FontFamily("Anton").Bold().TextColor(_whiteColor).FontSize(140).LetterSpacing(20).Add("HEIMSPIEL");
            headerText.Paint(surface.Canvas, new SKPoint((bitmap.Width - headerText.MeasuredWidth) / 2f, heaederTextTopMargin), new TextPaintOptions { });

            //DachseLogo
            var dachseLogoSize = 250;
            var dachseLogoTopMargin = 400;
            var dachseLogoRightMargin = 20;
            var dachseLogo = await ImageLoader.LoadDachseLogo();
            var logoBitmap = SKBitmap.Decode(dachseLogo);
            surface.Canvas.DrawBitmap(logoBitmap, new SKRect(bitmap.Width - dachseLogoSize - dachseLogoRightMargin, dachseLogoTopMargin, bitmap.Width - dachseLogoRightMargin, dachseLogoTopMargin + dachseLogoSize));

            surface.Canvas.Flush();
            using var newImage = surface.Snapshot();
            var newImageData = newImage.Encode(SKEncodedImageFormat.Jpeg, 100);
            return newImageData;
        }
    }
}
