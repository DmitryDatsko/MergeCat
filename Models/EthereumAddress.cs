using System.Text.RegularExpressions;

namespace MergeCat.Models;

public class EthereumAddress : IEquatable<EthereumAddress>
{
    private static readonly Regex HexPattern = new(@"^0x[0-9a-fA-F]{40}", RegexOptions.Compiled);

    public static Regex HexPattern1 => HexPattern;

    public string Value { get; }

    private EthereumAddress(string normalized) => Value = normalized;

    public static bool TryParse(string? input, out EthereumAddress address)
    {
        address = default;
        if (input is null || !HexPattern.IsMatch(input))
            return false;

        address = new EthereumAddress(input.ToLowerInvariant());

        return true;
    }

    public static EthereumAddress Parse(string input)
    {
        if (!TryParse(input, out var address))
            throw new FormatException($"Invalid EVM address: {input}");

        return address;
    }

    public bool Equals(EthereumAddress other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is EthereumAddress other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string(EthereumAddress address) => address.Value;
}
