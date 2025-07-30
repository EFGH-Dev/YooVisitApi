using YooVisitApi.Dtos.User;

namespace YooVisitApi.Models.Login
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public UserDto User { get; set; }
    }
}
