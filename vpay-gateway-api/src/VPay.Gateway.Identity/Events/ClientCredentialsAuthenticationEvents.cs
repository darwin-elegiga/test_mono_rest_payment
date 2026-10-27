namespace VPay.Gateway.Identity.Events;

/// <summary>
/// Events invoked by the authentication handler to give the application
/// control at different points where processing occurs.
/// </summary>
public class ClientCredentialsAuthenticationEvents
{
    /// <summary>
    /// The handler that will be invoked when authentication fails and encounters an exception.
    /// </summary>
    public Func<ClientCredentialsAuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = _ => Task.CompletedTask;

    /// <summary>
    /// The handler that will be invoked when the credentials need validation.
    /// </summary>
    /// <remarks>
    /// This property must have a value for authentication to occur.  The handler should
    /// construct an authentication principal, attach it to the context.Principal property,
    /// and call context.Success().
    /// </remarks>
    public Func<ClientCredentialsValidateCredentialsContext, Task> OnValidateCredentials { get; set; } = _ => Task.CompletedTask;

    public virtual Task AuthenticationFailed(ClientCredentialsAuthenticationFailedContext context) =>
        OnAuthenticationFailed(context);

    public virtual Task ValidateCredentials(ClientCredentialsValidateCredentialsContext context) =>
        OnValidateCredentials(context);
}
