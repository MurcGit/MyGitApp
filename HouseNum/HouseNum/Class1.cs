using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace AddrHo
{
    class Ident
    {
        private const string conString = "Server=virt30; Initial Catalog=RefAddress; Integrated Security=True; Pooling=True; Connection Timeout=60;";
        private const string conStringInDll = "context connection=true";

        private const string cSqlIdent = "SELECT ShortName FROM RefAddress.dbo.RefTempShortName with(nolock) Order by ShortName";

        [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]

        public static List<string> Idents()
        {
            List<string> res = new List<string>();
            using (SqlConnection con = new SqlConnection(conString))
            {
                con.Open();
                using (SqlCommand com = new SqlCommand(cSqlIdent, con))
                {
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                res.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            return res;
        }

        public List<string> adrIdent = Idents();
        //    new List<string>()
        //{ "а/я", "обл", "респ", "край", "АО", "Аобл", "г", "гп", "пгт", "д", "с", "р-н", "мкр", "кв-л", "пр-кт", "линия", "тракт", "ул", "ш", "проезд" };

        public List<string> hoIdent = new List<string>() { "д.", "вл", "стр", "соор", "к.", "лит", "пав", "бл", "фл", "уч" };
    }

    public static class Program
    {
        static string Reg(string str)
        {
            //@"[,]([^,]*?(\bпроезд\b))+?[,]" // убрать " -" "- " "17 А" "17-А" "стр." "23/8" "пр-кт"
            string first = "", before = "";
            Ident id = new Ident();
            int indexOfsubstring = 0, indexOfident = 0;

            // Пункт "А"
            str = ", " + str + ",";

            // Пункт "Б"
            str = Regex.Replace(str, @"(\()([^()]*)(стр),$", @"$3 $1$2");

            // Пункт "В"
            str = Regex.Replace(str, @"\([^(]*?\)", @"");
            str = Regex.Replace(str, @"\([^()]*?$", @"");

            // Пункт "Г"
            str = Regex.Replace(str, @"[—–]", @"-");
            str = Regex.Replace(str, @"[\\]", @"/");
            str = Regex.Replace(str, @";", @",");
            str = str.Replace(" -", "-").Replace("- ", "-");

            // Пункт "Д"
            str = Regex.Replace(str, @"\b(дом|домовладение|домовлад|здание|здан|зд)\b", @" д. ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(владение|владен|влад)\b", @" вл ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(строение|строен)\b", @" стр ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(сооружение|сооружен|сооруж)\b", @" соор ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(корпус|корп|кор|корпуса)\b", @" к. ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(литера|литер|литр)\b", @" лит ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(павильон)\b", @" пав ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(блок)\b", @"бл", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(флигель|флиг)\b", @"фл", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b(участок|участ|уч-к)\b", @" уч ", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\b([0-9]{1,4})[\W_]{1}([а-яa-z]{1})[, ]", @"$1$2 ", RegexOptions.IgnoreCase);
            str = str.Replace("  ", " ");

            // Пункт "Е"
            str = Regex.Replace(str, @"\b([а-яa-z]?)(стр|лит)([а-яa-z]?)\b", @"$1 $2 $3", RegexOptions.IgnoreCase);
            //str = Regex.Replace(str, @"\b([а-яa-z]?)(к[\.]?)([абг-я]?)\b", @"$1 $2 $3", RegexOptions.IgnoreCase); //мкр извращается

            // Пункт "Ж"
            before = Regex.Match(str, @",[ ]?[^,0-9 ]+[ ][^, ]+[ ][^,0-9 ]+").ToString();
            while (before != "")
            {
                first = before.Substring(1).Trim();
                string[] pars = first.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (id.adrIdent.Exists(x => x.Contains(pars[0].ToLower())) && id.adrIdent.Exists(x => x.Contains(pars[2].ToLower())))
                {
                    str = Regex.Replace(str, before, ", " + pars[1] + " " + pars[0] + ", " + pars[2]);
                    before = Regex.Match(str, @",[ ]?[^,0-9 ]+[ ][^, ]+[ ][^,0-9 ]+").ToString();
                }
                else
                {
                    indexOfsubstring = str.IndexOf(before) + 1;
                    before = Regex.Match(str.Substring(indexOfsubstring), @",[ ]?[^,0-9 ]+[ ][^, ]+[ ][^,0-9 ]+").ToString();
                }
            }
            before = ""; first = "";
            indexOfsubstring = 0;

            // Пункт "З"
            foreach (Match be in Regex.Matches(str, @"\b[^, ]+[ ][^, ]+"))
            {
                string[] pars = be.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (id.adrIdent.Exists(x => x.Contains(pars[1].ToLower())) && (!id.adrIdent.Exists(x => x.Contains(pars[0].ToLower()))))
                {
                    first = pars[1];
                }
                else if ((!id.adrIdent.Exists(x => x.Contains(pars[1].ToLower()))) && (id.adrIdent.Exists(x => x.Contains(pars[0].ToLower()))))
                {
                    str = str.Replace(" " + first + " " + pars[0] + " ", " " + first + ", " + pars[0] + " ");
                }
                else if (id.adrIdent.Exists(x => x.Contains(pars[0].ToLower())) && id.adrIdent.Exists(x => x.Contains(pars[1].ToLower())))
                {
                    str = Regex.Replace(str, be.ToString(), pars[0] + ", " + pars[1]);
                }
            }

            // Пункт "И"
            foreach (Match be in Regex.Matches(str, @",[ ]?[^,0-9 ]+[ ][0-9]+([ ]|[а-яa-z]\b)", RegexOptions.IgnoreCase))
            {
                first = be.ToString().Substring(1).Trim();
                string[] pars = first.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (id.adrIdent.Exists(x => x.Contains(pars[0].ToLower())))
                {
                    str = Regex.Replace(str, be.ToString(), ", " + pars[1] + " " + pars[0] + ",");
                }
            }
            str = str.Replace("  ", " ").Replace(",,", ",");

            // Пункт "К" //'Санкт-Петербург г, 10-я дом 57Б линия' --> 'Санкт-Петербург г, линия 10-я дом 57Б'
            foreach (Match be in Regex.Matches(str, @"[,]([^,]*)([^, ]+[ ][^, ]+)([ ][^,0-9 ]+)[ ]?[,]"))
            {
                first = be.ToString().TrimEnd(',').Trim();
                string[] pars = first.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                indexOfsubstring = pars.Length;
                if ((id.adrIdent.Exists(x => x.Contains(pars[indexOfsubstring - 1].ToLower())) && id.hoIdent.Exists(x => x.Contains(pars[indexOfsubstring - 3])) && pars[indexOfsubstring - 1].ToLower() != "а/я")
                || (pars[indexOfsubstring - 1].ToLower() != "а/я" && id.adrIdent.Exists(x => x.Contains(pars[indexOfsubstring - 1].ToLower()))
                && Regex.IsMatch(pars[indexOfsubstring - 3], @"^[0-9]{1,4}") && Regex.IsMatch(pars[indexOfsubstring - 2], @"^[0-9]{1,4}")))
                {
                    first = ", " + pars[indexOfsubstring - 1] + " " + string.Join(" ", pars, 1, indexOfsubstring - 2) + ",";
                    str = Regex.Replace(str, be.ToString(), first);
                }
            }

            // Пункт "Л" //'Санкт-Петербург г, линия 10-я дом 57Б' --> 'Санкт-Петербург г, линия 10-я, дом 57Б'
            str = Regex.Replace(str, @"([^, ])[ ]\b(д.|вл|стр|соор|к.|лит|пав|бл|фл|уч)([ ][^, ])", @"$1, $2$3", RegexOptions.IgnoreCase); //\\сделать иденты домов с точкой//\\

            // Пункт "М" //'Санкт-Петербург г, линия 10-я 57Б' --> 'Санкт-Петербург г, линия 10-я, 57Б'
            str = Regex.Replace(str, @"([0-9][а-яa-z]?)( [0-9]{1,4}[а-яa-z]?\b)", @"$1,$2", RegexOptions.IgnoreCase);

            foreach (Match be in Regex.Matches(str, @",[ ]?[^,0-9 ]+[ ][0-9]+([ ]|[а-яa-z]\b)", RegexOptions.IgnoreCase)) //'Санкт-Петербург г, линия 10-я, 57Б' --> 'Санкт-Петербург г, 10-я линия, 57Б' 
            {
                first = be.ToString().Substring(1).Trim();
                string[] pars = first.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (id.adrIdent.Exists(x => x.Contains(pars[0].ToLower())))
                {
                    str = Regex.Replace(str, be.ToString(), ", " + pars[1] + " " + pars[0] + ",");
                }
            }

            // Пункт "Н" 
            foreach (Match be in Regex.Matches(str, @"\b[^,0-9 ]+[ ][^,0-9 ]+"))
            {
                string[] pars = be.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (id.adrIdent.Exists(x => x.Contains(pars[0].ToLower())) && id.adrIdent.Exists(x => x.Contains(pars[1].ToLower())))
                {
                    str = Regex.Replace(str, be.ToString(), pars[0] + ", " + pars[1]);
                }
            }
            str = str.Replace("  ", " ");

            // Пункт "О"
            foreach (Match be in Regex.Matches(str, @"\b[^, ]+[ ][а-яa-z]{2,}[ ][0-9IVX]", RegexOptions.IgnoreCase))
            {
                string[] pars = be.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (Regex.IsMatch(pars[0], @"^([0-9]{1,4}|[а-яa-z]{1}|д.|вл|стр|соор|к.|лит|пав|бл|фл|уч)$", RegexOptions.IgnoreCase) &&
                (!id.adrIdent.Exists(x => x.Contains(pars[1]))) && (!id.hoIdent.Exists(x => x.Contains(pars[1]))) &&
                Regex.IsMatch(pars[2], @"\b([0-9IVX]+)"))
                {
                    str = str.Replace(be.ToString(), pars[0] + ", " + pars[1] + " " + pars[2]);
                }
            }

            // Пункт "П"
            str = Regex.Replace(str, @",[ ]?\b(д.|вл|стр|соор|к.|лит|пав|бл|фл|уч)[ ]([0-9]{1,4}?[а-яa-z]?)[ ]([^0-9, ]+)\b", @", $1 $2, $3", RegexOptions.IgnoreCase);
            foreach (Match be in Regex.Matches(str, @",[ ]?([0-9]{1,4}[а-яa-z]?)[ ]\b([^,0-9 ]{2,})\b[ ][0-9]", RegexOptions.IgnoreCase))
            {
                string[] pars = be.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (id.adrIdent.Exists(x => x.Contains(pars[2])) && pars[1] != "а/я")
                {
                    str = str.Replace(be.ToString(), ", " + pars[1] + " " + pars[2] + ", " + pars[3]);
                }
                else if (pars[2].ToLower() == "а/я")
                {
                    str = str.Replace(be.ToString(), ", " + pars[1] + ", " + pars[2] + " " + pars[3]);
                }
            }

            // Пункт "Р"
            str = Regex.Replace(str, @"([0-9]{1,4}[а-яa-z]{1}) ([кk]) ([0-9]{1,4})", @"$1, к. $3", RegexOptions.IgnoreCase);

            // Пункт "С"
            first = Regex.Match(str, @"\b(д.|вл|стр|соор|к.|лит|пав|бл|фл|уч)[, ]").ToString();
            indexOfident = str.IndexOf(first);
            first = "";
            first = Regex.Match(str, @"[, ][0-9]{1,4}").ToString();
            indexOfsubstring = str.IndexOf(first);
            if ((indexOfsubstring < indexOfident && indexOfident > 0 && indexOfsubstring > 0) || (indexOfsubstring != 0 && indexOfident == 0))
            {
                str = str.Remove(0, indexOfsubstring);
            }
            else if ((indexOfident < indexOfsubstring && indexOfsubstring > 0 && indexOfident > 0) || (indexOfident != 0 && indexOfsubstring == 0))
            {
                str = str.Remove(0, indexOfident);
            }

            return str;
        }

        static string Spl(string final)
        {
            final = Reg(final);
            bool flag = false;
            string rep = "";
            string[] pars = final.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            final = "";
            foreach (string part in pars)
            {
                rep = part.Replace("д.", "").Replace("вл", "").Replace("стр", "").Replace("соор", "").Replace("к.", "").Replace("лит", "").Replace("пав", "").Replace("бл", "").Replace("фл", "").Replace("уч", "").Replace("  ", " ");
                if (Regex.IsMatch(rep, @"^[а-яa-z]?[ ]?[0-9]{0,4}[/]?[0-9]{0,4}[а-яa-z]?[ ]?[0-9]{0,4}$", RegexOptions.IgnoreCase))
                {
                    final += part;
                    flag = true;
                }
                else
                {
                    flag = false;
                }
                if (final != "" && (!flag))
                {
                    break;
                }
            }
            final = final.Replace("  ", " ").Trim();
            final = Regex.Replace(final, @"(.*[0-9]) [0-9]+[а-яa-z]?$", @"$1");
            final = Regex.Replace(final, @"(.*[0-9]) ([0-9]+[а-яa-z ]?)(д.|вл|стр|соор|к.|лит|пав|бл|фл|уч)?$", @"$1 $3 $2");

            return final;
        }
    }
}