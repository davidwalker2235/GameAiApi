using GameAiApi.Data;
using GameAiApi.Options;
using GameAiApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Temporalmente desactivado porque CORS se está gestionando desde Azure App Service.
// Para volver al comportamiento anterior, descomentar este bloque y el registro de CORS.
// var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.Configure<AzureFoundryOptions>(builder.Configuration.GetSection(AzureFoundryOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiChatService, AzureFoundryChatService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnection")));

// Temporalmente desactivado porque CORS se está gestionando desde Azure App Service.
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("FrontendCors", policy =>
//     {
//         if (allowedOrigins.Length == 0)
//         {
//             policy.AllowAnyOrigin()
//                 .AllowAnyHeader()
//                 .AllowAnyMethod();
//
//             return;
//         }
//
//         policy.WithOrigins(allowedOrigins)
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//     });
// });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
// Temporalmente desactivado porque CORS se está gestionando desde Azure App Service.
// app.UseCors("FrontendCors");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/backoffice-users", () => Results.Redirect("/backoffice-users.html", permanent: true));

app.Run();
