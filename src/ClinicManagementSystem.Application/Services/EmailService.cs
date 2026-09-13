using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ClinicManagementSystem.Application.Services;

public interface IEmailService
{
    Task SendPasswordResetOtpAsync(
        string recipientEmail,
        string otp);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetOtpAsync(
        string recipientEmail,
        string otp)
    {
        // 1. Đọc cấu hình SMTP
        string smtpHost =
            _configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException(
                "SMTP Host is not configured.");

        int smtpPort =
            _configuration.GetValue<int>("Email:SmtpPort");

        string username =
            _configuration["Email:Username"]
            ?? throw new InvalidOperationException(
                "SMTP Username is not configured.");

        string password =
            _configuration["Email:Password"]
            ?? throw new InvalidOperationException(
                "SMTP Password is not configured.");

        string senderEmail =
            _configuration["Email:SenderEmail"]
            ?? username;

        string senderName =
            _configuration["Email:SenderName"]
            ?? "Clinic Management System";

        // 2. Tạo email
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                senderName,
                senderEmail));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject =
            "Mã OTP đặt lại mật khẩu";

        message.Body = new TextPart("plain")
        {
            Text =
$"""
Xin chào,

Mã OTP đặt lại mật khẩu của bạn là:

{otp}

Mã OTP có hiệu lực trong 5 phút.

Nếu bạn không yêu cầu đặt lại mật khẩu,
vui lòng bỏ qua email này.

Clinic Management System
"""
        };

        // 3. Kết nối SMTP
        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            smtpHost,
            smtpPort,
            SecureSocketOptions.StartTls);

        // 4. Đăng nhập Gmail SMTP
        await smtpClient.AuthenticateAsync(
            username,
            password);

        // 5. Gửi email
        await smtpClient.SendAsync(message);

        // 6. Ngắt kết nối
        await smtpClient.DisconnectAsync(true);
    }
}