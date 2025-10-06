// Fay-yaad Ahmed Parker
// ST10434066
// Group 1

// References: List your references here
// Rick-Anderson (n.d.). Getting Started - EF Core. [online] learn.microsoft.com. Available at: https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli.
//GeeksforGeeks. (n.d.). SQL Tutorial. [online] Available at: https://www.geeksforgeeks.org/sql-tutorial/?ref=outind.
//W3Schools (2024). HTML Tutorial. [online] W3schools.com. Available at: https://www.w3schools.com/html/.
//OpenAI (2025). OpenAI. [online] OpenAI. Available at: https://openai.com/.


using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            builder.Services.Configure<AzureStorageOptions>(
                builder.Configuration.GetSection("AzureStorage"));

            builder.Services.AddControllersWithViews();

            builder.Services.AddSingleton<ABCRetailStorageApp.Services.TableStorageService>();
            builder.Services.AddHttpClient();

            var app = builder.Build();

          
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

          
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}