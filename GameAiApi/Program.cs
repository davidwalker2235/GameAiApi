using GameAiApi.Data;
using GameAiApi.Options;
using GameAiApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureFoundryOptions>(builder.Configuration.GetSection(AzureFoundryOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiChatService, AzureFoundryChatService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/backoffice-users", () => Results.Redirect("/backoffice-users.html", permanent: true));

app.Run();
