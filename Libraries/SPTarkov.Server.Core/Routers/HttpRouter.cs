using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace SPTarkov.Server.Core.Routers;

[Injectable]
public class HttpRouter
{
    protected readonly IEnumerable<DynamicRouter> _dynamicRoutes;
    protected readonly IEnumerable<StaticRouter> _staticRouters;

    public HttpRouter(
        IEnumerable<StaticRouter> staticRouters,
        IEnumerable<DynamicRouter> dynamicRoutes
    )
    {
        _staticRouters = staticRouters;
        _dynamicRoutes = dynamicRoutes;
    }

    /*
    protected groupBy<T>(list: T[], keyGetter: (t: T) => string): Map<string, T[]> {
        const map: Map<string, T[]> = new Map();
        for (const item of list) {
            const key = keyGetter(item);
            const collection = map.get(key);
            if (!collection) {
                map.set(key, [item]);
            } else {
                collection.push(item);
            }
        }
        return map;
    }
    */

    public async ValueTask<string?> GetResponse(HttpRequest req, string sessionID, string? body)
    {
        var wrapper = new ResponseWrapper("");

        var handled = await HandleRoute(req, sessionID, wrapper, _staticRouters, false, body);
        if (!handled)
        {
            await HandleRoute(req, sessionID, wrapper, _dynamicRoutes, true, body);
        }

        return wrapper.Output;
    }

    protected async ValueTask<bool> HandleRoute(
        HttpRequest request,
        string sessionID,
        ResponseWrapper wrapper,
        IEnumerable<Router> routers,
        bool dynamic,
        string? body
    )
    {
        var url = request.Path.Value;

        // remove retry from url
        if (url?.Contains("?retry=") ?? false)
        {
            url = url.Split("?retry=")[0];
        }

        var matched = false;
        foreach (var route in routers)
        {
            if (route.CanHandle(url, dynamic))
            {
                if (dynamic)
                {
                    wrapper.Output =
                        await (route as DynamicRouter).HandleDynamic(
                            url,
                            body,
                            sessionID,
                            wrapper.Output
                        ) as string;
                }
                else
                {
                    wrapper.Output =
                        await (route as StaticRouter).HandleStatic(
                            url,
                            body,
                            sessionID,
                            wrapper.Output
                        ) as string;
                }

                matched = true;
            }
        }

        return matched;
    }

    protected class ResponseWrapper(string? output)
    {
        public string? Output { get; set; } = output;
    }
}
