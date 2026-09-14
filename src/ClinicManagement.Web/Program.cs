var builder = WebApplication.CreateBuilder(args);


// =====================================================
// RAZOR PAGE
// Dùng cho _Host.cshtml
// =====================================================

builder.Services.AddRazorPages();


// =====================================================
// BLAZOR SERVER
// Cho phép sử dụng component .razor
// =====================================================

builder.Services.AddServerSideBlazor();


// =====================================================
// OPEN API
// Có thể giữ lại vì project được tạo từ webapi
// =====================================================

builder.Services.AddOpenApi();


var app = builder.Build();


// =====================================================
// STATIC FILE
// Cho phép đọc:
// css
// js
// image
// bootstrap...
// =====================================================

app.UseStaticFiles();


// =====================================================
// ROUTING
// =====================================================

app.UseRouting();


// =====================================================
// BLAZOR HUB
//
// Browser
//    ↕ SignalR
// Blazor Server
// =====================================================

app.MapBlazorHub();


// =====================================================
// Nếu URL không match endpoint khác
// thì đưa về _Host.cshtml
// =====================================================

app.MapFallbackToPage("/_Host");


app.Run();