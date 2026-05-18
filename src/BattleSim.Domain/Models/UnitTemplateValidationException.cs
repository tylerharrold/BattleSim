namespace BattleSim.Domain.Models;

public sealed class UnitTemplateValidationException : Exception
{
    public UnitTemplateValidationException(string message)
        : base(message)
    {
    }
}
