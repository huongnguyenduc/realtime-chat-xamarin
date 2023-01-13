using System;
using Fusillade;

namespace FreeChat.Services
{
    public interface IApiService<T>
    {
        T GetApi(Priority priority);
    }
}

