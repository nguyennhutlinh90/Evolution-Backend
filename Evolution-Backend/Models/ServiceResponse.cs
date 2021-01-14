using System.Collections.Generic;

namespace Evolution_Backend.Models
{
    public class ServiceResponse<T>
    {
        public ServiceResponse()
        {

        }

        public ServiceResponse(T data)
        {
            Data = data;
        }

        public ServiceResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public T Data { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class ServiceReadResponse<T>
    {
        public ServiceReadResponse()
        {

        }

        public ServiceReadResponse(List<T> datas, long total)
        {
            Datas = datas;
            Total = total;
        }

        public ServiceReadResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public List<T> Datas { get; set; }

        public long Total { get; set; }

        public string ErrorMessage { get; set; }
    }
}
