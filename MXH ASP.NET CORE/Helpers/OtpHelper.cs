using System.Collections.Concurrent;

namespace MXH_ASP.NET_CORE.Helpers
{
    /// <summary>
    /// Lớp giúp quản lý mã OTP
    /// </summary>
    public static class OtpHelper
    {
        // Lưu trữ mã OTP và thời gian hết hạn theo số điện thoại
        private static readonly ConcurrentDictionary<string, OtpInfo> _otpStore = new();

        // Thời gian hết hạn mặc định của mã OTP (phút)
        private const int OTP_EXPIRY_MINUTES = 5;

        /// <summary>
        /// Tạo mã OTP ngẫu nhiên 6 chữ số
        /// </summary>
        public static string GenerateOtp()
        {
            // Tạo mã OTP 6 chữ số ngẫu nhiên
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Lưu mã OTP cho số điện thoại
        /// </summary>
        public static void SaveOtp(string phoneNumber, string otpCode)
        {
            var otpInfo = new OtpInfo
            {
                Code = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES)
            };

            _otpStore.AddOrUpdate(phoneNumber, otpInfo, (key, oldValue) => otpInfo);
        }

        /// <summary>
        /// Kiểm tra mã OTP có hợp lệ không
        /// </summary>
        public static bool VerifyOtp(string phoneNumber, string otpCode)
        {
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(otpCode))
                return false;

            if (_otpStore.TryGetValue(phoneNumber, out var otpInfo))
            {
                // Kiểm tra thời gian hết hạn
                if (otpInfo.ExpiryTime >= DateTime.UtcNow)
                {
                    // Kiểm tra mã OTP
                    bool isValid = otpInfo.Code == otpCode;
                    
                    // Nếu xác thực thành công, xóa mã OTP để không dùng lại được
                    if (isValid)
                    {
                        _otpStore.TryRemove(phoneNumber, out _);
                    }
                    
                    return isValid;
                }
                else
                {
                    // Mã OTP đã hết hạn, xóa khỏi store
                    _otpStore.TryRemove(phoneNumber, out _);
                }
            }

            return false;
        }

        /// <summary>
        /// Thông tin về mã OTP
        /// </summary>
        private class OtpInfo
        {
            /// <summary>
            /// Mã OTP
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// Thời gian hết hạn
            /// </summary>
            public DateTime ExpiryTime { get; set; }
        }
    }
} 