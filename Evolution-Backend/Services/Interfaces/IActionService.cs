using Evolution_Backend.DbModels;

using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface IActionService
    {
        Task<string> Create(Action_Collection action);
    }
}
