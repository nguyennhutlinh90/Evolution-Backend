using System;

namespace Evolution_Backend.Models
{
    public class Authentication
    {
        public string id { get; set; }

        public string user_name { get; set; }

        public string first_name { get; set; }

        public string last_name { get; set; }

        public string role { get; set; }

        public string device_id { get; set; }

        public string token { get; set; }

        public DateTime expiration { get; set; }
    }

    public class Authentication_Request
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }

    public class PDA_SignIn_Request
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string DeviceId { get; set; }
    }

    public class PDA_SignOut_Request
    {
        public string UserName { get; set; }

        public string DeviceId { get; set; }
    }
}
