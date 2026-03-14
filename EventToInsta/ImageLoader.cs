namespace EventToInsta
{
    public static class ImageLoader
    {
        private static string Images = Path.Combine(AppContext.BaseDirectory, "Images");
        private static string SeniorenImages = Path.Combine(Images, "Herren");
        private static string JugendImages = Path.Combine(Images, "Herren");
        private static string LogoImages = Path.Combine(Images, "Logos");

        private static Random _random = new Random();

        public static Dictionary<string, string> AgeToSponsor = new Dictionary<string, string>
        {
            { "U12", "Böhnke.png" },
            { "U14", "Mann.png" },
            { "U16", "Stockmeyer.webp" },
            { "U18", "Mittelstedt.png" },
        };

        public static byte[] LoadJugend()
        {
            var files = Directory.GetFiles(JugendImages);
            var randomFile = files[_random.Next(files.Length)];
            var image = File.ReadAllBytes(randomFile);
            return image;
        }

        public static byte[] LoadSenioren()
        {
            var files = Directory.GetFiles(SeniorenImages);
            var randomFile = files[_random.Next(files.Length)];
            var image = File.ReadAllBytes(randomFile);
            return image;
        }

        public static byte[] LoadDachseLogo()
        {
            var dachseLogo = File.ReadAllBytes(Path.Combine(LogoImages, "Dachse.png"));
            return dachseLogo;
        }

        public static byte[] GetSponsorImage(string age)
        {
            if (!AgeToSponsor.ContainsKey(age))
                return null;
            var sponsorFileName = AgeToSponsor[age];
            var sponsorenFile = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Images/Sponsoren", sponsorFileName));
            return sponsorenFile;
        }
    }
}
