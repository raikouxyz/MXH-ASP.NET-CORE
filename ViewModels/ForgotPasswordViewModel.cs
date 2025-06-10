using System.ComponentModel.DataAnnotations;

namespace MXH_ASP.NET_CORE.ViewModels
{
    /// <summary>
    /// ViewModel cho chức năng quên mật khẩu
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// ViewModel cho chức năng xác thực mã OTP
    /// </summary>
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã xác thực")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực phải có 6 ký tự")]
        [Display(Name = "Mã xác thực")]
        public string OtpCode { get; set; }
    }

    /// <summary>
    /// ViewModel cho chức năng đặt lại mật khẩu
    /// </summary>
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }
    }
} 