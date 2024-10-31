namespace EFDotnet.Models;

public class UserRegisterReq
{
    public string username { get; set; }
    public string email { get; set; }
    public string password { get; set; }
}

public class UserLoginReq
{
    public string username { get; set; }
    public string password { get; set; }
}