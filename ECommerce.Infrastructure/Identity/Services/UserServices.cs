using ECommerce.Infrastructure.DbContext;
using Microsoft.Extensions.Configuration;
using ECommerce.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Domain.Constants;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using System.Security.Claims;
using System.Text;

namespace ECommerce.Infrastructure.Identity.Services;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signinManager;

    public UserService(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signinManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _signinManager = signinManager;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterUserAsync(RegisterDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = dto.Email,
                UserName = dto.Email,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(",", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Customer);

            var userProfile = new UserProfile
            {
                Id = Guid.CreateVersion7(),
                ApplicationUserId = user.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _context.UserProfiles.AddAsync(userProfile);
            await _context.SaveChangesAsync();

            var token = await GenerateJWTToken(user, Roles.Customer);

            await transaction.CommitAsync();

            return new AuthResponse(
                user.UserName!,
                Roles.Customer,
                token,
                DateTime.UtcNow.AddHours(1)
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.UserName);
        if (user is null)
        {
            throw new Exception("User Not Found");
        }

        var isAuthenticated = await _signinManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!isAuthenticated.Succeeded)
        {
            throw new Exception("Invalid Credentials");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Customer;

        var token = await GenerateJWTToken(user, role);

        return new AuthResponse(
            user.UserName!,
            role,
            token,
            DateTime.UtcNow.AddHours(1)
        );
    }

    protected async Task<string> GenerateJWTToken(ApplicationUser user, string role)
    {
        var key = _configuration["JwtSettings:SecretKey"];
        var byteKey = Encoding.UTF8.GetBytes(key!);
        var securityKey = new SymmetricSecurityKey(byteKey);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(up => up.ApplicationUserId == user.Id);

        var claims = new Claim[]
        {
            new("UserProfileId", userProfile!.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, role),

        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddHours(1)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}