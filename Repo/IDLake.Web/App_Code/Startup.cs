using Microsoft.Owin;
using Owin;

namespace IDLake.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
