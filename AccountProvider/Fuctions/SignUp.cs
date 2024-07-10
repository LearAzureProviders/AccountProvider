//using AccountProvider.Models;
//using Data.Entities;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using System.Text;

//namespace AccountProvider.Fuctions
//{
//    public class SignUp(ILogger<SignUp> logger, UserManager<UserAccount> userManager)
//    {
//        private readonly ILogger<SignUp> _logger = logger;
//        private readonly UserManager<UserAccount> _userManager = userManager;


//        [Function("SignUp")]
//        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
//        {
//            string body = null!;

//            try
//            {
//                body = await new StreamReader(req.Body).ReadToEndAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($" ERROR : SignUp.StreamReader :: {ex.Message}");
//            }           

//            if (body != null)
//            {
//                UserRegistrationModel urr = null!;
//                try
//                {
//                    urr = JsonConvert.DeserializeObject<UserRegistrationModel>(body)!;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError($" ERROR : Signup.JsonConvert.DeserializeObject<UserRegistrationModel> :: {ex.Message}");
//                }



//                if (urr != null && !string.IsNullOrEmpty(urr.Email) && !string.IsNullOrEmpty(urr.Password))
//                {
//                    using var _httpClient = new HttpClient();

//                    if (!await _userManager.Users.AnyAsync(x => x.Email == urr.Email ))
//                    {
//                        var userAccount = new UserAccount
//                        {
//                            FirstName = urr.FirstName,
//                            LastName = urr.LastName,
//                            Email = urr.Email,
//                            UserName = urr.Email

//                        };
//                        var result = await _userManager.CreateAsync(userAccount, urr.Password);
//                        if (result.Succeeded)
//                        {
//                            //SEND VERIFICATION (Function) CODE
//                            var verificationPayload = new
//                            {
//                                urr.Email
//                            };
//                            var content = new StringContent(JsonConvert.SerializeObject(verificationPayload), Encoding.UTF8, "application/json");
//                            var response = await _httpClient.PostAsync("https://verifikationskodprovider.azurewebsites.net/api/Lear?code=udnx8pP9X1REkr6V1UR6FD0C0s3E4r9qss4mnEE58UYZAzFun5VZJg%3D%3D", content);

//                            if (response.IsSuccessStatusCode)
//                            {
//                                return new OkResult();
//                            }
//                            else
//                            {
//                                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        return new ConflictResult();
//                    }
//                }

//            }

//            return new BadRequestResult();
//        }
//    }
//}


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

namespace AccountProvider.Functions
{
    public class SignUp
    {
        private readonly ILogger<SignUp> _logger;
        private readonly UserManager<UserAccount> _userManager;

        public SignUp(ILogger<SignUp> logger, UserManager<UserAccount> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [Function("SignUp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("SignUp function triggered.");

            // Leer el cuerpo de la solicitud
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Request body: {body}");

            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Request body is empty.");
                return new BadRequestResult();
            }

            // Deserializar el modelo de registro de usuario
            var urr = JsonConvert.DeserializeObject<UserRegistrationModel>(body);
            _logger.LogInformation($"Deserialized UserRegistrationModel: {urr?.Email}");

            if (urr == null || string.IsNullOrEmpty(urr.Email) || string.IsNullOrEmpty(urr.Password))
            {
                _logger.LogWarning("Invalid user registration model.");
                return new BadRequestResult();
            }

            // Verificar si el usuario ya existe
            if (await _userManager.Users.AnyAsync(x => x.Email == urr.Email))
            {
                _logger.LogWarning($"Email already exists: {urr.Email}");
                return new ConflictResult();
            }

            // Crear el usuario
            var userAccount = new UserAccount
            {
                FirstName = urr.FirstName,
                LastName = urr.LastName,
                Email = urr.Email,
                UserName = urr.Email
            };

            var result = await _userManager.CreateAsync(userAccount, urr.Password);
            if (!result.Succeeded)
            {
                _logger.LogError($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation($"User created successfully: {urr.Email}");

            // Enviar el código de verificación
            try
            {
                using var _httpClient = new HttpClient();
                var verificationPayload = new { urr.Email };
                var content = new StringContent(JsonConvert.SerializeObject(verificationPayload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://verifikationskodprovider.azurewebsites.net/api/Lear?code=udnx8pP9X1REkr6V1UR6FD0C0s3E4r9qss4mnEE58UYZAzFun5VZJg%3D%3D", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Verification code sent successfully to: {urr.Email}");
                    return new OkResult();
                }
                else
                {
                    _logger.LogError($"Failed to send verification code. Status code: {response.StatusCode}");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception while sending verification code: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
