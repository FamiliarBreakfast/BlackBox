namespace BlackBox.System.Peripherals;

public static class Chronometer
{
	private static long _time100NanosecondsSinceEpoch = 984241843990000000;
	
	public static long Time()
	{
		return _time100NanosecondsSinceEpoch;
	}
}

public class DateTime
{
	//todo datetime class using Int128 or BigInt
	//only really needs to handle gregorian dates and arbitrary timezone offsets
	//and maybe relativity
	//so not that simple
}