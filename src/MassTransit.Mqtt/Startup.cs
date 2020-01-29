using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MassTransit.Mqtt.Configuration;
using MassTransit.Mqtt.MessageQueue.Consumers;
using MassTransit.Mqtt.MessageQueue.Messages;
using MassTransit.Mqtt.MessageQueue.Serialisation;
using MassTransit.Mqtt.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MassTransit.Mqtt
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
      // MassTransit settings
      var mbs = Configuration.GetSection("MessageBus");
      services.Configure<MessageBusSettings>(mbs);
      var busSettings = mbs.Get<MessageBusSettings>();

      services.AddMassTransit(x =>
      {
        x.AddConsumer<MqttMessageConsumer>();

        x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
          var host = cfg.Host(new Uri(busSettings.Url), h =>
          {
            h.Username(busSettings.Username);
            h.Password(busSettings.Password);
          });

          cfg.ClearMessageDeserializers();

          var deserializer = JsonSerializer.CreateDefault();
          var jsonContent = new ContentType("application/json");
          cfg.AddMessageDeserializer(jsonContent, () => new RawJsonMessageDeserializer(jsonContent, deserializer));
          var mstJsonContent = new ContentType("application/vnd.masstransit+json");
          cfg.AddMessageDeserializer(mstJsonContent, () => new RawJsonMessageDeserializer(mstJsonContent, deserializer));

          cfg.SetLoggerFactory(provider.GetService<ILoggerFactory>());

          cfg.ReceiveEndpoint("masstransit.mqttconsumer", e =>
          {
            e.Bind("masstransit.mqtt", x =>
            {
              x.ExchangeType = ExchangeType.Topic;

              // route all exchange messages to our queue
              x.RoutingKey = "#";
            });

            e.ConfigureConsumer<MqttMessageConsumer>(provider);
            EndpointConvention.Map<MqttMessage>(e.InputAddress);
          });
        }));
      });
      services.AddSingleton<IHostedService, BusService>();

      services.AddControllersWithViews();
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
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
      });
    }
  }
}
