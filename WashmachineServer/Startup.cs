using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;
using WashmachineServer.MessageHandling;
namespace WashmachineServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ///<summary>
            ///�������������� ������ �������������� ��� �������, � �������� �� ���������� �������������� ����������� � PostgreSQL
            ///</summary>
            ConnectToDB connectToDB = new ConnectToDB();
            List<long> lst = new List<long>();
            //lst = connectToDB.GetUserList();

            bool kek = connectToDB.IsUserExist(520105754);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddControllers();
            services.AddControllers().AddNewtonsoftJson();
            services.AddSingleton<IVkApi>(sp => {
                var api = new VkApi();
                api.Authorize(new ApiAuthParams { AccessToken = Configuration["Config:AccessToken"] });
                return api;
            });
            
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            //app.UseMvc();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
