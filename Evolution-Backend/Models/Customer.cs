namespace Evolution_Backend.Models
{
    public class Customer_Request
    {
        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string StatusCode { get; set; }
    }

    public class Customer_ChangeStatus_Request
    {
        public string CustomerCode { get; set; }

        public string StatusCode { get; set; }
    }
}
