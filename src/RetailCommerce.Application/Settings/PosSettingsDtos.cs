namespace RetailCommerce.Application.Settings;

public record PosSettingsDto(string ViewMode, int ReturnPolicyDays, IReadOnlyList<string> VisibleProductFields);

public record UpdatePosSettingsRequest(string ViewMode, int ReturnPolicyDays, IReadOnlyList<string> VisibleProductFields);
