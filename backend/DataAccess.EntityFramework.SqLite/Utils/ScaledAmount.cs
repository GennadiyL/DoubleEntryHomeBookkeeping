namespace DataAccess.EntityFramework.SqLite.Utils;

/// <summary>
/// Converts account-currency decimals to exact SQLite integers.
/// Storage uses a fixed scale of ten thousand.
/// Conversion never rounds or truncates excess fractional digits.
/// Values outside signed 64-bit storage fail explicitly.
/// Arithmetic remains decimal until the checked integer conversion.
/// Reading reverses the scale without floating-point arithmetic.
/// The converter contains no bookkeeping validation policy.
/// Callers determine precision and transaction failure handling.
/// </summary>
internal static class ScaledAmount
{
	public static long ToStorage(decimal value)
	{
		decimal scaled = value * 10000m;
		if (scaled != decimal.Truncate(scaled))
		{
			throw new OverflowException("The amount cannot be represented at the storage scale.");
		}
		return checked((long)scaled);
	}

	public static decimal FromStorage(long value) => value / 10000m;
}
