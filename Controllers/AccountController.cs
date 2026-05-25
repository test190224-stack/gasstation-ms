using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GasStationMS.Models;

namespace GasStationMS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Հաշիվը ժամանակավորապես արգելափակված է։ Խնդրում ենք փորձել ավելի ուշ։");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Սխալ էլ․ փոստ կամ գաղտնաբառ։");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied() => View();
    }

    // ============ ViewModels ============
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Էլ․ փոստի դաշտը պարտադիր է")]
        [EmailAddress(ErrorMessage = "Սխալ էլ․ փոստի ձևաչափ")]
        [Display(Name = "Էլ․ փոստ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Գաղտնաբառի դաշտը պարտադիր է")]
        [DataType(DataType.Password)]
        [Display(Name = "Գաղտնաբառ")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Հիշել ինձ")]
        public bool RememberMe { get; set; }
    }
}
