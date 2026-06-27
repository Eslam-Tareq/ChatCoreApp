using ChatCoreApp.Hubs;
using ChatCoreApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatCoreApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string CorsPolicy = "myPolicy";
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddCors(opt => opt.AddPolicy(CorsPolicy,
                 p => {
                     
                     p.AllowAnyHeader();
                     p.AllowAnyOrigin();
                     p.AllowAnyMethod();
            
                        }
            )

                );
            builder.Services.AddDbContext<ChatCoreContext>(
                opt=>opt.UseSqlServer(builder.Configuration.GetConnectionString("ChatCoreConnection"))
                );

            builder.Services.AddSignalR();

            builder.Services
                        .AddIdentity<ApplicationUser, IdentityRole>()
                        .AddEntityFrameworkStores<ChatCoreContext>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(CorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHub<ChatHub>("/MyChat");
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Chat}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
