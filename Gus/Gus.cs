using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gus
{
    public class Gus
    {
		GusLProcessor _processor = null;
		List<string> _memory = null;

        // TODO
		// Some pairs we remove - either meaningless or nasty
        public static List<string> BadCombos = new List<string>
        {
            "po", "to" ,"eo", "ro","do","mo","cp","ct","cr","cd",
            "cm","cp","cw","wp","wt","ww","no","nn","Po","PP",
            "PF","Fe","Fr","Fo","FP","FF","It","le","lr","Id",
            "lc","lo","1P","1F","11","12","13","14","15","2c",
            "2o","2P","2F","3c","3o","3P","3F","4c","4o","5c",
            "5o",":c",":o",":e", "eP"
        };

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
            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (var hypothesis in Hypothesise())
            {
                int prediction;
                if (TestHypothesis(sequence, hypothesis, false, out prediction))
                {
					sw.Stop();

					GusEventArgs gusEventArgs = new GusEventArgs()
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
					if(gusEventArgs.IsCorrect)
					{
						// Remember this rule
                        Remember(hypothesis);
                        break;
					}
                    // Otherwise carry on
					else
					{
						sw.Reset();
                        sw.Restart();
					}  
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
				// If we errored then bail.
				else if (predictResult == GusStatus.Error)
				{
					if (verbose)
					{
						Console.WriteLine("Hypothesis failed with error: {0}", _processor.LastError);
					}
					predictedCorrectly = false;
					break;
				}
				// If we made a guess, but it's wrong, then bail
				else if (predictResult == GusStatus.OK && prediction != sequence[i])
				{
					if (verbose)
					{
						Console.WriteLine("Hypothesis failed with incorrect prediction: {0}", prediction);
					}
					predictedCorrectly = false;
					break;

				}
				// If we made a guess and its correct then flag this.
				else if (predictResult == GusStatus.OK && prediction == sequence[i])
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
			    if (predictResult == GusStatus.Underflow)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Hypothesis caused underflow");
                    }
                    success = false;
                }
				else if (predictResult == GusStatus.Error)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Hypothesis failed with error: {0}", _processor.LastError);
                    }
					success = false;
                }
                // If we made a guess, lets hope its right
                else if (predictResult == GusStatus.OK)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Hypothesis predicting: {0}", finalPrediction);
                    }
					success = true;
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
            Stack<int> stack = new Stack<int>();
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
                    string h = string.Concat(hypothesis);
                    {
                        yield return h;
                    }
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

	    string RemoveBadPairs(string v)
        {
            if (v.Length < 2)
            {
                return v;
            }

            foreach (string pair in BadCombos)
            {
                v = v.Replace(pair, string.Empty);
            }
            return v;
        }

        void Remember(string hypothesis)
		{
			if (hypothesis.Length > 1)
			{
				for (int i = 2; i <= hypothesis.Length; i++)
				{
                    for (int j = 0; j <= hypothesis.Length - i; j++)
					{
						string factoid = hypothesis.Substring(j, i);
                        if(!_memory.Contains(factoid))
						{
							_memory.Add(factoid);
						}
					}
				}
			}
		}
    }
}
