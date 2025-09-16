namespace Chainly.Infrastructure.Helpers;

public class EnumHelper
{
    public static IEnumerable<TDto> ToDtoList<TEnum, TDto>(
        Func<int, string, TDto> createDto)
        where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(e => createDto(Convert.ToInt32(e), e.ToString()));
    }
}