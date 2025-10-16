using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Identity.Azure;

public class AzureActiveDirectoryProvider : IUserProvider
{
    private readonly AzureAdOptions _options;
    private readonly GraphServiceClient _graphServiceClient;

    public AzureActiveDirectoryProvider(AzureAdOptions options)
    {
        _options = options;

        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var clientSecretCredential = new ClientSecretCredential(_options.TenantId, _options.AppId, _options.ClientSecret);
        _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
    }

    public async Task CreateUserAsync(IUser user)
    {
        try
        {
            var createdUser = await _graphServiceClient.Users.PostAsync(new Microsoft.Graph.Models.User
            {
                UserPrincipalName = user.Username,
                PasswordProfile = new Microsoft.Graph.Models.PasswordProfile { Password = user.Password },
                GivenName = user.FirstName,
                Surname = user.LastName,
                DisplayName = $"{user.FirstName} {user.LastName}",
                AccountEnabled = true,
                Mail = user.Email,
                MailNickname = user.Email.Split('@')[0]
            });

            user.Id = createdUser.Id;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        await _graphServiceClient.Users[userId].DeleteAsync();
    }

    public async Task<IUser> GetUserById(string userId)
    {
        try
        {
            var user = await _graphServiceClient.Users[userId].GetAsync();

            return new User
            {
                Id = user.Id,
                Username = user.UserPrincipalName,
                FirstName = user.GivenName,
                LastName = user.Surname
            };
        }
        catch (ODataError ex)
        {
            if (ex.ResponseStatusCode == 404)
            {
                return null;
            }

            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IUser> GetUserByUsernameAsync(string username)
    {
        try
        {
            var user = await _graphServiceClient.Users[username].GetAsync();

            return new User
            {
                Id = user.Id,
                Username = user.UserPrincipalName,
                FirstName = user.GivenName,
                LastName = user.Surname
            };
        }
        catch (ODataError ex)
        {
            if (ex.ResponseStatusCode == 404)
            {
                return null;
            }

            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IList<IUser>> GetUsersAsync()
    {
        var result = await _graphServiceClient.Users.GetAsync();

        return result.Value.Select(x => (IUser)new User
        {
            Id = x.Id,
            Username = x.UserPrincipalName,
            FirstName = x.GivenName,
            LastName = x.Surname
        }).ToList();
    }

    public async Task UpdateUserAsync(string userId, IUser user)
    {
        var returnedUser = await _graphServiceClient.Users[userId].PatchAsync(new Microsoft.Graph.Models.User
        {
            GivenName = user.FirstName,
            Surname = user.LastName,
            DisplayName = $"{user.FirstName} {user.LastName}"
        });
    }
}
