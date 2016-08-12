using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RestrictionBuilder
{
    public class GetLocs
    {
        public static List<int> Locatelines(string input)
        {
            string bpattern = "BECMG";
            string tpattern = "TEMPO";
            List<int> locs = new List<int>(new int[] { 0 });

            foreach (Match match in Regex.Matches(input, bpattern))
                locs.Add(match.Index);

            foreach (Match match in Regex.Matches(input, tpattern))
                locs.Add(match.Index);
                locs.Add(input.Length);
                locs.Sort();

            return locs;
        }
    }
}