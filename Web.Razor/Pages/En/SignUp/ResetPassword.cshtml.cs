using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Base.Accounts.Helpers;
using Server.Base.Core.Configs;
using Server.Base.Core.Services;
using Server.Base.Database.Accounts;
using System.ComponentModel.DataAnnotations;
using Web.Razor.Services;

namespace Web.Razor.Pages.En.SignUp;

public class ResetPasswordModel(InternalRwConfig iConfig, AccountHandler aHandler,
    PasswordHasher hasher, TemporaryDataStorage tempStorage) : PageModel
{
    [Required(ErrorMessage = "Please Enter Password")]
    [StringLength(15, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Please Enter Confirm Password")]
    [StringLength(15, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    public async Task<IActionResult> OnGet([FromRoute] string id)
    {
        var account = tempStorage.GetData<AccountDbEntry>(id);

        if (account == null)
        {
            await EmailService.Delay();
            return RedirectToPage("ResetPasswordInvalid");
        }

        ViewData["ServerName"] = iConfig.ServerName;

        return Page();
    }

    public async Task<IActionResult> OnPost([FromRoute] string id)
    {
        if (!ModelState.IsValid)
            return Page();

        var account = tempStorage.GetData<AccountDbEntry>(id);

        if (account == null)
        {
            await EmailService.Delay();
            return RedirectToPage("ResetPasswordInvalid");
        }

        if (string.IsNullOrEmpty(Password))
        {
            return Page();
        }

        if (ConfirmPassword != Password)
        {
            return Page();
        }

        var newAccount = aHandler.GetAccountFromEmail(account.Email);
        newAccount.Write.Password = hasher.GetPassword(newAccount.Username, Password);
        aHandler.Update(newAccount.Write);

        tempStorage.RemoveData(id, account);

        return RedirectToPage("ResetPasswordSuccessful");
    }
}
