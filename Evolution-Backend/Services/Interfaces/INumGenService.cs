using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface INumGenService
    {
        Task<string> Create(Num_Gen_Collection numGen);

        Task<string> Update(string genType, Num_Gen_Collection numGen);

        Task<ServiceResponse<Num_Gen_Collection>> Get(string genType);
    }
}
