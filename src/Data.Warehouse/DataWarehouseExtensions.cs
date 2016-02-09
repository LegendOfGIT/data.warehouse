using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Data.Web;

namespace Data.Warehouse
{
    public static class DataWarehouseExtensions
    {
        private static object GetAsMatchingObject(this string value)
        {
            var stringvalue = value as string ?? string.Empty;

            var cultures = new[] {
                CultureInfo.GetCultureInfo("de-DE"),
                CultureInfo.GetCultureInfo("en-US")
            };

            //  Datentyp decimal wurde erkannt >> Rückgabe des String als decimal
            var @decimal = value.GetAsMatchingDecimal();
            if(@decimal.HasValue)
            {
                return (double)@decimal.Value;
            }
            //  Datentyp DateTime wurde erkannt >> Rückgabe des String als DateTime
            var datetime = default(DateTime);
            if (cultures.Any(culture => DateTime.TryParse(value, culture, DateTimeStyles.None, out datetime)))
            {
                return datetime;
            }
            //  Datentyp DateTime wurde erkannt >> Rückgabe des String als DateTime
            var noexpressions = new[] { bool.TrueString.ToLower(), "ja", "yes" };
            var yesexpressions = new[] { bool.FalseString.ToLower(), "nein", "no" };
            if (
                noexpressions.Any(expression => expression == stringvalue.ToLower()) ||
                yesexpressions.Any(expression => expression == stringvalue.ToLower())
            )
            {
                return yesexpressions.Any(expression => expression == stringvalue.ToLower());
            }

            return stringvalue;
        }
        private static decimal? GetAsMatchingDecimal(this string value)
        {
            var @decimal = default(decimal?);

            if(!string.IsNullOrEmpty(value))
            { 
                var number = value;
                var separators = @"(,|\.)";
                var matches = Regex.Matches(number, separators);
                var tokens = Regex.Split(number, separators).Where(s => !Regex.IsMatch(s, separators));
                if (matches != null && matches.Count > 1)
                {
                    number = string.Join(string.Empty, tokens.Take(tokens.Count() - 1));
                    number += "," + tokens.Last();
                }
                number = number.Replace(".", ",");
                tokens = number.Split(',');
                if (tokens.First().Length < 4 && tokens.Last().Length == 3)
                {
                    number = number.Replace(",", string.Empty);
                }

                var d = default(decimal);
                if(decimal.TryParse(
                    number,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("de-DE"),
                    out d
                ))
                {
                    @decimal = d;
                }
            }

            return @decimal;
        }

        /// <summary>
        /// Bildet einen Hashcode für das Wiedererkennen der Information in dem Data-Warehouse
        /// </summary>
        /// <param name="information">Informationssammlung</param>
        /// <returns>Gibt einen Hashcode zurück, der die Inhalte der Informationssammlung in kurzer Form erkennbar macht.</returns>
        public static string GetInformationHashcode(this Dictionary<string, IEnumerable<object>> information)
        {
            var hashcode = default(string);

            hashcode = string.Join(
                string.Empty,
                information?.Select(entry =>
                {
                    return string.Format(
                        "{0}.{1}",
                        //  Hashcode des Dictionary-Key
                        entry.Key.GetHashCode().ToString(),
                        //  Hashcode aller Einträge der Liste unter dem Dictionaryschlüssel
                        string.Join(
                            string.Empty,
                            entry.Value?.Select(token => 
                                token.GetHashCode().ToString()
                            )
                        ).GetHashCode().ToString()
                    );
                })
            //  Hashcode aus allen zuvor ermittelten Hashcodes
            ).GetHashCode().ToString();

            return hashcode;
        }
        /// <summary>
        /// Ermittelt die ID-Information aus der Informationssammlung
        /// </summary>
        /// <param name="information">Informationssammlung</param>
        /// <returns>ID-Information</returns>
        public static string GetInformationId(this Dictionary<string, IEnumerable<object>> information)
        {
            var id = default(string);

            var idexpressions = new[] { ".id", "id" };
            var ids = 
                information != null ? 
                information?.FirstOrDefault(entry => idexpressions.Any(expression => entry.Key.ToLower().EndsWith(expression))) : 
                default(KeyValuePair<string, IEnumerable<object>>)
            ;
            id = (ids?.Value?.FirstOrDefault() ?? new object()).ToString();

            return id;
        }
        /// <summary>
        /// Bereitet die Informationen für die Einspeisung in das Data-Warehouse vor
        /// </summary>
        /// <param name="information">Informationssammlung</param>
        /// <returns>Gibt die vorbereitete Form der Informationssammlung zurück</returns>
        public static Dictionary<string, IEnumerable<object>> PrepareInformation(this Dictionary<string, IEnumerable<object>> information)
        {
            return
                information?.ToDictionary(
                    entry => {
                        var key = entry.Key;

                        var tokens = entry.Key.Split('.');
                        key = tokens?.Length > 1 ? string.Join(".", tokens.Skip(1)) : key;

                        return key;
                    },
                    entry => entry.Value.Select(
                        //  Wert
                        v => 
                            new string(
                                (v as string)
                                //  HTML-Bestandteile entfernen
                                .RemoveHTML()
                                //  Steuerzeichen entfernen
                                .Where(c => !char.IsControl(c)).ToArray()
                            )
                            //  Überzählige Leerzeichen entfernen
                            .Trim()
                            //  Wandele in Objekt mit passenden Typen um
                            .GetAsMatchingObject()
                    )
                )
            ;
        }
    }
}
