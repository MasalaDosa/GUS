using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gus
{
    /// <summary>
    /// Sequence Guessing Program inspired by 
	/// GUS by David Deutsch and David Jonnson-Davies
	/// From Acorn User March 1987
    /// </summary>
	class Program
	{      
		static void Main()
		{
			var gus = new Gus();
			gus.OnGuess += Gus_OnGuess;
			while (true)
			{
				// Try and guess what's next in this list
				var sequence = GetSequence();

				var response = Choose("Do you want me to Guess or Test a gusL string?", new List<char> {'G', 'T'});

				if (response == 'G')
				{
					gus.GuessSequence(sequence);
				}
				else
				{
					TestGusL(gus, sequence, true);

				}
			}
		}

		static void Gus_OnGuess(Object sender, GusEventArgs e)
		{
			Console.WriteLine("Elapsed: {0}.", e.Elapsed);
            Console.WriteLine("I predict {0} ({1}).", e.Prediction, e.Hypothesis);
            // TODO - Convert GusL hypothesis to English
            var y = Choose("Am I correct?", new List<char> { 'Y', 'N' });

			if (y == 'Y')
			{
				e.IsCorrect = true;
			}
			else
            {
                Console.WriteLine("Ok, let me try again.");
            }
        }
        
	    static char Choose(string prompt, List<char> choices)
        {
            char result;
            Console.Write(string.Format("{0} ({1}).", prompt, string.Join('/', choices)));
            while (true)
            {
                var response = Console.ReadLine().ToUpperInvariant();
                if (choices.Exists(response.StartsWith))
                {
                    result = choices.Find(response.StartsWith);
                    break;
                }
                Console.WriteLine(string.Format("Please enter one of ({0}).", string.Join('/', choices)));
            }
            return result;
        }

		static List<int> GetSequence()
        {
            List<int> sequence;
            while (true)
            {
				sequence = null;
                Console.WriteLine("Enter a sequence of integers (separated by whitespace");
                var input = Console.ReadLine();
                var seq = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                try
                {
					sequence = seq.Select(s => int.Parse(s)).ToList();
                }
                catch (FormatException)
                {
                    Console.WriteLine("Integers only please");
                }
				if (sequence != null)
				{
					Console.WriteLine(string.Join(", ", sequence));
					var c = Choose("OK?", new List<char> { 'Y', 'N' });
					if (c == 'Y')
                    {
                        break;
                    }
                }
            }
		    return sequence;
        }
                    
	    static void TestGusL(Gus gus, List<int> sequence, bool verbose)
        {
            Console.WriteLine("GusL:");
            var gusl = Console.ReadLine();
            int prediction;
            var isGood = gus.TestHypothesis(sequence, gusl, verbose, out prediction);
        }    
	}
}