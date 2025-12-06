namespace IdentityService.Api.Extensions
{
    public static class MimeType
    {
        public static string GetMimeType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };
        }
    }
}
