using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;

namespace SPTarkov.Server.Core.Callbacks;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.HttpCallbacks)]
public class HttpCallbacks(HttpServer httpServer) : IOnLoad
{
    public Task OnLoad()
    {
        httpServer.Load();

        return Task.CompletedTask;
    }

    public string GetImage()
    {
        return "";
    }
}
