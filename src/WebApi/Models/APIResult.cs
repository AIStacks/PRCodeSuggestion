using FluentResults;

namespace WebApi.Models;

public record APIResult<T>(bool IsSuccess, string[] Errors)
{
    public T Value { get; set; }
}

public static class ResultDtoHelper
{
    public static APIResult<T> ToDto<T>(this Result<T> result) where T : class
    {
        var dto = new APIResult<T>
        (
            IsSuccess: result.IsSuccess,
            Errors: result.Errors.Select(e => e.Message).ToArray()
        );
        if (result.IsSuccess)
        {
            dto.Value = result.Value;
        }

        return dto;
    }
}
