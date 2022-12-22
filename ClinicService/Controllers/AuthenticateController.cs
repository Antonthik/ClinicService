﻿using ClinicService.Models;
using ClinicService.Models.Requests;
using ClinicService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace ClinicService.Controllers
{
    [Authorize]
    [Route("api/auth")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        #region Services

        private readonly IAuthenticateService _authenticateService;

        #endregion

        #region Constructors

        public AuthenticateController(IAuthenticateService authenticateService)
        {
            _authenticateService = authenticateService;
        }

        #endregion

        #region Public Methods

        [AllowAnonymous]
        [HttpPost("login")]
        public ActionResult<AuthenticationResponse> Login([FromBody] AuthenticationRequest authenticationRequest)
        {
            AuthenticationResponse authenticationResponse = _authenticateService.Login(authenticationRequest);
            if (authenticationResponse.Status == Models.AuthenticationStatus.Success)//если статус аутентификации успешен
            {
                Response.Headers.Add("X-Session-Token", authenticationResponse.SessionContext.SessionToken);//передаем токен через заголовок запроса
            }
            return Ok(authenticationResponse);//возвращаем результат обработки запроса
        }

        [HttpGet("session")]
        public ActionResult<SessionContext> GetSession()//не возвращаем заголовок, а нам присылают токен в виде заголовка.Здесь токен проверяем на корректность
        {
            //[Authorization: Bearer XXXXXXXXXXXXXXXXXXXXXXXX] - пример запроса.Bearer-схема взаимодействия("на предъявителя")
            var authorization = Request.Headers[HeaderNames.Authorization];//берем из справочника констань правильное название
            if (AuthenticationHeaderValue.TryParse(authorization, out var headerValue))//отделям заголовок от токена
            {
                var scheme = headerValue.Scheme; // "Bearer"
                var sessionToken = headerValue.Parameter; // Token

                if (string.IsNullOrEmpty(sessionToken))
                    return Unauthorized();

                SessionContext sessionContext = _authenticateService.GetSessionInfo(sessionToken);
                if (sessionContext == null)
                    return Unauthorized();

                return Ok(sessionContext);
            }
            return Unauthorized();
        }


        #endregion

    }
}
