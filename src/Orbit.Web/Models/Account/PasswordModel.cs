using System.ComponentModel.DataAnnotations;

namespace Orbit.Web.Models.Account;

internal class PasswordModel
{
	[Required(ErrorMessage = "Mevcut şifre zorunludur.")]
	public string CurrentPassword { get; set; } = string.Empty;

	[Required(ErrorMessage = "Yeni şifre zorunludur.")]
	[MinLength(6, ErrorMessage = "En az 6 karakter olmalıdır.")]
	public string NewPassword { get; set; } = string.Empty;

	[Required(ErrorMessage = "Yeni şifre tekrar zorunludur.")]
	public string ConfirmNewPassword { get; set; } = string.Empty;
}
