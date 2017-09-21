using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace IBISGlossaryWebAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "ActionApi",
                routeTemplate: "api/{controller}/{action}/{param}",
                defaults: new { controller = "GlossaryAPI", term = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "AddItem",
                routeTemplate: "api/{controller}/{action}/{definition}/{term}",
                defaults: new { controller = "GlossaryAPI", term = "", definition = "" }
            );

            config.Routes.MapHttpRoute(
                name: "ModItem",
                routeTemplate: "api/{controller}/{action}/{definition}/{id}/{term}",
                defaults: new { controller = "GlossaryAPI", term = "", id = "", definition = "" }
            );
        }
    }
}
