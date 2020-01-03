using System.Text;
using System.Text.RegularExpressions;

namespace System.Collections.Generic
{
  public static class Extensions
  {

    /// <summary>
    /// Formats the characters in the list so that there is a space between two underscores, 
    /// but not between two letters or a letter and an underscore
    /// </summary>
    /// <param name="value">The <see cref="List{char}"/> of the letters to make pretty</param>
    /// <returns>A pretty <see cref="System.String"/> to display</returns>
    public static string ToHangmanString(this List<char> value)
    {
      // Make sure there is work to be done
      if (value == null || value.Count == 0)
        return string.Empty;

      StringBuilder sb = new StringBuilder();

      // First, add a space after each character
      foreach (var c in value)
        sb.Append(c + " ");

      // Now use some Regular Expression magic to strip out some spaces
      return Regex.Replace(sb.ToString(), @"( )*([a-zA-Z])( )*", @"$2").Trim();
    }
  }
}
