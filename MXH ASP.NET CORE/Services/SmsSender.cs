using System.Diagnostics;

namespace MXH_ASP.NET_CORE.Services
{
    /// <summary>
    /// Interface xử lý gửi SMS
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// Gửi SMS đến số điện thoại
        /// </summary>
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }

    /// <summary>
    /// Implement giả lập gửi SMS
    /// Trong môi trường thực tế, cần thay thế bằng dịch vụ SMS thật (Twilio, Vonage, etc.)
    /// </summary>
    public class FakeSmsSender : ISmsSender
    {
        private readonly ILogger<FakeSmsSender> _logger;

        public FakeSmsSender(ILogger<FakeSmsSender> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Giả lập gửi SMS bằng cách ghi log
        /// </summary>
        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            // Log thông tin SMS
            _logger.LogInformation($"FAKE SMS to {phoneNumber}: {message}");
            
            // Hiển thị thông tin trong Output window khi chạy debug
            Debug.WriteLine($"FAKE SMS to {phoneNumber}: {message}");
            
            // Mô phỏng độ trễ khi gửi SMS
            return Task.FromResult(true);
        }
    }
} 