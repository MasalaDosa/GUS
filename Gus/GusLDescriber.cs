using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gus
{
    public class GusLDescriber
    {
		string _gusl;

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
			return string.Format("I think that each new term is {0}.", Describe());
		}

	    string Describe()
		{
			
			string result = string.Empty;
            string tmp;
            string ignore;
			char c = _gusl.Last();
            _gusl = _gusl.Substring(0, _gusl.Length - 1);
			switch (c)
			{
				case char n when n >= '1' && n <= '@':
					result = string.Format("{0}", _numbers[n - '1']);
					break;
				case 'p':
					result =  string.Format("the sum of {0} and {1}", Describe(), Describe());
					break;
				case 'm':
					result =  string.Format("the difference between {1} and {0}", Describe(), Describe());
					break;
				case 't':
					result =  string.Format("the product of {0} with {1}", Describe(), Describe());
					break;
				case 'd':
					result =  string.Format("the integer part of the quotient when {1} is divided by {0}", Describe(), Describe());
					break;
				case 'r':
					result =  string.Format("the remainder when {1} is divided by {0}", Describe(), Describe());
					break;
				case 'e':
					result =  string.Format("the result of raising {1} to the power of {0}", Describe(), Describe());
					break;
				case 'w':
                    tmp = _gusl;
                    ignore = Describe();
                    result = Describe();
                    _gusl = string.Concat(_gusl, "(", tmp.Substring(_gusl.Length + 1), ")");
                    break;
				case 'c':
                    tmp = _gusl;
					result =  Describe();
                    _gusl = tmp;
					break;
				case 'o':
					// Throw away this
					ignore = Describe();
                    // Then do this
					result =  Describe();
					break;
				case 'P':
					result =  string.Format("the prime who’s position in the sequence of primes is {0}", Describe());
					break;
				case 'F':
					result =  string.Format("the factorial of {0}", Describe());
					break;
				case 'n':
					result =  string.Format("the number of the new term");
					break;
                // These represent the terms = 128 is the last term, 129 is the penultimate etc.
				case char n when n >= 128 && n <= 143:
					result = TermDescription(n - 128);
					break;
                case ')':
                    result = Describe();
                    var countParens = 1;
                    do
                    {
                        c = _gusl.Last();
                        countParens = countParens + (c == '(' ? -1 : 0) - (countParens == ')' ? -1 : 0);
                        _gusl = _gusl.Substring(0, _gusl.Length - 1);
                    } while (countParens != 0);
                    break;
			}    
			return result;
		}
    
	    string TermDescription(int n)
		{
			string result = string.Empty;
            
			if (n == 0)
            {
                result = "the last term";
            }
            else if (n == 1)
            {
                result = "the penultimate term";
            }
            else
            {
				result = string.Concat("the ",string.Concat(Enumerable.Repeat("ante-", n - 1)), "penultimate term");
            }
			return result;
		}
	}
}
