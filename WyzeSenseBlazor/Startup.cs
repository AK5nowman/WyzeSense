using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using WyzeSenseBlazor.DataServices;
using WyzeSenseBlazor.DataStorage;
using WyzeSenseCore;
using WyzeSenseBlazor.Settings;

namespace WyzeSenseBlazor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            MapConfiguration();
        }
        public IConfiguration Configuration { get; }

        private void MapConfiguration()
        {
            BrokerHostSettings brokerHostSettings = new();
            Configuration.GetSection(nameof(BrokerHostSettings)).Bind(brokerHostSettings);
            AppSettingsProvider.BrokerHostSettings = brokerHostSettings;

            ClientSettings clientSettings = new();
            Configuration.GetSection(nameof(ClientSettings)).Bind(clientSettings);
            AppSettingsProvider.ClientSettings = clientSettings;

            WyzeSettings wyzeSettings = new();
            Configuration.GetSection(nameof(WyzeSettings)).Bind(wyzeSettings);
            AppSettingsProvider.WyzeSettings = wyzeSettings;
            Console.WriteLine("Dongle path: " + wyzeSettings.UsbPath);

            DatabaseSettings databaseSettings = new();
            Configuration.GetSection(nameof(DatabaseSettings)).Bind(databaseSettings);
            AppSettingsProvider.DatabaseSettings = databaseSettings;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddAntDesign();
            services.AddMqttClientHostedService();


            services.AddSingleton<IDataStoreOptions>(new DataStoreOptions()
            {
                Path = AppSettingsProvider.DatabaseSettings.DatabasePath
            });
            services.AddSingleton<IDataStoreService, DataStoreService>();

            services.AddSingleton<IWyzeSenseLogger, WyzeLogger>();
            services.AddSingleton<IWyzeDongle, WyzeDongle>();
            services.AddHostedService<WyzeSenseService>();
            services.AddSingleton<IWyzeSenseService, WyzeSenseService>();

            services.AddScoped<IMQTTTemplateService, MQTTTemplateService>();
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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
