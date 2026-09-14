var builder = WebApplication.CreateBuilder(args);


// =====================================================
// RAZOR PAGE
// =====================================================

builder.Services.AddRazorPages();


// =====================================================
// BLAZOR SERVER
// =====================================================

builder.Services.AddServerSideBlazor();


// =====================================================
// HTTP CLIENT
// Frontend -> Backend API
// =====================================================

builder.Services.AddHttpClient("ClinicApi", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];

    client.BaseAddress = new Uri(baseUrl!);
});


// =====================================================
// OPEN API
// =====================================================

builder.Services.AddOpenApi();

var app = builder.Build();


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();

app.MapFallbackToPage("/_Host");

app.Run();