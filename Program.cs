using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zenko.Services; // Agregar para que reconozca tus servicios

var builder = WebApplication.CreateBuilder(args);

// Registramos MVC
builder.Services.AddControllersWithViews();

// Registramos los servicios para inyección de dependencias
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<CalculoService>();

var app = builder.Build();

// Inicializar tipos de insumo base en la BD
BD.InicializarTiposInsumo();

// Middleware básico
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
