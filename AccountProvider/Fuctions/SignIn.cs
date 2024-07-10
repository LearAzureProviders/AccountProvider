using AccountProvider.Models;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AccountProvider.Fuctions
{
    public class SignIn
    {
        private readonly ILogger<SignIn> _logger;
        private readonly SignInManager<UserAccount> _signInManager;
        private readonly HttpClient _httpClient;

        public SignIn(ILogger<SignIn> logger, UserManager<UserAccount> userManager, HttpClient httpClient, SignInManager<UserAccount> signInManager)
        {
            _logger = logger;
            _httpClient = httpClient;
            _signInManager = signInManager;
        }

        [Function("SignIn")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string body = null!;

            try
            {
                body = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($" ERROR : SignIn.Run :: {ex.Message}");
            }
            
            if (body != null)
            {
                UserLoginModel ulr = null!;
                try
                {
                    ulr = JsonConvert.DeserializeObject<UserLoginModel>(body)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError($" ERROR : SignIn.JsonConvert.DeserializeObject<UserLoginModel> :: {ex.Message}");
                }


                if(ulr != null && !string.IsNullOrEmpty(ulr.Email) && !string.IsNullOrEmpty(ulr.Password))
                {
                    try
                    {
                        var result = await _signInManager.PasswordSignInAsync(ulr.Email, ulr.Password, ulr.IsPersistent, false);
                        if (result.Succeeded)
                        {

                            //Get token from Token provider

                            return new OkObjectResult("accestoken");
                        }
                        return new UnauthorizedResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($" ERROR : SignIn.await _signInManager.PasswordSignInAsync :: {ex.Message}");
                    }
                }



            }



            return new BadRequestResult();
        }
    }
}
