using System;
namespace Gus
{
	public class GusEventArgs : EventArgs
    {
		public TimeSpan Elapsed { get; set; } 
		public int Prediction { get; internal set; }
		public string Hypothesis { get; internal set; }
		public bool IsCorrect { get; internal set; }
	}
}
