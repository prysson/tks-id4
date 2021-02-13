﻿using CompanyEmployees.Client.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CompanyEmployees.Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        [Authorize]
        public async Task<IActionResult> Companies()
        {
            var httpClient = _httpClientFactory.CreateClient("APIClient");
            var response = await httpClient.GetAsync("api/companies").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var companiesString = await response.Content.ReadAsStringAsync();
            var companies = JsonSerializer.Deserialize<List<CompanyViewModel>>(companiesString,
           new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(companies);
        }

        public IActionResult Index()
        {
            return View();
        }
       [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Privacy()
        {
            var idpClient = _httpClientFactory.CreateClient("IDPClient");
            var metaDataResponse = await idpClient.GetDiscoveryDocumentAsync();
            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            var response = await idpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = metaDataResponse.UserInfoEndpoint,
                Token = accessToken
            });
            if (response.IsError)
            {
                throw new Exception("Problem while fetching data from the UserInfo endpoint",
               response.Exception);
            }
            var addressClaim = response.Claims.FirstOrDefault(c => c.Type.Equals("address"));
            User.AddIdentity(new ClaimsIdentity(new List<Claim> { new
                Claim(addressClaim.Type.ToString(), addressClaim.Value.ToString()) }));
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
