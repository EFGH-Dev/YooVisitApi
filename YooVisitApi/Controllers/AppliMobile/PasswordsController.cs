using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using YooVisitApi.Dtos.Passwords;
using YooVisitApi.Models.UserModel;

namespace YooVisitApi.Controllers.AppliMobile
{
    [ApiController]
    [Route("api/[Controller]")]
    public class PasswordsController : ControllerBase
    {
        private readonly UserManager<UserApplication> _userManager;
        private readonly IEmailSender _emailSender;

        public PasswordsController(UserManager<UserApplication> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // On ne divulgue jamais si l'email existe pour éviter l'énumération d'utilisateurs
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return Ok("Si un compte correspondant à cet email existe, un lien de réinitialisation a été envoyé.");
            }

            // Génération du token et encodage pour URL
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var safeToken = WebUtility.UrlEncode(token);

            // TODO: adapter l'URL de reset (deep link / site web)
            var resetLink = $"https://yoovisit.app/reset-password?email={WebUtility.UrlEncode(user.Email)}&token={safeToken}";

            // Envoi de l'email via le service injecté
            await _emailSender.SendEmailAsync(
                user.Email,
                "YooVisit - Réinitialisation de votre mot de passe",
                $"<p>Pour réinitialiser votre mot de passe, <a href='{resetLink}'>cliquez ici</a>.</p>"
            );

            return Ok("Si un compte correspondant à cet email existe, un lien de réinitialisation a été envoyé.");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                // C'est ici que la magie opère :
                // La validation [Compare("NewPassword")] du DTO est gérée
                // automatiquement. Si les mots de passe ne matchent pas,
                // ça s'arrête ici.
                return BadRequest(ModelState);
            }

            // On utilise le typage fort, comme tu l'aimes.
            UserApplication user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // FAILSAFE : Toujours rester vague.
                // On ne dit pas "Utilisateur trouvé, mais token invalide".
                // On dit juste que l'opération a échoué.
                return BadRequest("Erreur lors de la réinitialisation. Veuillez réessayer.");
            }

            // --- LE POINT CRUCIAL ---
            // Le token a été encodé (UrlEncode) pour être mis dans un lien.
            // On doit le décoder avant de le donner à Identity.
            string decodedToken = WebUtility.UrlDecode(dto.Token);

            // C'est le "boss battle" : Identity vérifie le token,
            // son expiration, ET change le mot de passe en une seule étape.
            IdentityResult result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

            if (result.Succeeded)
            {
                // Achievement Unlocked: Sécurité Restaurée
                return Ok("Votre mot de passe a été réinitialisé avec succès.");
            }

            // Échec : Le token était invalide, expiré, ou déjà utilisé.
            // (On peut logger les 'result.Errors' côté serveur pour le debug)
            return BadRequest("Token invalide ou expiré. Veuillez refaire une demande.");
        }
    }
}
