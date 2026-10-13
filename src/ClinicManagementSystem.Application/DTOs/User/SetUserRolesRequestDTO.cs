using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.User;

public class SetUserRolesRequestDTO : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng gửi danh sách vai trò.")]
    [MaxLength(100, ErrorMessage = "Tối đa 100 vai trò trong một yêu cầu.")]
    public int[]? RoleIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RoleIds?.Any(id => id <= 0) == true)
            yield return new("Id vai trò phải lớn hơn 0.", new[] { nameof(RoleIds) });
        if (RoleIds != null && RoleIds.Distinct().Count() != RoleIds.Length)
            yield return new("Danh sách vai trò không được trùng Id.", new[] { nameof(RoleIds) });
    }
}
