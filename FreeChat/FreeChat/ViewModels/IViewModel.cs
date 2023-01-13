using System.Threading.Tasks;

namespace FreeChat.ViewModels
{
    public interface IViewModel
    {
        Task Initialize();
        Task Stop();
    }
}
