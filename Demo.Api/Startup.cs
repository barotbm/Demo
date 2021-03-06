using System;
using Demo.Api.Extensions;
using Demo.Common.Filters;
using Demo.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Api
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
            // service dependencies
            services.ConfigureDemoServices(Configuration);

            services.ConfigureApiVersioning();

            services.ConfigureCors();

            services.ConfigureSwagger();

            services.AddControllers(options =>
                {
                    //options.ReturnHttpNotAcceptable = true;
                    options.Filters.Add(typeof(TrackActionPerformanceFilter));
                })
                //.AddXmlDataContractSerializerFormatters()
                .AddNewtonsoftJson();
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
                app.UseApiExceptionHandler(options =>
                {
                    options.AddResponseDetails = UpdateApiErrorResponse;
                    options.DetermineLogLevel = DetermineLogLevel;
                });
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "v1.0");
                //options.SwaggerEndpoint("/swagger/v2/swagger.json", "v2"); // Future Version
                options.RoutePrefix = "swagger";
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static LogLevel DetermineLogLevel(Exception ex)
        {
            if (ex.Message.StartsWith("cannot open database", StringComparison.InvariantCultureIgnoreCase) ||
                ex.Message.StartsWith("a network-related", StringComparison.InvariantCultureIgnoreCase))
            {
                return LogLevel.Critical;
            }

            return LogLevel.Error;
        }

        private void UpdateApiErrorResponse(HttpContext context, Exception ex, ApiError apiError)
        {
            if (ex.GetType().Name == nameof(SqlException))
            {
                apiError.Detail = "Exception was a database exception!";
            }

            //apiError.Links = "https://gethelpformyerror.com/";
        }
    }
}