using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;

/// <summary>
/// Available local codes.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "The names are based on the language_territory format")]
// ReSharper disable InconsistentNaming - the names are based on the language_territory format
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocaleCode
{
    /// <summary>
    /// German (Germany)
    /// </summary>
    de_DE,

    /// <summary>
    /// French (France)
    /// </summary>
    fr_FR,

    /// <summary>
    /// Spanish (Spain)
    /// </summary>
    es_ES,

    /// <summary>
    /// Italian (Italy)
    /// </summary>
    it_IT,

    /// <summary>
    /// English (United Kingdom)
    /// </summary>
    en_GB,

    /// <summary>
    /// Dutch (Netherlands)
    /// </summary>
    nl_NL,

    /// <summary>
    /// Polish (Poland)
    /// </summary>
    pl_PL,

    /// <summary>
    /// Swedish (Sweden)
    /// </summary>
    sv_SE
}
