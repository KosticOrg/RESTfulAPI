using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESTfulWebAPI
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = 500;
            context.HttpContext.Response.ContentType = "application/json";
            context.Result = new ObjectResult(new { Error = context.Exception.Message});
            context.ExceptionHandled = true;       
        }
    }
}
