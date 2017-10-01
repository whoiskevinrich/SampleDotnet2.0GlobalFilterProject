using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleDotnet2._0GlobalFilterProject
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            // bind the "AzureAd" Section in appsettings.json to my OpenIdConnectOptions
            .AddAzureAd(options => Configuration.Bind("AzureAd", options))
            .AddJwtBearer(options =>
                {
                    options.Audience = Configuration["JwtBearer.Audience"];
                    options.Authority = Configuration["JwtBearer.Authority"];
                } )
            // configure Cookie auth 
            .AddCookie(options =>
                {
                    options.LoginPath = "/Account/SignIn";
                    options.LogoutPath = "/Account/SignOut";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = OnRedirectToLogin
                    };
                });

            // authorization
            services.AddAuthorization(options =>
            {
                // require user to have cookie auth or jwt bearer token
                options.AddPolicy("Authenticated",
                    policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser());
            });

            // Add global authorization policy
            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new AuthorizeFilter("Authenticated"));
            });

            services.AddMvc();
        }

        private static Task OnRedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // return 401 if not "logged in" from an API Call
                context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                return Task.CompletedTask;
            }

            // Redirect users to login page
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
