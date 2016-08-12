using System;
using System.Text.RegularExpressions;

namespace RestrictionBuilder
{
    class Program
    {
        static void Main()
        {
            var input = "TAF KQL5 170000Z 1700/1806 27009KT 9999 SCT070 QNH2970INS TEMPO 1702/1704 34015G25KT BECMG 1704/1705 25015G25KT 9999 SCT070 QNH2971INS TEMPO 1706/1707 34025G35KT BECMG 1709/1710 2010G15KT 4800 -TSRA OVC120 QNH2980INS TEMPO 1717/1720 30015G25KT  TX41/1710Z TN24/1703Z";
            var taf = Regex.Replace(input, @"\s+", " ");
            var locs = GetLocs.Locatelines(taf);
            var kql = new Taf(taf, locs);

//            foreach (Match match in Regex.Matches(kql.Lines[0], @"(\d+)/(\d+)"))
//               Console.WriteLine(match.Value);

            kql.ProcessTaf();
            //Console.WriteLine(kql.Id);
        }
    }
}