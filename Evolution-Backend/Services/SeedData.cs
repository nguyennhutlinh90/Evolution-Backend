using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class SeedData
    {
        public static async Task InitUser(IServiceProvider serviceProvider)
        {
            var userName = "admin";
            var userService = serviceProvider.GetService<IUserService>();
            var srGet = await userService.Get(userName);
            if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                throw new Exception(srGet.ErrorMessage);

            if (srGet.Data == null)
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();

                var msgCreate = await userService.Create(new User_Collection()
                {
                    user_name = userName,
                    password = (config["SeedData:UserPwd"] ?? "admin@123").HashPassword(),
                    first_name = "Admin",
                    last_name = "Admin",
                    role = Constants.Role.Admin
                });

                if (!string.IsNullOrEmpty(msgCreate))
                    throw new Exception(msgCreate);
            }
        }
    }
}
