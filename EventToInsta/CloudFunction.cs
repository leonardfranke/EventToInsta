using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

namespace EventToInsta
{
    internal class CloudFunction : IHttpFunction
    {
        public async Task HandleAsync(HttpContext context)
        {
            await Program.Main([]);
        }
    }
}
