using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class UserService : ServiceBase, IUserService
    {
        private readonly DbContext _dbContext;
        private readonly string _tokenKey;
        public UserService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
            _tokenKey = config.Value.TokenKey;
        }

        public async Task<ServiceResponse<Authentication>> Authentication(string userName, string password)
        {
            return await ExecuteAsync(async () =>
            {
                var user = await _dbContext.user.FindOneAsync(u => u.user_name == userName && u.password == password);
                if (user == null)
                    return null;

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenKeyEncode = Encoding.ASCII.GetBytes(_tokenKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.id),
                        new Claim(ClaimTypes.Name, user.user_name),
                        new Claim(ClaimTypes.Role, user.role ?? "")
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKeyEncode), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return new Authentication
                {
                    id = user.id,
                    user_name = user.user_name,
                    first_name = user.first_name,
                    last_name = user.last_name,
                    role = user.role,
                    device_id = user.device_id,
                    token = tokenHandler.WriteToken(token),
                    expiration = token.ValidTo
                };
            });
        }

        public async Task<string> Create(User_Collection user)
        {
            return await ExecuteAsync(async () =>
            {
                user.created_on = user.updated_on = DateTime.Now;

                await _dbContext.user.InsertOneAsync(user);
            });
        }

        public async Task<string> Delete(string userName)
        {
            return await ExecuteAsync(async () =>
            {
                await _dbContext.user.DeleteOneAsync(u => u.user_name == userName);
            });
        }

        public async Task<string> Update(string userName, User_Collection user)
        {
            return await ExecuteAsync(async () =>
            {
                user.updated_on = DateTime.Now;

                await _dbContext.user.ReplaceOneAsync(u => u.user_name == userName, user);
            });
        }

        public async Task<string> UpdateDeviceId(string userName, string deviceId)
        {
            return await ExecuteAsync(async () =>
            {
                var updateSetDeviceId = Builders<User_Collection>.Update.Set(u => u.device_id, deviceId);
                var updateCombine = Builders<User_Collection>.Update.Combine(updateSetDeviceId);
                await _dbContext.user.UpdateOneAsync(po => po.user_name == userName, updateCombine);
            });
        }

        public async Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.user.Read<User_Collection, T>(stages, pageSkip, pageLimit);
            });
        }

        public async Task<ServiceResponse<User_Collection>> Get(string userName)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.user.FindOneAsync(u => u.user_name == userName);
            });
        }

        public async Task<ServiceResponse<User_Collection>> GetById(string userId)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.user.FindOneAsync(u => u.id == userId);
            });
        }
    }
}
