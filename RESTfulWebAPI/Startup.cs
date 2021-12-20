using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace RESTfulWebAPI
{
    public class Startup
    {
        private readonly IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                    .AddXmlSerializerFormatters()
                    .AddXmlDataContractSerializerFormatters();
                    
            services.AddCors(o => o.AddPolicy("MyPolicy",builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader(); 
            }));

            services.AddDbContext<WebAPIModel>(options => 
               options.UseSqlServer(Configuration.GetConnectionString("WebAPIDatabase")));

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<WebAPIModel>()
                .AddDefaultTokenProviders();

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this-is-my-secret-key"));

            var tokenValidationParameter = new TokenValidationParameters()
            {
                IssuerSigningKey = signingKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(x => x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(token => token.TokenValidationParameters = tokenValidationParameter);

            services.AddSwaggerDocument(config =>
            {
                config.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "My Web API";
                    document.Info.Description = "A Simple ASP.NET Core Web API";                   
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "Haidelberg",
                        Email = "contact@haidelberg.com",
                        Url = "https://www.ibmhs.eu"
                    };                   
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();           
            else
                app.UseExceptionHandler(
                    o => 
                    {
                        o.Run(async context =>
                        {
                            context.Response.StatusCode = 500;
                            context.Response.ContentType = "application/json";
                            //await context.Response.WriteAsync("Something went wrong !!! We are working on Problem");
                            var exception = context.Features.Get<IExceptionHandlerFeature>();
                            if (exception != null)
                                await context.Response.WriteAsync(string.Format("Error : {0}",exception.Error.Message));
                        });
                    }            
                );

            app.UseRouting();

            app.UseCors("MyPolicy");

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseOpenApi();

            app.UseSwaggerUi3();
        }
    }
}
