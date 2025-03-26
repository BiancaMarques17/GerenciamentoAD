
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Adicionar configuração do appsettings.json
builder.Services.Configure<ConfigurationManager>(builder.Configuration);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();






builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500") // Substitua pelo domínio correto
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

});


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");// Ativar CORS
//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
