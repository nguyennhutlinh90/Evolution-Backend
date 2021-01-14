using Evolution_Backend.Models;

using System;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class ServiceBase
    {
        protected string Execute(Action executor)
        {
            try
            {
                executor();
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        protected async Task<string> ExecuteAsync(Func<Task> excuter)
        {
            try
            {
                await excuter();
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        protected ServiceResponse<T> Execute<T>(Func<T> executor)
        {
            try
            {
                var result = executor();
                return new ServiceResponse<T>(result);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<T>(ex.Message);
            }
        }

        protected async Task<ServiceResponse<T>> ExecuteAsync<T>(Func<Task<T>> executor)
        {
            try
            {
                var result = await executor();
                return new ServiceResponse<T>(result);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<T>(ex.Message);
            }
        }

        protected async Task<ServiceReadResponse<T>> ExecuteAsync<T>(Func<Task<ReadResponse<T>>> executor)
        {
            try
            {
                var result = await executor();
                return new ServiceReadResponse<T>(result.Datas, result.Total);
            }
            catch (Exception ex)
            {
                return new ServiceReadResponse<T>(ex.Message);
            }
        }
    }
}
