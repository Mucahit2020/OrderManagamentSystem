using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Common.Attributes;

public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is Guid guid)
        {
            return guid != Guid.Empty;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"{name} cannot be empty GUID.";
}