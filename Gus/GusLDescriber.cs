using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gus
{
    public class GusLDescriber
    {
		string _gusl;
        int _ptr;

		static string[] _numbers =
		{
			"one", "two", "three", "four", "five", "six"," seven", "eight",
			"nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen"
		};

        public GusLDescriber(string gusl)
        {
			// These represent the last 16 terms
			for (int i = 128; i < 144; i++)
			{
				gusl = (char)i + gusl;
			}
			_gusl = gusl;
		}
        
   
		public string GetDescription()
	    {
			_ptr = _gusl.Length - 1;
			string result = string.Format("I think that each new term is {0}", Describe());
			result = string.Join(" ", result.Split(' ', StringSplitOptions.RemoveEmptyEntries));
			return result;
		}

	    string Describe()
		{
			var cptr = _ptr;
			_ptr--;

			string result = string.Empty;
			char c = _gusl[cptr];
            
			switch (c)
			{
				case char n when n >= '1' && n <= '@':
					result = string.Format(" {0} ", _numbers[n - '1']);
					break;
				case 'p':
					result =  string.Format(" the sum of {1} and {0} ", Describe(), Describe());
					break;
				case 'm':
					result =  string.Format(" the difference between {1} and {0} ", Describe(), Describe());
					break;
				case 't':
					result =  string.Format(" the product of {1} with {0} ", Describe(), Describe());
					break;
				case 'd':
					result =  string.Format(" the integer part of the quotient when {1} is divided by {0} ", Describe(), Describe());
					break;
				case 'r':
					result =  string.Format(" the remainder when {1} is divided by {0} ", Describe(), Describe());
					break;
				case 'e':
					result =  string.Format(" the result of raising {1} to the power of {0} ", Describe(), Describe());
					break;
				case 'w':
					char temp = _gusl[_ptr];
					Describe();
					result = Describe();
                    // Reinstate the thing we just threw away.
					_ptr++;
					_gusl = _gusl.Substring(0, _ptr) + temp + _gusl.Substring(_ptr + 1);
                    
					break;
				case 'c':
					string s =  Describe();
					_ptr++;
					result =  s;
					break;
				case 'o':
					// Throw away this
					Describe();
					result =  Describe();
					break;
				case 'P':
					result =  string.Format(" the prime who’s position in the sequence of primes is {0} ", Describe());
					break;
				case 'F':
					result =  string.Format(" the factorial of {0} ", Describe());
					break;
				case 'n':
					result =  string.Format(" the number of the new term ");
					break;
                // These represent the terms = 128 is the last term, 129 is the penultimate etc.
				case char n when n >= 128 && n <= 143:
					result = TermDescription(n - 128);
					break;
			}    
			return result;
		}

	    string TermDescription(int n)
		{
			string result = string.Empty;
            
			if (n == 0)
            {
                result = " the last term ";
            }
            else if (n == 1)
            {
                result = " the penultimate term ";
            }
            else
            {
				result = string.Concat(" the ",string.Concat(Enumerable.Repeat("ante-", n - 1)), "penultimate term ");
            }
			return result;
		}
	}
}
