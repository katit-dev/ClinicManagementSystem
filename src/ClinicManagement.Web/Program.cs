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
// LOCAL STORAGE
// =====================================================
builder.Services.AddLocalStorageServices();


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
// AUTHORIZATION
// =====================================================

builder.Services.AddAuthorizationCore();


// =====================================================
// AUTHENTICATION STATE PROVIDER
// =====================================================

builder.Services.AddScoped<
    CustomAuthenticationStateProvider>();

builder.Services.AddScoped<
    AuthenticationStateProvider>(
        provider =>
            provider.GetRequiredService<
                CustomAuthenticationStateProvider>()
    );

// =====================================================
// DI STATE SERVICE
// =====================================================
builder.Services.AddScoped<UserStateService>();


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