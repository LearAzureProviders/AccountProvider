using AccountProvider.Models;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace AccountProvider.Fuctions
{
    public class Verify
    {
        private readonly ILogger<Verify> _logger;
        private readonly UserManager<UserAccount> _userManager;

        public Verify(ILogger<Verify> logger, UserManager<UserAccount> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [Function("Verify")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string body = null!;

            try
            {
                body = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($" ERROR :: Verify.Run {ex.Message}");
                return new BadRequestResult();
            }


            if (!string.IsNullOrEmpty(body))
            {
                VerificationModel vm = null!;

                try
                {
                    vm = JsonConvert.DeserializeObject<VerificationModel>(body)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError($" ERROR :: Verify.JsonConvert.DeserializeObject<VerificationModel {ex.Message}");
                    return new BadRequestResult();
                }

                if(vm != null && !string.IsNullOrEmpty(vm.Email) && !string.IsNullOrEmpty(vm.VerificationCode))
                {
                    //verify code using VerificationProvider
                    using var _httpClient = new HttpClient();

                    var verificationPayload = new
                    {
                        vm.Email,
                        vm.VerificationCode
                    };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(verificationPayload), Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync("https://verifikationskodprovider.azurewebsites.net/api/verify", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var userAccount =await _userManager.FindByEmailAsync(vm.Email);
                        if (userAccount != null!)
                        {
                            userAccount.EmailConfirmed = true;
                            await _userManager.UpdateAsync(userAccount);

                            if(await _userManager.IsEmailConfirmedAsync(userAccount))
                            {
                                return new OkResult();
                            }


                        }
                    }
                }

            }
           
            return new UnauthorizedResult();
        }
    }
}
