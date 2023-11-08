using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a C# expression.
/// </summary>
public class CSharpExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public CSharpExpression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpression"/> class.
    /// </summary>
    /// <param name="value">The C# expression.</param>
    public CSharpExpression(string value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Gets or sets the C# expression.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Returns the C# expression.
    /// </summary>
    public override string ToString() => Value ?? "";
}