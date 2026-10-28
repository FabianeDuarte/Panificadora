using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Data;
using PanificadoraServidor.Endpoints;
using PanificadoraServidor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Padrao") ?? "Data Source=panificadora.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();          // cria o banco SQLite na primeira execução
    Seed.Executar(db);                    // dados de exemplo
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthApi();                         // exige login em /api/admin, /api/app e /api/auth
app.MapAuth();                            // /api/auth/* e /api/admin/usuarios
app.MapAdmin();                           // /api/admin/*  (site da distribuidora)
app.MapApp();                             // /api/app/*    (aplicativo MAUI do vendedor)
app.Run();
