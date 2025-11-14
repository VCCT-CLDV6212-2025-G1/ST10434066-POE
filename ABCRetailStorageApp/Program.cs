//====================================================================================================================================================================
// Fay-yaad Ahmed Parker
// ST10434066
// Group 1

// References: List your references here
// Rick-Anderson (n.d.). Getting Started - EF Core. [online] learn.microsoft.com. Available at: https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli.
// GeeksforGeeks. (n.d.). SQL Tutorial. [online] Available at: https://www.geeksforgeeks.org/sql-tutorial/?ref=outind.
// W3Schools (2024). HTML Tutorial. [online] W3schools.com. Available at: https://www.w3schools.com/html/.
// OpenAI (2025). OpenAI. [online] OpenAI. Available at: https://openai.com/.
//===================================================================================================================================================================

using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ABCRetailStorageApp.Services;

namespace ABCRetailStorageApp
{
    public sealed class AzureStorageOptions
    {
        public string ConnectionString { get; set; } = "";
        public string TableNameCustomers { get; set; } = "";
        public string TableNameProducts { get; set; } = "";
        public string BlobContainer { get; set; } = "";
        public string QueueName { get; set; } = "";
        public string FileShare { get; set; } = "";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Azure Storage configuration
            builder.Services.Configure<AzureStorageOptions>(
                builder.Configuration.GetSection("AzureStorage"));

            // MVC
            builder.Services.AddControllersWithViews();

            // Tables 
            builder.Services.AddSingleton<TableStorageService>();

            // HttpClient for Azure Functions 
            builder.Services.AddHttpClient();

            // cart support 
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<CartService>();

            // Cookie authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Pipeline configuration
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Session must come before auth that uses it
            app.UseSession();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}