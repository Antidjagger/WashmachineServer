using System;
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
            ///Нижепреведённые вызовы использовались для отладки, к удалению по завершении проектирования подключения к PostgreSQL
            ///</summary>
            ConnectToDB connectToDB = new ConnectToDB(configuration);
            //lst = connectToDB.GetUserList();
            

            connectToDB.MainLogWriting("Server started");
        }
        /// <summary>
        /// //
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="file"></param>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
       

        /// <summary>
        /// ///
        /// </summary>
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddControllers();
            services.AddControllers().AddNewtonsoftJson();
            string AT;
            switch (Configuration["ConnectionToApi:TakeATFromConfig"])
            {
                case "true":
                    AT = Configuration["ConnectionToApi:AccessToken"];
                    break;
                case "false":
                    AT = Environment.GetEnvironmentVariable(Configuration["ConnectionToApi:EnvironmentAT"]);
                    break;
                default:
                    //Здесь нужно реализовать запись в локальный лог-файл
                    AT = null;
                    break;
            }
            services.AddSingleton<IVkApi>(sp => {
                var api = new VkApi();
                api.Authorize(new ApiAuthParams { AccessToken = AT });
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
