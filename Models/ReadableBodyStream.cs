using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;


namespace chesterBackendNet31.Models
{

    //todo hapus, gaguna
    public class ReadableBodyStreamAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // For ASP.NET 3.1
            context.HttpContext.Request.EnableBuffering();
        }
    }
}
