using AccountProvider.Models;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace AccountProvider.Fuctions
{
    public class SignUp(ILogger<SignUp> logger, UserManager<UserAccount> userManager)
    {
        private readonly ILogger<SignUp> _logger = logger;
        private readonly UserManager<UserAccount> _userManager = userManager;
   

        [Function("SignUp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            string body = null!;

            try
            {
                body = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($" ERROR : SignUp.StreamReader :: {ex.Message}");
            }           
            
            if (body != null)
            {
                UserRegistrationModel urr = null!;
                try
                {
                    urr = JsonConvert.DeserializeObject<UserRegistrationModel>(body)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError($" ERROR : Signup.JsonConvert.DeserializeObject<UserRegistrationModel> :: {ex.Message}");
                }

               

                if (urr != null && !string.IsNullOrEmpty(urr.Email) && !string.IsNullOrEmpty(urr.Password))
                {
                    using var _httpClient = new HttpClient();

                    if (!await _userManager.Users.AnyAsync(x => x.Email == urr.Email ))
                    {
                        var userAccount = new UserAccount
                        {
                            FirstName = urr.FirstName,
                            LastName = urr.LastName,
                            Email = urr.Email,
                            UserName = urr.Email

                        };
                        var result = await _userManager.CreateAsync(userAccount, urr.Password);
                        if (result.Succeeded)
                        {
                            //SEND VERIFICATION (Function) CODE
                            var verificationPayload = new
                            {
                                urr.Email
                            };
                            var content = new StringContent(JsonConvert.SerializeObject(verificationPayload), Encoding.UTF8, "application/json");
                            var response = await _httpClient.PostAsync("https://verifikationskodprovider.azurewebsites.net/api/Lear?code=udnx8pP9X1REkr6V1UR6FD0C0s3E4r9qss4mnEE58UYZAzFun5VZJg%3D%3D", content);

                            if (response.IsSuccessStatusCode)
                            {
                                return new OkResult();
                            }
                            else
                            {
                                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                            }
                        }
                    }
                    else
                    {
                        return new ConflictResult();
                    }
                }
                
            }

            return new BadRequestResult();
        }
    }
}
