using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gus
{
    public class Gus
    {
		GusLProcessor _processor;
		readonly List<string> _memory;

		public event GuessHandler OnGuess;
        public delegate void GuessHandler(object sender, GusEventArgs e);
        
        public Gus()
		{
			_processor = new GusLProcessor();
			_memory = new List<string>();
			_memory.AddRange(_processor.GusLAtoms.Select(c=> c.ToString()));
		}

		public void GuessSequence(List<int> sequence)
        {
            var sw = new Stopwatch();
            sw.Start();

            foreach (var hypothesis in Hypothesise())
            {
                int prediction;
                if (TestHypothesis(sequence, hypothesis, false, out prediction))
                {
					sw.Stop();

					var gusEventArgs = new GusEventArgs
                    {
                        Elapsed = sw.Elapsed,
                        Prediction = prediction,
                        Hypothesis = hypothesis
                    };
					var guess = OnGuess;
					if (guess != null)
					{

						guess(this, gusEventArgs);
					}
					// Stop if correct
					if (gusEventArgs.IsCorrect)
					{
						// Remember this rule
						Remember(hypothesis);
						break;
					}
					sw.Reset();
					sw.Restart();
				}
            }
        }
  
        /// <summary>
        /// Tests a hypothesis.
        /// </summary>
		public bool TestHypothesis(List<int> sequence, string hypothesis, bool verbose, out int finalPrediction)
        {
			finalPrediction = 0;

            if (verbose)
            {
				Console.WriteLine("Testing hypothesis '{0}'.", hypothesis);
            }
            
			// Test if our hypothesis can generate the last integer in the sequence, then the punultimate etc.
			bool predictedCorrectly = false;
			for (int i = sequence.Count - 1; i >= 0; i--)
			{
				int prediction;
				var predictResult = TryMakePrediction(sequence.GetRange(0, i), hypothesis, verbose, out prediction);
				// If underflow then break from the loop - but we don't change the predictedCorrectly flag
				if (predictResult == GusStatus.Underflow)
				{
					if (verbose)
					{
						Console.WriteLine("Hypothesis caused underflow");
					}
					break;
				}
				if (predictResult == GusStatus.Error)
				{
					if (verbose)
					{
						Console.WriteLine("Hypothesis failed with error: {0}", _processor.LastError);
					}
					predictedCorrectly = false;
					break;
				}
				if (predictResult == GusStatus.OK && prediction != sequence[i])
				{
					if (verbose)
					{
						Console.WriteLine("Hypothesis failed with incorrect prediction: {0}", prediction);
					}
					predictedCorrectly = false;
					break;

				}
				// If we made a guess and its correct then flag this.
				if (predictResult == GusStatus.OK && prediction == sequence[i])
				{
					if (verbose)
					{
						Console.WriteLine("Hypothesis succeeded with correct prediction: {0}", prediction);
					}
					predictedCorrectly = true;
				}
			}

			// If we're still here then we haven't errored.
			// If we have predicted sucessfully at least once before finishing or underflowing then have a final guess.
			bool success = false;
			if (predictedCorrectly)
            {
                // Make a final prediction            
				var predictResult = TryMakePrediction(sequence, hypothesis, verbose, out finalPrediction);
				switch (predictResult)
				{
					case GusStatus.Underflow:
						if (verbose)
						{
							Console.WriteLine("Hypothesis caused underflow");
						}
						success = false;
						break;
					case GusStatus.Error:
						if (verbose)
						{
							Console.WriteLine("Hypothesis failed with error: {0}", _processor.LastError);
						}
						success = false;
						break;
					case GusStatus.OK:
						if (verbose)
						{
							Console.WriteLine("Hypothesis predicting: {0}", finalPrediction);
						}
						success = true;
						break;
				}
			}

			return success;
		}

		/// <summary>
		/// Create a stack from the supplied list and run the supplied GusL string against it.
		/// If sucessful a prediction for the next element will be returned
		/// </summary>
		public GusStatus TryMakePrediction(List<int> sequence, string gusl, bool verbose, out int prediction)
        {
			prediction = 0;

			// Build a stack for this sequence
            var stack = new Stack<int>();
            for (int i = 0; i < sequence.Count; i++)
            {
                stack.Push(sequence[i]);
            }
            
            var result = _processor.Interpret(stack, gusl, verbose);
            if (result == GusStatus.OK)
            {
                prediction = stack.Peek();
            }
            return result;
        }

		public IEnumerable<string> Hypothesise()
        {
            int n = 1;
            while (true)
            {
                foreach (var hypothesis in Combine(n))
                {
                    var h = string.Concat(hypothesis);
                    // Ignore combinations that contain a bad pair.
					// Or end in a number (its little more than a guess)
                    // Or contains unimportant leading chars
                    if( ContainsBadPairs(h) ||
					    (h.Length > 1 && '1' <= h.Last() && '@' >= h.Last()) ||
					    ContainsUnimportandLeadingOps(h))
					{
						continue;
					}
					yield return h;
                }
                n++;
            }
        }
        
        IEnumerable<List<string>> Combine(int n)
        {
            if (n == 0)
            {
                yield return new List<string>();
                yield break;
            }
            foreach (var i in _memory)
            {
                foreach (var s in Combine(n - 1))
                {
                    yield return s.Prepend(i).ToList();
                }
            }
        }

		void Remember(string hypothesis)
        {
            if (hypothesis.Length > 1)
            {
                for (int i = 2; i <= 2; i++) // Limit to remembering groups of 2 only -  hypothesis.Length; i++)
                {
                    for (int j = 0; j <= hypothesis.Length - i; j++)
                    {
                        var factoid = hypothesis.Substring(j, i);
                        if (!_memory.Contains(factoid))
                        {
                            _memory.Add(factoid);
                        }
                    }
                }
            }
        }

		/// <summary>
        /// Some combinations we ignore.
        /// The are either:
        /// Pointless (e.g. "po"),
        /// Redundant (e.g. "cp" - better expressed as "2t"),
        /// Nasty (e.g "FF")
        /// </summary>
	    static bool ContainsBadPairs(string hypothesis)
        {
            List<string> badCombos = new List<string>
			{
                "po", "to" ,"eo", "ro","do","mo","cp","ct","cr","cd",
                "cm","cp","cw","wp","wt","ww","no","nn","Po","PP",
                "PF","Fe","Fr","Fo","FP","FF","It","le","lr","Id",
                "lc","lo","1P","1F","11","12","13","14","15","2c",
                "2o","2P","2F","3c","3o","3P","3F","4c","4o","5c",
                "5o",":c",":o",":e", "eP"
			};

			if (hypothesis.Length < 2)
            {
                return false;
            }

            foreach (string pair in badCombos)
            {
				if (hypothesis.Contains(pair))
				{
					return true;
				}
            }
            return false;
        }

        static bool ContainsUnimportandLeadingOps(string gusl)
		{
			return gusl != GuslSimplify(gusl);
		}


		/// <summary>
        /// Removes pointless leading characters from a guls string
        /// e.g. "ppp28:tnm" is basically the same as "8:2tnm"
        /// The leading chars do not have any effect on the final result.
        /// All they serve is to eat up the stack and make an underflow likely
        /// Consider the tree:
        /// m -> n
        ///   -> t -> :
        ///        -> 8
        /// </summary>
        static string GuslSimplify(string gusl)
        {
            int index = gusl.Length - 1;
            int count = 0;
            string usefulRhs = string.Empty;
            for (int i = gusl.Length - 1; i >= 0; i--)
            {
                char c = gusl[i];
                usefulRhs = string.Concat(c, usefulRhs);

                // These operations have no real effect on this check
                if (new List<char> { 'w', 'o' }.Contains(c))
                {
                    continue;
                }

                count += RequiredChildNodes(c);

                if (count == 0)
                {
                    break;
                }
                count--;
            }
            return usefulRhs;
        }


        static int RequiredChildNodes(char c)
        {
            // 'o' ? 'w' ?

            switch (c)
            {
                // These all count as 'end nodes' in our syntax tree
                case char n when n >= '1' && n <= '@' ||
                    new List<char> { 'n', 'c' }.Contains(n):
                    return 0;
                // All these require 2 child nodes / argument
                case char n when new List<char> { 'p', 'm', 't', 'd', 'r', 'e' }.Contains(n):
                    return 2;
                // These require 1 childnode / argument
                case char n when new List<char> { 'P', 'F' }.Contains(n):
                    return 1;
                default:
                    return 0;
            }
        }
	}
}
