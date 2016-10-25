
[assembly: Microsoft.Owin.OwinStartup(typeof(AdMaiora.Listy.Api.Startup))]

namespace AdMaiora.Listy.Api
{
    using Microsoft.Owin;
    using Owin;


    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureMobileApp(app);
        }
    }
}