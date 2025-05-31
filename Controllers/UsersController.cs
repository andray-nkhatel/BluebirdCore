using BluebirdCore.DTOs;
using BluebirdCore.Entities;
using BluebirdCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BluebirdCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            });

            return Ok(userDtos);
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var user = new Entities.User
                {
                    Username = createUserDto.Username,
                    FullName = createUserDto.FullName,
                    Email = createUserDto.Email,
                    Role = createUserDto.Role
                };

                var createdUser = await _userService.CreateUserAsync(user, createUserDto.Password);

                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, new UserDto
                {
                    Id = createdUser.Id,
                    Username = createdUser.Username,
                    FullName = createdUser.FullName,
                    Email = createdUser.Email,
                    Role = createdUser.Role.ToString(),
                    IsActive = createdUser.IsActive,
                    CreatedAt = createdUser.CreatedAt,
                    LastLoginAt = createdUser.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update user (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UserDto userDto)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            user.FullName = userDto.FullName;
            user.Email = userDto.Email;
            user.IsActive = userDto.IsActive;

            var updatedUser = await _userService.UpdateUserAsync(user);

            return Ok(new UserDto
            {
                Id = updatedUser.Id,
                Username = updatedUser.Username,
                FullName = updatedUser.FullName,
                Email = updatedUser.Email,
                Role = updatedUser.Role.ToString(),
                IsActive = updatedUser.IsActive,
                CreatedAt = updatedUser.CreatedAt,
                LastLoginAt = updatedUser.LastLoginAt
            });
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Reset user password (Admin only)
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto resetDto)
        {
            var success = await _userService.ResetPasswordAsync(id, resetDto.NewPassword);
            if (!success)
                return NotFound();

            return Ok(new { message = "Password reset successfully" });
        }

        /// <summary>
        /// Deactivate user (Admin only)
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            var success = await _userService.DeactivateUserAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "User deactivated successfully" });
        }
    }
}