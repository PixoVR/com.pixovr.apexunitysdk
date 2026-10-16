namespace PixoVR.Apex
{
	/// <summary>A lazily created, process-wide instance of a plain C# class.</summary>
	public class PixoSingleton<TInstance> where TInstance : class, new()
	{
		private static readonly TInstance kInstance = new TInstance();

		public static TInstance Instance => kInstance;
	}
}
