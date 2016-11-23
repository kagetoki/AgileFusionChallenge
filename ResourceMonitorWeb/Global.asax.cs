﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ResourceMonitorWeb
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
			ProcessMonitor.Monitor.Start();
        }

		protected void Application_End()
		{
			ProcessMonitor.Monitor.Stop();
		}
    }
}
