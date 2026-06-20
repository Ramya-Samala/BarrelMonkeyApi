using BarrelMonkeyApi.Middleware;
using BarrelMonkeyApi.Services;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);


// singleton so the in-memory cache is shared across requests 
// otherwise every request would reload from disk
builder.Services.AddSingleton<FileDataStore>();

builder.Services.AddControllers()
    .AddNewtonsoftJson(opts =>
    {
        // Serialize enums as strings and handle null values cleanly
        opts.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// OpenAPI / Swagger —  easy for exploring the API while developing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Barrel Monkey API",
        Version = "v1",
        Description = "A small RESTful service for managing barrels and the monkeys inside them.",
    });

    // Teaching Swagger about the API key header so we can authenticate in the UI
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "Provide your API key in this header.",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });

    // Include XML doc comments if the file is present (set <GenerateDocumentationFile> in csproj to enable)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

//  Port configuration 

builder.WebHost.UseUrls(
    builder.Configuration["Urls"] ?? "http://localhost:5080"
);

//  Build and configure the middleware pipeline

var app = builder.Build();

// Log every request + response status and duration
app.UseRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Barrel Monkey API v1");
    c.RoutePrefix = "swagger";  // available at /swagger
});

app.UseRouting();
app.MapControllers();

//  Startup banner — makes it easy to see at a glance that the app is running and where to access it

var urls = builder.Configuration["Urls"] ?? "http://localhost:5080";
app.Logger.LogInformation("==============================");
app.Logger.LogInformation("  Barrel Monkey API starting");
app.Logger.LogInformation("  Listening on: {Urls}", urls);
app.Logger.LogInformation("  Swagger UI:   {Urls}/swagger", urls);
app.Logger.LogInformation("  Health check: {Urls}/health", urls);
app.Logger.LogInformation("==============================");

app.Run();
