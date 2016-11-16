﻿namespace AdMaiora.Listy.Api.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mail;
    using System.Web.Http;
    using System.Web.Http.Tracing;
    using System.Web.Security;
    using System.Collections.Generic;
    using System.IO;
    using System.Web;
    using System.Linq;
    using System.Data.Entity;
    using System.Threading.Tasks;
    using System.IdentityModel.Tokens;
    using System.Security.Claims;

    using AdMaiora.Listy.Api.Models;
    using AdMaiora.Listy.Api.DataObjects;

    using Microsoft.Azure.Mobile.Server;
    using Microsoft.Azure.Mobile.Server.Config;
    using Microsoft.Azure.Mobile.Server.Login;
    using Microsoft.Azure.NotificationHubs;

    using SendGrid;
    using SendGrid.Helpers.Mail;    

    public class UsersController : ApiController
    {
        #region Inner Classes
        #endregion

        #region Constants and Fields

        // Authorization token duration (in days)
        public const int AUTH_TOKEN_MAX_DURATION = 1;

        public const string JWT_SECURITY_TOKEN_AUDIENCE = "https://listy-api.azurewebsites.net/";
        public const string JWT_SECURITY_TOKEN_ISSUER = "https://listy-api.azurewebsites.net/";

        #endregion

        #region Constructors

        public UsersController()
        {
        }

        #endregion

        #region Users Endpoint Methods

        [HttpPost, Route("users/register")]
        public async Task<IHttpActionResult> RegisterUser(Poco.User credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.Email))
                return BadRequest("The email is not valid!");

            if (string.IsNullOrWhiteSpace(credentials.Password))
                return BadRequest("The password is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    User user = ctx.Users.SingleOrDefault(x => x.Email == credentials.Email);
                    if (user != null)
                        return InternalServerError(new InvalidOperationException("This email has already taken!"));

                    user = new User { Email = credentials.Email, Password = credentials.Password };
                    user.Ticket = Guid.NewGuid().ToString();
                    ctx.Users.Add(user);
                    ctx.SaveChanges();

                    string apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                    SendGridAPIClient mc = new SendGridAPIClient(apiKey);

                    Email to = new Email(user.Email);
                    Email from = new Email("info@admaiorastudio.com");
                    string subject = "Welocme to Listy!";
                    Content content = new Content("text/plain",
                        String.Format("Hi {0},\n\nYou registration on Listy is almost complete. Please click on this link to confirm your registration!\n\n{1}",
                        user.Email.Split('@')[0],
                        String.Format("https://listy-api.azurewebsites.net/users/confirm?ticket={0}", user.Ticket)));
                    Mail mail = new Mail(from, subject, to, content);

                    dynamic response = await mc.client.mail.send.post(requestBody: mail.Get());

                    return Ok(Dto.Wrap(new Poco.User
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        AuthAccessToken = null,
                        AuthExpirationDate = null

                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route("users/verify")]
        public async Task<IHttpActionResult> VerifyUser(Poco.User credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.Email))
                return BadRequest("The email is not valid!");

            if (string.IsNullOrWhiteSpace(credentials.Password))
                return BadRequest("The password is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    User user = ctx.Users.SingleOrDefault(x => x.Email == credentials.Email);
                    if (user == null)
                        return InternalServerError(new InvalidOperationException("This email is not registered!"));

                    if (user.IsConfirmed)
                        return InternalServerError(new InvalidOperationException("This email has been already confirmed!"));

                    string p1 = FormsAuthentication.HashPasswordForStoringInConfigFile(user.Password, "MD5");
                    string p2 = FormsAuthentication.HashPasswordForStoringInConfigFile(credentials.Password, "MD5");
                    if (p1 != p2)
                        return InternalServerError(new InvalidOperationException("Your credentials seem to be not valid!"));

                    string apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                    SendGridAPIClient mc = new SendGridAPIClient(apiKey);

                    Email to = new Email(user.Email);
                    Email from = new Email("info@admaiorastudio.com");
                    string subject = "Welocme to Listy!";
                    Content content = new Content("text/plain",
                        String.Format("Hi {0},\n\nYou registration on Listy is almost complete. Please click on this link to confirm your registration!\n\n{1}",
                        user.Email.Split('@')[0],
                        String.Format("https://listy-api.azurewebsites.net/users/confirm?ticket={0}", user.Ticket)));
                    Mail mail = new Mail(from, subject, to, content);

                    dynamic response = await mc.client.mail.send.post(requestBody: mail.Get());
                    if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                        return InternalServerError(new InvalidOperationException("Internal mail error. Retry later!"));

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet, Route("users/confirm")]
        public IHttpActionResult ConfirmUser(string ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket))
                return BadRequest("The ticket is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    User user = ctx.Users.SingleOrDefault(x => x.Ticket == ticket);
                    if (user == null)
                        return BadRequest("This ticket is not a real!");

                    user.IsConfirmed = true;
                    ctx.SaveChanges();

                    IHttpActionResult response;
                    //we want a 303 with the ability to set location
                    HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.RedirectMethod);
                    responseMsg.Headers.Location = new Uri("http://www.admaiorastudio.com/listy.php");
                    response = ResponseMessage(responseMsg);
                    return response;
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route("users/login")]
        public IHttpActionResult LoginUser(Poco.User credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.Email))
                return BadRequest("The email is not valid!");

            if (string.IsNullOrWhiteSpace(credentials.Password))
                return BadRequest("The password is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    User user = ctx.Users.SingleOrDefault(x => x.Email == credentials.Email);
                    if (user == null)
                        return Unauthorized();

                    if (!user.IsConfirmed)
                        return InternalServerError(new InvalidOperationException("You must confirm your email first!"));

                    string p1 = FormsAuthentication.HashPasswordForStoringInConfigFile(user.Password, "MD5");
                    string p2 = FormsAuthentication.HashPasswordForStoringInConfigFile(credentials.Password, "MD5");
                    if (p1 != p2)
                        return Unauthorized();

                    var token = GetAuthenticationTokenForUser(user.Email);
                    user.LoginDate = DateTime.Now.ToUniversalTime();
                    user.LastActiveDate = user.LoginDate;
                    user.AuthAccessToken = token.RawData;
                    user.AuthExpirationDate = token.ValidTo;
                    ctx.SaveChanges();

                    return Ok(Dto.Wrap(new Poco.User
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        LoginDate = user.LoginDate,
                        AuthAccessToken = user.AuthAccessToken,
                        AuthExpirationDate = user.AuthExpirationDate
                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet, Route("users/restore")]
        public IHttpActionResult RestoreUser(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return BadRequest("The access token is not valid!");

            try
            {
                using (var ctx = new ListyDbContext())
                {
                    User user = ctx.Users.SingleOrDefault(x => x.AuthAccessToken == accessToken);
                    if (user == null)
                        return Unauthorized();

                    user.LoginDate = DateTime.Now.ToUniversalTime();
                    user.LastActiveDate = user.LoginDate;
                    ctx.SaveChanges();

                    return Ok(Dto.Wrap(new Poco.User
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        LoginDate = user.LoginDate,
                        AuthAccessToken = user.AuthAccessToken,
                        AuthExpirationDate = user.AuthExpirationDate
                    }));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Methods

        private JwtSecurityToken GetAuthenticationTokenForUser(string email)
        {
            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email.Split('@')[0]),
                new Claim(JwtRegisteredClaimNames.Email, email),
            };

            // The WEBSITE_AUTH_SIGNING_KEY variable will be only available when
            // you enable App Service Authentication in your App Service from the Azure back end
            https://azure.microsoft.com/en-us/documentation/articles/app-service-mobile-dotnet-backend-how-to-use-server-sdk/#how-to-work-with-authentication

            var signingKey = Environment.GetEnvironmentVariable("WEBSITE_AUTH_SIGNING_KEY");
            var audience = UsersController.JWT_SECURITY_TOKEN_AUDIENCE;
            var issuer = UsersController.JWT_SECURITY_TOKEN_ISSUER;

            var token = AppServiceLoginHandler.CreateToken(
                claims,
                signingKey,
                audience,
                issuer,
                TimeSpan.FromDays(UsersController.AUTH_TOKEN_MAX_DURATION)
                );

            return token;
        }

        #endregion
    }
}