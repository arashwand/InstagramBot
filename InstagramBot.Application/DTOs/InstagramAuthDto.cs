namespace InstagramBot.Application.DTOs
{
    public class InstagramAuthDto
    {
        public string AuthorizationUrl { get; set; }
        public string State { get; set; }
    }

    public class InstagramCallbackDto
    {
        public string Code { get; set; }
        public string State { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }

    public class InstagramTokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class InstagramLongLivedTokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class FacebookPageResponse
    {
        public List<FacebookPage> Data { get; set; }
    }

    public class FacebookPage
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AccessToken { get; set; }
        public string Category { get; set; }
        public List<string> Tasks { get; set; }
    }

    public class InstagramBusinessAccount
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string ProfilePictureUrl { get; set; }
        public int FollowersCount { get; set; }
        public int FollowsCount { get; set; }
        public int MediaCount { get; set; }
    }

}
