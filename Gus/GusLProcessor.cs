using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace Gus
{
	
    /// <summary>
	/// A processor for the GusL language as described in the
	/// March 1987 edition of Acorn User Magasine.
	/// The following operators are supported:
	/// p - Plus            - a b becomes (a + b)
	/// m - Minus           - a b becomes (a - b)
	/// t - Times           - a b becomes (a * b)
	/// d - Divide          - a b becomes (a / b)
	/// r - Remain          - a b becomes (a % b)
	/// e - Exp             - a b becomes (a ^ b)
	/// w - Swap            - a b becomes b a
	/// c - Copy            - a becomes a a
	/// o - Pop             - a b becomes a
	/// 1 - 1               - a becomes a 1
	/// ...
	/// : - 10              - a becomes a 10
	/// ; &lt; = &gt;  ?    - a becomes {11:15}
	/// @ - 16              - a becomes a 16
	/// P - Prime           - n becomes nthPrime
	/// F - Fact            - n becomes Factorial(n)
	/// n - Number          - a becomes a nth
    /// </summary>
    public class GusLProcessor
    {
		const int MAX_FACT = 12;
		const int MAX_NUMBER_OF_PRIMES = 100;

		/// <summary>
        /// The atoms of the Gus Language.
        /// </summary>
		public static ReadOnlyCollection<char> GusLAtoms{ get; } = new List<char>
        {
            'p', 'm', 't', 'd', 'r', 'w', 'c', 'o', 'e',
            '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?', '@',
            'P', 'F', 'n'
        }.AsReadOnly();

		public string LastError { get; private set; }
                
        Stack<int> _stack;
	    int _initialStackCount;
		bool _verbose;
  
		static List<int> _primeCache;
		static Dictionary<char, MethodInfo> _methodInfoCache = new Dictionary<char, MethodInfo>();

        /// <summary>
		/// Interpret the specified GusL against the given stack (optionally verbose).
        /// </summary>
        public GusStatus Interpret(Stack<int> stack, string gusL, bool verbose = false)
        {
			_stack = stack;
			_initialStackCount = stack.Count;
			_verbose = verbose;
            
			GusStatus status = GusStatus.OK;

            if (verbose)
            {
                Console.WriteLine("GusL: {0}", gusL);
                DumpStack();
            }

            // Evaluate the GusL string against the stack.
            foreach (var c in gusL)
            {
                status = Process(c);
                if (verbose)
                {
                    DumpStack();
                }
                if(status != GusStatus.OK)
				{
					break;
				}
            }

            // Finally - in order for this to be useful there has to be at least one item remaining on the stack.
            if(status == GusStatus.OK && _stack.Count == 0)
			{
				LastError = "The stack was empty after processing.";
				status = GusStatus.Error;
			}
			return status;
        }
        
        /// <summary>
        /// Process the specified GusL atom.
		/// TODO - Check for overflows and handle internally
		/// e.g. "e4nmd" on "99 96 91 84 75"
        /// </summary>
        GusStatus Process(char op)
        {
			if (!GusLAtoms.Contains(op))
			{
				LastError = string.Format("Only valid GusL atoms are allowed: {0}.", string.Join(", ", GusLAtoms));
				return GusStatus.Error;
			}

			if (_verbose)
			{
				Console.WriteLine("Evaluating: {0}", op);
			}

			if (op >= '1' && op <= '@')
			{
				_stack.Push(op - '1' + 1);
				return GusStatus.OK;
			}
			return (GusStatus)GetMethodInfo(op).Invoke(this, null);
		}

		///  <summary>
		/// Pops N elements into a list
        /// </summary>
		List<int> PopN(int n)	
		{
			var r = new List<int>();
			while (n > 0)
            {
				r.Add(_stack.Pop());
				n--;
            }
            return r;
        }

        /// <summary>
        /// Adds top two elements
        /// </summary>
        /// <returns>The p.</returns>
        GusStatus Process_p()
        {
			if (_stack.Count < 2)
            {
                return GusStatus.Underflow;
            }
            var ops = PopN(2);
			try
			{
				_stack.Push(checked(ops[1] + ops[0]));
			}
            catch(OverflowException)
			{
				LastError = "Operation caused overflow";
				return GusStatus.Error;
			}
            
			return GusStatus.OK;
        }
        
        /// <summary>
        /// Subtracts top element from penultimate
        /// </summary>
        /// <returns>The m.</returns>
		GusStatus Process_m()
        {
			if (_stack.Count < 2)
            {
                return GusStatus.Underflow;
            }
            var ops = PopN(2);
			try
			{
				_stack.Push(checked(ops[1] - ops[0]));
			}
			catch (OverflowException)
            {
                LastError = "Operation caused overflow";
                return GusStatus.Error;
            }
			return GusStatus.OK;
        }

        /// <summary>
        /// Multiplies top two elements
        /// </summary>
        /// <returns>The t.</returns>
		GusStatus Process_t()
        {
			if (_stack.Count < 2)
            {
                return GusStatus.Underflow;
            }
            var ops = PopN(2);
			try
			{
				_stack.Push(checked(ops[1] * ops[0]));
			}
			catch (OverflowException)
            {
                LastError = "Operation caused overflow";
                return GusStatus.Error;
            }
			return GusStatus.OK;
        }

        /// <summary>
        /// Divides top two elements
        /// </summary>
        /// <returns>The d.</returns>
		GusStatus Process_d()
        {
			if (_stack.Count < 2)
            {
                return GusStatus.Underflow;
            }
			var ops = PopN(2);
            if (ops[0] == 0)
            {
                LastError = "Division by zero (divide)";
                return GusStatus.Error;
            }
			try
			{
				_stack.Push(checked(ops[1] / ops[0]));
			}
			catch (OverflowException)
            {
                LastError = "Operation caused overflow";
                return GusStatus.Error;
            }
			return GusStatus.OK;
        }

        /// <summary>
        /// Replaces top two elements a b with a mod b
        /// </summary>
        /// <returns>The r.</returns>
		GusStatus Process_r()
		{
			if (_stack.Count < 2)
            {
                return GusStatus.Underflow;
            }
			var ops = PopN(2);
            if(ops[0] == 0)
			{
				LastError = "Division by zero (modulus)";
				return GusStatus.Error;
			}
			try
			{
				_stack.Push(checked(ops[1] % ops[0]));
			}
			catch (OverflowException)
            {
                LastError = "Operation caused overflow";
                return GusStatus.Error;
            }
			return GusStatus.OK;
		}

		/// <summary>
		/// Replaces the top two elements a b with a ^ b
		/// </summary>
		/// <returns>The e.</returns>
		GusStatus Process_e()
		{
			if (_stack.Count < 2)
			{
				return GusStatus.Underflow;
			}
			var ops = PopN(2); // 0 was top 1 was penult 0 = a 1 = b
            
			// 0 ^ (n <=0) = NaN
			if (ops[1] == 0 && ops[0] <= 0)
			{
				LastError = string.Format("Invalid Exp {0} ^ {1}", ops[1], ops[0]);
				return GusStatus.Error;
			}

			// >1 ^ -ve = non integer
            // <1 ^ -ve = non integer
			if ((ops[1] > 1 || ops[1] < -1 ) && Math.Abs(ops[0]) == -1)
			{
                LastError = string.Format("Invalid Exp {0} ^ {1}", ops[1], ops[0]);
                return GusStatus.Error;
            }
            
            // This should trap most - id not all overflows
            if(ops[0] * Math.Log(Math.Abs(ops[1])) + 1e-10 > Math.Log(int.MaxValue))
			{
				LastError = "Operation caused overflow";
                return GusStatus.Error;
			}
			try
			{
				double pow = checked(Math.Pow(ops[1], ops[0]));
				_stack.Push(checked((int)pow));
			}
			catch (OverflowException)
            {
                LastError = "Operation caused overflow";
                return GusStatus.Error;
            }

			return GusStatus.OK;

       }

        /// <summary>
        /// Swaps the top two elements
        /// </summary>
        /// <returns>The w.</returns>
		GusStatus Process_w()
		{
			if (_stack.Count < 2)
            {
                return GusStatus.Underflow;
            }
			var ops = PopN(2);
			_stack.Push(ops[0]);
			_stack.Push(ops[1]);
			return GusStatus.OK;
		}
        
        /// <summary>
        /// Creates a copy of the top element
        /// </summary>
        /// <returns>The c.</returns>
		GusStatus Process_c()
		{
			if (_stack.Count < 1)
            {
                return GusStatus.Underflow;
            }
			var ops = PopN(1);
			_stack.Push(ops[0]);
			_stack.Push(ops[0]);
			return GusStatus.OK;
		}

        /// <summary>
        /// Pops and discards the top element
        /// </summary>
        /// <returns>The o.</returns>
		GusStatus Process_o()
		{
			if (_stack.Count < 1)
            {
                return GusStatus.Underflow;
            }
			PopN(1);
			return GusStatus.OK;
		}
        
        /// <summary>
        /// Replaces top element n with nth prime.
        /// </summary>
        /// <returns>The p.</returns>
		GusStatus Process_P()
		{
			if (_stack.Count < 1)
            {
                return GusStatus.Underflow;
            }
			var ops = PopN(1);

			if (_primeCache == null)
            {
				BuildPrimeCache(MAX_NUMBER_OF_PRIMES);

            }

			if (MAX_NUMBER_OF_PRIMES >= ops[0] && ops[0] > 0)
			{
				_stack.Push(_primeCache[ops[0] - 1]);
				return GusStatus.OK;
			}
			LastError = string.Format("Attempted n th prime > {0}", MAX_NUMBER_OF_PRIMES);
			return GusStatus.Error;
		}

        /// <summary>
        /// Replaces top element with it's factorial.
        /// </summary>
        /// <returns>The f.</returns>
		GusStatus Process_F()
		{
            if(_stack.Count < 1)
			{
				return GusStatus.Underflow;
			}
			var ops = PopN(1);
			if (ops[0] <= MAX_FACT)
			{
				_stack.Push(Factorial(ops[0]));
				return GusStatus.OK;
			}
			LastError = string.Format("Attempted factorial > {0}", MAX_FACT);
			return GusStatus.Error;
		}

        /// <summary>
        /// n - Pushes a new element on to the stack with the value of original stackcount + 1
		/// i.e. the ordinal of the value we afre hoping to calculate.
        /// </summary>
        /// <returns>The n.</returns>
		GusStatus Process_n()
		{
			// The term we are interested in guessing - based on original stack length
			_stack.Push(_initialStackCount + 1);
			return GusStatus.OK;
		}

		public void DumpStack()
        {
            foreach (var c in _stack)
            {
                Console.Write(c + "\t");
            }
            Console.WriteLine();
        }
        
        /// <summary>
        /// There are various combinations which we don't want to bother executing.
		/// These include:
		/// Certain pairs we ignore - either pointless (e.g "po"), redundant (e.g. "cp" is better expressed as '2t'), or just nasty (e.g "FF").
		/// Certain combinations countain pointless leading chars which have no useful effect on the final result (e.g. "ppp28:tnm" is basically the same as "8:2tnm") 
		/// Ends in a number - little more than a guess
        /// </summary>
        public static bool IsGoodGusl(string gusl)
		{

			return !(gusl.Any(c => !GusLAtoms.Contains(c)) ||
			         (gusl.Length > 1 && '1' <= gusl.Last() && '@' >= gusl.Last()) ||
			         ContainsBadPairs(gusl) ||
                     ContainsUnimportandLeadingOps(gusl));
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

		/// <summary>
        /// Gets the method info for a give GusL atom
        /// </summary>
        /// <param name="op">Op.</param>
        MethodInfo GetMethodInfo(char op)
        {
			if (_methodInfoCache.ContainsKey(op))
			{
				return _methodInfoCache[op];
			}
			var methodName = string.Format("Process_{0}", op);
			var methodInfo = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			Debug.Assert(methodInfo != null, string.Format("Method {0} not found.", methodName));
			_methodInfoCache[op] = methodInfo;
			return methodInfo;
		}
        
        /// <summary>
        /// Returns the factorial of n
        /// </summary>
	    static int Factorial(int n)
        {
			int result = 1;
            for (int i = 0; i < n; i++)
			{
				result *= (i + 1);
			}
            return result;
        }

        /// <summary>
        /// Populates _primeCache with first n primes
        /// </summary>
        /// <param name="n">N.</param>
        static void BuildPrimeCache(int n)
        {
			_primeCache = new List<int>();
			_primeCache.AddRange(NPrimes(n));
        }

        /// <summary>
        /// Returns first n primes
        /// </summary>
        /// <returns>The rimes.</returns>
        /// <param name="n">N.</param>
		static IEnumerable<int>NPrimes(int n)
		{
			int current = 2;
            while(n > 0)
			{
				if(IsPrime(current))
				{
					yield return current;
					n--;
				}
				current++;
			}
		}

        /// <summary>
        /// Returns true if n is prime
        /// </summary>
        static bool IsPrime(int n)
        {
            if (n < 2)
            {
                return false;
            }
            for (int i = 2; i <= Math.Sqrt(n); i++)
            {
                if (n % i == 0)
                {
                    return false;
                }
            }
            return true;
        }
	}
}

