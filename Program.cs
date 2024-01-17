using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using sm_coding_challenge.Services.DataProvider;
using System;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// dee : We want to use NewtonsoftJson as the default JSON converter,
// that way The JSON sent out by API will reflect derived classes and not just base classes
builder.Services.AddControllersWithViews().AddNewtonsoftJson(o =>
{
    o.SerializerSettings.Converters.Add(new StringEnumConverter
    {
        //CamelCaseText = true,//absolete
        NamingStrategy = new CamelCaseNamingStrategy()
    });

});


builder.Services.AddSingleton<IDataProvider, DataProviderImpl>();

// dee: adding Polly HttpClient To implement http backoff with exponential backoff
builder.Services.AddHttpClient<IDataProvider, DataProviderImpl>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
        ;

// dee : Using in Memory Cache to optimize fetching data Set
// For prod we will probably use Redis for the cache
// builder.Services.AddStackExchangeRedisCache()
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();
IWebHostEnvironment env = app.Environment;
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
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                    retryAttempt)));
}
