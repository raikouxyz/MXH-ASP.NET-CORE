using System.Threading.Tasks;

namespace MXH_ASP.NET_CORE.Services
{
    /// <summary>
    /// Interface định nghĩa các phương thức gửi SMS
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// Gửi SMS đến số điện thoại được chỉ định
        /// </summary>
        /// <param name="phoneNumber">Số điện thoại người nhận</param>
        /// <param name="message">Nội dung tin nhắn</param>
        /// <returns>Task đại diện cho quá trình gửi SMS</returns>
        Task SendSmsAsync(string phoneNumber, string message);
    }

    /// <summary>
    /// Class thực thi việc gửi SMS
    /// </summary>
    public class SmsSender : ISmsSender
    {
        private readonly ILogger<SmsSender> _logger;

        public SmsSender(ILogger<SmsSender> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gửi SMS đến số điện thoại được chỉ định
        /// </summary>
        /// <param name="phoneNumber">Số điện thoại người nhận</param>
        /// <param name="message">Nội dung tin nhắn</param>
        /// <returns>Task đại diện cho quá trình gửi SMS</returns>
        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            // TODO: Thay thế bằng dịch vụ SMS thực tế
            // Hiện tại chỉ log thông tin để test
            _logger.LogInformation($"Sending SMS to {phoneNumber}: {message}");
            
            // Giả lập độ trễ của việc gửi SMS
            await Task.Delay(1000);
        }
    }
} 