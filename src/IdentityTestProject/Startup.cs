using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenIddict;
using OpenIddict.Core;
using OpenIddict.Models;

namespace IdentityTestProject
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("Local"));

                options.UseOpenIddict<int>();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("Application",
                    b => b.AllowAnyOrigin().AllowAnyMethod());
            });

            services.AddAuthentication(options => {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext, int>()
                .AddDefaultTokenProviders();

            services.AddOpenIddict<int>()
                .AddEntityFrameworkCoreStores<ApplicationDbContext>()
                .AddMvcBinders()
                .EnableAuthorizationEndpoint("/Connect/Authorize")
                .EnableTokenEndpoint("/Connect/Token")
                .EnableLogoutEndpoint("/Connect/Logout")
                .EnableUserinfoEndpoint("/Api/UserInfo")
                .AllowAuthorizationCodeFlow()
                .AllowPasswordFlow()
                .AllowRefreshTokenFlow()
                .EnableRequestCaching()
                .DisableHttpsRequirement();
        }  

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }

            app.UseStaticFiles();

            app.UseCors(options =>
            {
                options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });

            app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/Api"), branch =>
            {
                branch.UseStatusCodePagesWithReExecute("/Error");
                branch.UseIdentity();
            });

            app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/Api"), branch =>
            {
                branch.UseOAuthValidation();
            });

            app.UseOpenIddict();
            var openIddictOptions = app.ApplicationServices.GetRequiredService<IOptions<OpenIddictOptions>>().Value;
            //openIddictOptions.Provider.OnSerializeAccessToken = context =>
            //{
            //    return Task.FromResult(0);
            //};

            app.UseMvcWithDefaultRoute();

            InitializeAsync(app.ApplicationServices, CancellationToken.None).GetAwaiter().GetResult();                      
        }

        private async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();

                var manager = services.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication<int>>>();

                if (await manager.FindByClientIdAsync("mvc", cancellationToken) == null)
                {
                    var application = new OpenIddictApplication<int>
                    {
                        ClientId = "mvc",
                        DisplayName = "MVC Client Application",
                        LogoutRedirectUri = "http://localhost:52191/",
                        RedirectUri = "http://localhost:52191/signin-oidc"
                    };

                    await manager.CreateAsync(application, "901564A5-E7FE-42CB-B10D-61EF6A8F3654", cancellationToken);
                }

                if (await manager.FindByClientIdAsync("postman", cancellationToken) == null)
                {
                    var application = new OpenIddictApplication<int>
                    {
                        ClientId = "postman",
                        DisplayName = "Postman",
                        RedirectUri = "https://www.getpostman.com/oauth2/callback"
                    };

                    await manager.CreateAsync(application, cancellationToken);
                }

                if (await manager.FindByClientIdAsync("angular2", cancellationToken) == null)
                {
                    var application = new OpenIddictApplication<int>
                    {
                        ClientId = "angular2",
                        DisplayName = "Angular2",
                        RedirectUri = "http://localhost:52323",
                        LogoutRedirectUri = "http://localhost:52323"
                    };

                    await manager.CreateAsync(application, cancellationToken);
                }
            }
        } 
    }
}
