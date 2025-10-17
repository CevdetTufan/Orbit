using System.ComponentModel.DataAnnotations;

namespace Orbit.Web.Models.Account;

internal class EmailModel
{
	[Required(ErrorMessage = "E‑posta zorunludur.")]
	[EmailAddress(ErrorMessage = "Geçerli bir e‑posta giriniz.")]
	public string NewEmail { get; set; } = string.Empty;
}
