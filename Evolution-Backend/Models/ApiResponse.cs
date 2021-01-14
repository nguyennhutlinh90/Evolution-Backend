namespace Evolution_Backend.Models
{
    public class ApiResponse
    {
        public ApiResponse()
        {

        }

        public ApiResponse(object _data)
        {
            data = _data;
            is_success = true;
        }

        public ApiResponse(int errorCode, string errorMessage)
        {
            error_code = errorCode;
            error_message = errorMessage;
        }

        public object data { get; set; }

        public int error_code { get; set; }

        public string error_message { get; set; }

        public bool is_success { get; set; }
    }

    public class ApiReadResponse
    {
        public ApiReadResponse()
        {

        }

        public ApiReadResponse(object _data)
        {
            data = _data;
            is_success = true;
        }

        public ApiReadResponse(object _data, long _total)
        {
            data = _data;
            total = _total;
            is_success = true;
        }

        public ApiReadResponse(int errorCode, string errorMessage)
        {
            error_code = errorCode;
            error_message = errorMessage;
        }

        public object data { get; set; }

        public long total { get; set; }

        public int error_code { get; set; }

        public string error_message { get; set; }

        public bool is_success { get; set; }
    }
}
