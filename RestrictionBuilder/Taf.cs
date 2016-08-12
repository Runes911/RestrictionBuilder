using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RestrictionBuilder
{
    public class Taf
    {
        public string Id { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public int Maxtemp { get; private set; }
        public int Mintemp { get; private set; }
        public List<string> Lines { get; private set; }
        private string RedImpacts = @"(-TSRA|TSRA|\+TSRA|RA|\+RA|SHRA|\+SHRA|DZ|\+DZ|-SN|SN|\+SN|-SHSN|SHSN|\+SHSN)";
        private string YellowImpacts = @"(VCTS|-RA|-SHRA|VCSH|-DZ|VCSN)";
        private List<int> Rwy = new List<int>(new int[] { 03, 210 });
        private List<int> Cigmins = new List<int>(new int[] { 080, 120 });
        private List<int> Windmins = new List<int>(new int[] { 25, 31 });
        private List<int> Xwindmins = new List<int>(new int[] { 10, 17 });
        private List<int> Vismins = new List<int>(new int[] { 4800, 3200 });
        public List<string> Impacts { get; private set; }

        public Taf(string tafinput, List<int> locs)
        {

            Lines = new List<string>();

            for (int i = 0; i < locs.Count - 1; i++)
            {
                Lines.Add(tafinput.Substring(locs[i], locs[i + 1] - locs[i]));
            }

            Id = Lines[0].Substring(Lines[0].IndexOf("K"), 4);
            Start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, Convert.ToInt32(Lines[0].Substring(Lines[0].IndexOf(@"/") - 4, 2)), Convert.ToInt32(Lines[0].Substring(Lines[0].IndexOf(@"/") - 2, 2)), 00, 00, DateTimeKind.Utc);
            End = Start.AddHours(30);
            Maxtemp = Convert.ToInt32(Convert.ToInt32(Lines[Lines.Count - 1].Substring(Lines[Lines.Count - 1].IndexOf("TX") + 2, 2)));
            Mintemp = Convert.ToInt32(Convert.ToInt32(Lines[Lines.Count - 1].Substring(Lines[Lines.Count - 1].IndexOf("TN") + 2, 2)));
        }

        public void DisplayTaf()
        {
            foreach (string line in Lines)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine(Lines.Count);

        }

        public void ProcessTaf()
        {
            Impacts = new List<string>();
            const string becmgPattern = "BECMG";
            const string tempoPattern = "TEMPO";
            const string windPattern = @"(\d\d\d\d\dKT|\d\d\d\d\dG\d\dKT)";
            const string skyconPattern = @"(-TSRA|TSRA|\+TSRA|RA|\+RA|SHRA|\+SHRA|DZ|\+DZ|-SN|SN|\+SN|-SHSN|SHSN|\+SHSN|VCTS|-RA|-SHRA|VCSH|-DZ|VCSN)";
            const string cloudPattern = @"(BKN\d\d\d|OVC\d\d\d)";
            const string visibilityPattern = @"(\s\d\d\d\d\s)";
            const string timePattern = @"(\d\d\d\d)/(\d\d\d\d)";
            const string visibilityRestricters = @"HZ|FG|FU|BR";

            for (var i = 0; i < Lines.Count; i++)
            {
                var becmgLine = Regex.Match(Lines[i], becmgPattern);
                var tempoLine = Regex.Match(Lines[i], tempoPattern);
                var windTest = Regex.Match(Lines[i], windPattern);
                var skyConTest = Regex.Match(Lines[i], skyconPattern);
                var cloudTest = Regex.Match(Lines[i], cloudPattern);
                var visTest = Regex.Match(Lines[i], visibilityPattern);
                var timeTest = Regex.Match(Lines[i], timePattern);
                var firstLineTime = Regex.Match(Lines[0], timePattern);
                var visRestrictTest = Regex.Match(Lines[i], visibilityRestricters);
                
                if (windTest.Success && Convert.ToInt32(windTest.Value.Trim().Substring(windTest.Value.Length - 4,2)) >= Windmins[0] || skyConTest.Success || cloudTest.Success || visTest.Success && Convert.ToInt32(visTest.Value.Trim()) < Vismins[0])
                {
                    if (i == 0) //Is this the first line? Then execute.
                    {
                        var temponextLine = Regex.Match(Lines[i + 1], tempoPattern);
                        var nextLinetimeTest = Regex.Match(Lines[i + 1], timePattern);

                        if (temponextLine.Success)
                        {
                            Impacts.Add("(" + timeTest.Value.Substring(2, 2) + "-" + nextLinetimeTest.Value.Substring(2, 2) + ")" + " ");
                        }
                        else
                        {
                            Impacts.Add("(" + timeTest.Value.Substring(2, 2) + "-" + nextLinetimeTest.Value.Substring(7, 2) + ")" + " ");
                        }
                        

                        if (windTest.Success && Convert.ToInt32(windTest.Value.Trim().Substring(windTest.Value.Length - 4, 2)) > Windmins[0])
                        {
                            Impacts[i] = Impacts[i] + windTest.Value + ", ";
                        }

                        if (skyConTest.Success)
                        {
                            Impacts[i] = Impacts[i] + skyConTest.Value + ", ";
                        }

                        if (cloudTest.Success)
                        {
                            Impacts[i] = Impacts[i] + cloudTest.Value + ", ";
                            
                        }

                        if (visTest.Success && Convert.ToInt32(visTest.Value.Trim()) < Vismins[0])
                        {
                            Impacts[i] = Impacts[i] + Visconvert(Convert.ToInt32(visTest.Value.Trim())) + "SM" + " " + visRestrictTest.Value + ", ";

                        }
                    }

                    else //Not the first line. So execute this:
                    {

                        if (becmgLine.Success) //What type of line are we evaluating? Becoming line:
                        {
                            Impacts.Add("(" + timeTest.Value.Substring(7, 2) + "-");

                            if (i < Lines.Count - 1) //We need to evaluate if this is the last line. If it is, than the ending time for the line will be the end of the TAF.
                            {
                                var nextLinetimeTest = Regex.Match(Lines[i + 1], timePattern);
                                var temponextLine = Regex.Match(Lines[i + 1], tempoPattern);

                                if (temponextLine.Success && nextLinetimeTest.Value.Substring(2, 2) == timeTest.Value.Substring(7, 2))  //Testing to make sure the next line is a TEMP AND the end time will not be the same.
                                {
                                    Impacts.RemoveAt(Impacts.Count - 1);
                                }

                                else if (temponextLine.Success)  //The next line must not be a duplicate end time, so we can use it.
                                {
                                    Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + nextLinetimeTest.Value.Substring(2, 2) + ")" + " ";
                                }

                                else
                                {
                                    Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + nextLinetimeTest.Value.Substring(7, 2) + ")" + " ";
                                }
                                
                            }

                            else
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + firstLineTime.Value.Substring(7, 2) + ")" + " ";
                            }


                            if (windTest.Success && Convert.ToInt32(windTest.Value.Trim().Substring(windTest.Value.Length - 4, 2)) >= Windmins[0])
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + windTest.Value + ", ";
                            }

                            if (skyConTest.Success)
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + skyConTest.Value + ", ";
                            }

                            if (cloudTest.Success)
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + cloudTest.Value + ", ";

                            }

                            if (visTest.Success && Convert.ToInt32(visTest.Value.Trim()) < Vismins[0])
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + Visconvert(Convert.ToInt32(visTest.Value.Trim())) + "SM" + " " + visRestrictTest.Value;

                            }
                        }

                        else //Not a becoming line, must be a tempo. 
                        {

                            
                            Impacts.Add("(" + timeTest.Value.Substring(2, 2) + "-" + timeTest.Value.Substring(7, 2) + ")" + " ");

                            if (windTest.Success && Convert.ToInt32(windTest.Value.Trim().Substring(windTest.Value.Length - 4, 2)) >= Windmins[0])
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + windTest.Value + ", ";
                            }

                            if (skyConTest.Success)
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + skyConTest.Value + ", ";
                            }

                            if (cloudTest.Success)
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + cloudTest.Value + ", ";

                            }

                            if (visTest.Success && Convert.ToInt32(visTest.Value.Trim()) < Vismins[0])
                            {
                                Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + Visconvert(Convert.ToInt32(visTest.Value.Trim())) + "SM" + " " + visRestrictTest.Value;
                            }

                            if (i < Lines.Count - 1 && Impacts.Count > 3) //Making sure we arent at the last line and the previous line needs to be inserted between the TEMPO and BECMG line.
                            {
                                var nextLinetimeTest = Regex.Match(Lines[i + 1], timePattern);
                                var temponextLine = Regex.Match(Lines[i + 1], tempoPattern);
                                var tempoprevLine = Regex.Match(Lines[i - 1], tempoPattern);

                                if (temponextLine.Success) //If the next line is a TEMPO we dont need to add more lines.
                                {
                                    continue;
                                }

                                else if (tempoprevLine.Success && timeTest.Value.Substring(7, 2) != nextLinetimeTest.Value.Substring(7, 2)) //If the previous line is a TEMPO we must adjust the line it pulls from
                                {
                                    Impacts.Add("(" + timeTest.Value.Substring(7, 2) + "-" + nextLinetimeTest.Value.Substring(7, 2) + ")" + " ");
                                    Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + Impacts[Impacts.Count - 4].Substring(8);
                                }

                                else //Lastly, if none of the previous is true, then we can assume the next line is ok to pull from.
                                {
                                    Impacts.Add("(" + timeTest.Value.Substring(7, 2) + "-" + nextLinetimeTest.Value.Substring(7, 2) + ")" + " ");
                                    Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + Impacts[Impacts.Count - 3].Substring(8);
                                }
                            }

                            else if (i == Lines.Count - 1)
                            {
                                var tempoprevLine = Regex.Match(Lines[i - 1], tempoPattern);

                                if (tempoprevLine.Success) //If the previous line is a TEMPO we must adjust the line it pulls from
                                {
                                    Impacts.Add("(" + timeTest.Value.Substring(7, 2) + "-" + firstLineTime.Value.Substring(7, 2) + ")" + " ");
                                    Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + Impacts[Impacts.Count - 4].Substring(8);
                                }

                                else //Lastly, if none of the previous is true, then we can assume the next line is ok to pull from.
                                {
                                    Impacts.Add("(" + timeTest.Value.Substring(7, 2) + "-" + firstLineTime.Value.Substring(7, 2) + ")" + " ");
                                    Impacts[Impacts.Count - 1] = Impacts[Impacts.Count - 1] + Impacts[Impacts.Count - 3].Substring(8);
                                }
                            }
                        }
                    }
                }
            }
            for (var i = 0; i < Impacts.Count; i++) //Now that the impacts are finalized, they need to be trimmed and the trailing comma removed.
            {
                Impacts[i] = Impacts[i].Trim();
                var commaTail = Regex.Match(Impacts[i], @"...,$");

                if (commaTail.Success)
                    Impacts[i] = Impacts[i].Remove(Impacts[i].Length - 1, 1);
            }
            foreach (var impact in Impacts)
            {
                Console.WriteLine(impact);
            }
        }

        public string Visconvert(int vis)
        {
            switch (vis)
            {
                case 0200:
                    return "1/8";
                    
                case 0400:
                    return "1/4";
                    
                case 0600:
                    return "3/8";
                    
                case 0800:
                    return "1/2";
                    
                case 1000:
                    return "5/8";
                    
                case 1200:
                    return "3/4";
                    
                case 1400:
                    return "7/8";
                    
                case 1600:
                    return "1";
                    
                case 1800:
                    return "1 1/8";
                    
                case 2000:
                    return "1 1/4";

                case 2200:
                    return "1 3/8";

                case 2400:
                    return "1 1/2";

                case 2600:
                    return "1 5/8";

                case 2800:
                    return "1 3/4";

                case 3000:
                    return "1 7/8";

                case 3200:
                    return "2";

                case 3600:
                    return "2 1/4";

                case 4000:
                    return "2 1/2";

                case 4400:
                    return "2 3/4";

                case 4800:
                    return "3";

                case 6000:
                    return "4";

                case 8000:
                    return "5";

                case 9000:
                    return "6";

                default:
                    return "10";
            }
        }
    }
}
