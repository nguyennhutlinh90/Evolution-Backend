namespace Evolution_Backend.Models
{
    public class User_Request
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Role { get; set; }

        public string StatusCode { get; set; }
    }

    public class User_ChangePassword_Request
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }

    public class User_ChangeRole_Request
    {
        public string UserName { get; set; }

        public string Role { get; set; }
    }

    public class User_ChangeStatus_Request
    {
        public string UserName { get; set; }

        public string StatusCode { get; set; }
    }
}
