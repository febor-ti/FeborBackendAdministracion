namespace FeborBack.Application.DTOs.User;

public class StatusDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class StatusReasonDto
{
    public int StatusReasonId { get; set; }
    public string ReasonName { get; set; } = string.Empty;
}