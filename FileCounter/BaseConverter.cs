using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCounter
{
	public static class  BaseConverter
	{
		private static long ToDec(string n, int p)
		{
			long result = 0;
			foreach (var d in n)
			{
				var i = d < '0' || d > '9' ? char.ToUpper(d) - 'A' + 10 : d - '0';
				result = result * p + i;
			}
			return result;
		}

		private static string FromDec(long n, int p)
		{
			var result = "";
			for (; n > 0; n /= p)
			{
				var x = n % p;
				result = (char)(x < 0 || x > 9 ? x + 'A' - 10 : x + '0') + result;
			}
			return result;
		}
	
		private static bool Check(this string input, int fromBase)
		{
			var allowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(0, fromBase);
			return Regex.IsMatch(input, string.Format("^[{0}]+$", allowedChars), RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}

		public static bool TryToBase(this string input, int fromBase, int toBase, out string result)
		{
			var check = Check(input, fromBase);
			if (check)
				result = fromBase == toBase ? input : FromDec(ToDec(input, fromBase), toBase);
			else
				result = string.Empty;
			return check;
		}

	}

}

