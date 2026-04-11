using FeborBack.Domain.Entities;

namespace FeborBack.Infrastructure.DTOs;

public class UserWithPersonDto
{
    public int user_id { get; set; }
    public int person_id { get; set; }
    public string username { get; set; } = string.Empty;
    public string password_hash { get; set; } = string.Empty;
    public string password_salt { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public bool is_session_active { get; set; }
    public bool is_temporary_password { get; set; }
    public int failed_attempts { get; set; }
    public DateTime? last_access_at { get; set; }
    public int? authorized_by_user_id { get; set; }
    public string? avatar_name { get; set; }
    public int status_id { get; set; }
    public int? status_reason_id { get; set; }
    public int created_by { get; set; }
    public DateTime created_at { get; set; }
    public int? updated_by { get; set; }
    public DateTime? updated_at { get; set; }
    public string full_name { get; set; } = string.Empty;
    public string status_name { get; set; } = string.Empty;
    public long? total_count { get; set; } // Para paginación

    public LoginUser ToLoginUser()
    {
        return new LoginUser
        {
            UserId = user_id,
            PersonId = person_id,
            Username = username,
            PasswordHash = password_hash,
            PasswordSalt = password_salt,
            Email = email,
            IsSessionActive = is_session_active,
            IsTemporaryPassword = is_temporary_password,
            FailedAttempts = failed_attempts,
            LastAccessAt = last_access_at,
            AuthorizedByUserId = authorized_by_user_id,
            AvatarName = avatar_name,
            StatusId = status_id,
            StatusReasonId = status_reason_id,
            CreatedBy = created_by,
            CreatedAt = created_at,
            UpdatedBy = updated_by,
            UpdatedAt = updated_at,
            Person = new Person
            {
                PersonId = person_id,
                FullName = full_name
            },
            Status = new Status
            {
                StatusId = status_id,
                StatusName = status_name
            }
        };
    }
}