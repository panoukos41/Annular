using Microsoft.Extensions.Options;

namespace Annular.Modules;

/// <summary>
/// Marks a module as capable of validating itself.
/// Modules that are marked as such will be registered for validation
/// with an <see cref="IValidateOptions{TOptions}"/> where TOptions is the Module.
/// </summary>
public interface IValidModule
{
    /// <summary>
    /// Validate the current instance of the module.
    /// </summary>
    abstract ValidateOptionsResult Validate();
}
