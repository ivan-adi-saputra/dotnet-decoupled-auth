using AuthApp.Api.Models.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthApp.Api.Filters;

/// <summary>
/// Runs the FluentValidation validator registered for each action argument (if any)
/// before the action executes, short-circuiting with 400 on failure. Keeps controller
/// actions free of repeated manual input checks.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);
            if (!result.IsValid)
            {
                var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage).Distinct());
                context.Result = new BadRequestObjectResult(new AuthResponse(false, message));
                return;
            }
        }

        await next();
    }
}
