using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            //var httpContext = context.GetHttpContext();

            //// فقط کاربران احراز هویت شده می‌توانند به Dashboard دسترسی داشته باشند
            //return httpContext.User.Identity.IsAuthenticated;

            // برخی نسخه‌ها ممکن است این ویژگی را داشته باشند
            if (context is AspNetCoreDashboardContext aspNetContext)
            {
                var httpContext = aspNetContext.HttpContext;
                return httpContext.User.Identity.IsAuthenticated;
            }

            return false;

        }
    }
}
