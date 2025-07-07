using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Identity.GoogleCloud;

public class AwsCognitoIdentityProvider : IUserProvider
{
    private readonly AwsCognitoIdentityOptions _options;
    private readonly AmazonCognitoIdentityProviderClient _client;

    public AwsCognitoIdentityProvider(AwsCognitoIdentityOptions options)
    {
        _options = options;
        _client = _options.CreateAmazonCognitoIdentityProviderClient();
    }

    public async Task CreateUserAsync(IUser user)
    {
        var createUserRequest = new AdminCreateUserRequest
        {
            Username = user.Username,
            TemporaryPassword = user.Password,
            UserPoolId = _options.UserPoolID,
            UserAttributes = new List<AttributeType>
            {
                new AttributeType()
                {
                    Name = "email",
                    Value = user.Email
                },
                new AttributeType()
                {
                    Name = "given_name",
                    Value = user.FirstName
                },
                new AttributeType()
                {
                    Name = "middle_name",
                    Value = user.LastName
                }
            }
        };

        try
        {
            var createUserResponse = await _client.AdminCreateUserAsync(createUserRequest);

            user.Id = user.Username;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        await _client.AdminDeleteUserAsync(new AdminDeleteUserRequest { Username = userId, UserPoolId = _options.UserPoolID });
    }

    public async Task<IUser> GetUserById(string userId)
    {
        return await GetUserByUsernameAsync(userId);
    }

    public async Task<IUser> GetUserByUsernameAsync(string username)
    {
        try
        {
            var response = await _client.AdminGetUserAsync(new AdminGetUserRequest { Username = username, UserPoolId = _options.UserPoolID });

            return new User
            {
                Id = response.Username,
                Username = response.Username,
                Email = response.UserAttributes.Where(x => x.Name == "email").FirstOrDefault()?.Value,
                FirstName = response.UserAttributes.Where(x => x.Name == "given_name").FirstOrDefault()?.Value,
                LastName = response.UserAttributes.Where(x => x.Name == "middle_name").FirstOrDefault()?.Value,
            };
        }
        catch (UserNotFoundException)
        {
            return null;
        }
    }

    public async Task<IList<IUser>> GetUsersAsync()
    {
        IList<IUser> users = new List<IUser>();

        var response = await _client.ListUsersAsync(new ListUsersRequest { UserPoolId = _options.UserPoolID });

        foreach (var user in response.Users)
        {
            users.Add(new User
            {
                Id = user.Username,
                Username = user.Username,
                Email = user.Attributes.Where(x => x.Name == "email").FirstOrDefault()?.Value,
                FirstName = user.Attributes.Where(x => x.Name == "given_name").FirstOrDefault()?.Value,
                LastName = user.Attributes.Where(x => x.Name == "middle_name").FirstOrDefault()?.Value,
            });
        }

        return users;
    }

    public async Task UpdateUserAsync(string userId, IUser user)
    {
        await _client.AdminUpdateUserAttributesAsync(new AdminUpdateUserAttributesRequest
        {
            Username = user.Username,
            UserPoolId = _options.UserPoolID,
            UserAttributes = new List<AttributeType>
            {
                new AttributeType()
                {
                    Name = "email",
                    Value = user.Email
                },
                new AttributeType()
                {
                    Name = "given_name",
                    Value = user.FirstName
                },
                new AttributeType()
                {
                    Name = "middle_name",
                    Value = user.LastName
                }
            }
        });
    }
}
