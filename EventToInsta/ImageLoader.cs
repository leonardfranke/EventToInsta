using Dropbox.Api;
using Flurl.Http;

namespace EventToInsta
{
    public static class ImageLoader
    {
        private static FlurlClient _client;
        private static string _seniorenBildURL = "https://www.dropbox.com/scl/fi/86ezvp3jrijbmizkbu767/IMG_0014.JPG?rlkey=1d22i4gj86pa5yn7w2xkv9mis&st=30b8yrpb&dl=1";
        private static string _jugendBildURL = "https://dl.dropboxusercontent.com/scl/fi/6rsu25b3mznbneawh9rt3/Test.jpg?rlkey=d2nn4la06jprcknzbpx21varh&st=iyb1prhd&dl=1";
        private static string _dachseLogoURL = "https://www.dropbox.com/scl/fi/esau5yea4rphjgg1or6vh/Dachse.png?rlkey=dmm39zk26ex6pt89388ghdg1i&st=josv75pt&dl=1";

        private static DropboxClient _dropboxClient;
        private static string _dropboxApiKey = "sl.u.AGIMZonkF9wY1XTvQwdqmbxjEY6VJ9kGy0v6JB2o4Eh09-ClUX8MBU-KF3-nk3Xbapfh2uM3a1g-IL1_LSo73LgMn6ma1ZbygAynNlYFAMbLEuET_FKLDl4mu-snrJdDy6XOrim2WGTCUMeLyJy-izy1t-J_HnEHPQRjSToA7qrTFhXEs51su3lJdqYN-OeNd_JP5Y1T8sqDfzGo3g7qRXdAxz1-ReqggTmHou7kNAfS1wlZMsT_4tmQfOcM9KH1z4PJMXII7Wo37kNivi6NcuZC5JTNgRmi9Re29z8L0NuRm9LSdePMS_r_F3m6bkUmnpc0l_052dxL8zfsICwyvU3te6-Je_Bg1Y5iPHIJEAruqPCSkJHpmb-MdhM37HIFQdgeVp2MBj1V7lthmYVqa9_8rV8rQpqtKj7bHLicuLTyiIxbXHGsKIzDNlO3qIDxCVZv-g9SUokarwexG2xejXlzt1fK1AC1nSB4wfOiyXs-cOqXKj_FdpT-uVtIVUHbNnHTfF18-W1tTiV2UJisWvLHb97N77nTwEpHobfu6hZE-iQvpcmnCmbkLuPIU5pnQPICVQQJnz3Gmawbuj87YqWGo1aumLWD2xbgZhoHCPk2kpoXAyOVIIvf5ql8eTcPZo9LJnJuravLXTevZ-X3CboWWIPHkQ_gpLTFciUsl8XjyXzxf26QFU9rCgOUlgAEEpAY4eCQFy4wjV0Yd8wpFh8yX9shGHb-DbujiWObffXJ8xmhonRT_jbWh02pfpIxBOAIiuceuDKw7kVvk_b6dJdG8Cm7o22OOHhJQpIkDcfCKQd-K9tiMuVwIzNHhQpOsf7cRcsPXC4HghQ-e84_YtXB5MvuXLrPc8Aif-WY-YA-xNU_ZOGcQiMx68H82alrMEsUY_prhogoBiDYiosnKyTAxpcBQJV-dXvQZz8yrs1sK_oD1cnXWKGdQoM-nHX4wU2PlCX7MsLKc7Pam8MtMVs6VuVFjfCGp6THTM6q96gZXFu36nKlYKmNll6dWv_kRG7fwTlifP_GmNpDAETcu8GoANiBQ8JTamOegZNfxco8bLyxzMRkE-3n07OOtX4PesvdfL1ZW0rmC-D0g0kP_Hw3Erj819TvqbhFGhZiHHNJDHE98oUyi9cqxCHDnWxs2N516noYw2F3STfDJTyDjpWGE76LhL8Rdmrh_encrH39y5bL0ym0K9zzf5CFNwqGUjbKzXuTywUKztxI1f7NvtqCv6eGCItRum6t-PjuoUynH4eIA2imTuDbvixrgzsuS5mTUouy_99Pp1u-7aPVl4Ae";
        private static Random _random = new Random();

        static ImageLoader()
        {
            _client = new FlurlClient();
            _dropboxClient = new DropboxClient(_dropboxApiKey);
        }

        public static Task<byte[]> LoadJugend()
        {
            return _client.Request(_jugendBildURL).GetBytesAsync();
        }

        public static async Task<byte[]> LoadSenioren()
        {
            var result = await _dropboxClient.Files.ListFolderAsync("/Senioren");
            var files = result.Entries.Where(i => i.IsFile).ToList();
            var randomFileData = files[_random.Next(files.Count)];
            var file = await _dropboxClient.Files.DownloadAsync(randomFileData.PathLower);
            return await file.GetContentAsByteArrayAsync();
        }

        public static Task<byte[]> LoadDachseLogo()
        {
            return _client.Request(_dachseLogoURL).GetBytesAsync();
        }
    }
}
