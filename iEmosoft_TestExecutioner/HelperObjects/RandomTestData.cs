using System;
using System.Collections.Generic;
using System.Linq;

namespace aUI.Automation.HelperObjects
{
    public class RandomTestData
    {
        public Randomizer Rnd = new();

        public RandomTestData()
        {
            FirstNames = new List<string>() { "Mike", "Michael", "Michelle", "Tom", "Tommy", "Thomas", "Eric", "Erick", "Jon", "John", "Jonny", "Theo", "Theodore", "Teddy", "Gretchen", "Sarah", "Sara", "Grace", "Gracie", "Evie", "Eveline", "Evelyn", "Ron", "Ronny", "Beau", "Muriel", "Pat", "Patrick", "Elizabeth", "Liz", "Julie", "Helen", "Joe", "Joesph", "Joey", "Teresa", "Alex", "Betsy", "Katie", "Kate", "Jenny", "Jennifer", "Angela", "Sadie", "Melissa", "Missy" };
            LastNames = new List<string>() { "Jones", "Bridgewater", "Krammer", "Oden", "Thomason", "Jackson", "Nelson", "Wilson", "Olson", "Olsen", "Mullenham", "Emo", "Caauwe", "Mertz", "Linkert", "Washington", "Bush", "Aniston", "Montana", "Manning", "Ponder", "Bauer", "Salzwadel", "Mcguire", "Anderson", "Mulner", "Hagen", "Austin", "Williams", "Cobb", "Pucket", "Mauer", "Hurbek", "Erickson", "Walser", "Ryans", "Arnold", "Kelly", "Smith", "Johnson", "Wesson" };
            CompanyNames = new List<string>() { "Mircosoft", "iEmosoft", "Apple", "Apple Computer", "General Electric", "Protolabs", "ADP", "Make Music", "Portoco", "Sorin", "Eue, Rachie and Associates", "Target", "Sears", "Kmart", "Wards", "Ebay", "Amazon", "Walmart", "Tires Plus", "Discount Tire", "Bloomingdales", "Lunds", "Super Value", "Cub Foods", "Holiday Gas", "BP", "Verizon", "ATT", "Medtronic", "Boston Science", "Remington", "Smith and Wesson", "Good Year" };
            Countries = new List<string>() { "United States", "Canada", "Mexico", "Liberia", "Houndouras", "Brazil", "France", "Germany", "United Kingdom", "Russia", "Japan", "China", "Austrailia", "Sweden", "Norway" };
            States = new List<string>() { "Alabama", "Alaska", "Connecticut", "Minnesota", "Wisconsin", "Illinois", "Iowa", "North Dakota", "South Dakota", "Kentucky", "Tennesee", "Texas", "New York", "California", "Wyoming", "Oregon", "Maine", "Florida" };
            Cities = new List<string>() { "Jacksonville", "Minneapolis", "St. Paul", "Duluth", "Hopkins", "Hastings", "Little Falls", "Paris", "Rome", "London", "Moscow", "York", "Waconia", "Minnetonka", "Wayzata", "Jamestown", "Montgomery", "Excelsior" };
            Vehicles = new List<(int, string, string)>() { (2010, "Toyota", "Camry"), (2011, "Toyota", "4Runner"), (2014, "Honda", "Accord"), (2020, "Nissan", "Altima") };
        }
        public List<string> FirstNames { get; set; }
        public List<string> LastNames { get; set; }
        public List<string> CompanyNames { get; set; }
        public List<string> Countries { get; set; }
        public List<string> Cities { get; set; }
        public List<string> States { get; set; }
        public List<(int, string, string)> Vehicles { get; set; }

        public string GetRandomString(int length, bool spaces = false)
        {
            var ops = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            if (spaces)
            {
                ops += " ";
            }

            var stringChars = new char[length];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = ops[Rnd.Next(0, ops.Length)];
            }

            return new string(stringChars);
        }

        public string GetRandomAlphaNumericString(int length, bool spaces = false)
        {
            var ops = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            if (spaces)
            {
                ops += " ";
            }

            var stringChars = new char[length];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = ops[Rnd.Next(0, ops.Length)];
            }

            return new string(stringChars);
        }

        public string GetRandomFirstName(int appendGuidDigits = 0)
        {
            return FirstNames[Rnd.Next(0, FirstNames.Count - 0)] + GetGuidSubString(appendGuidDigits);
        }

        public string GetRandomLastName(int appendGuidDigits = 0)
        {
            return LastNames[Rnd.Next(0, LastNames.Count - 0)] + GetGuidSubString(appendGuidDigits);
        }

        public string GetRandomCompanyName(int appendGuidDigits = 0)
        {
            return CompanyNames[Rnd.Next(0, CompanyNames.Count - 0)] + GetGuidSubString(appendGuidDigits);
        }

        public string GetRandomCity(int appendGuidDigits = 0)
        {
            return Cities[Rnd.Next(0, Cities.Count - 0)] + GetGuidSubString(appendGuidDigits);
        }

        public string GetRandomState()
        {
            return States[Rnd.Next(0, States.Count - 0)];
        }

        public (int Year, string Make, string Model) GetRandomVehicle()
        {
            return Vehicles[Rnd.Next(0, Vehicles.Count - 0)];
        }

        public string GetGuidSubString(int guidLength)
        {
            if (guidLength > 0)
            {
                return Guid.NewGuid().ToString().Substring(0, guidLength);
            }

            return "";
        }

        public string GetRandomVIN()
        {

            var ops = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
            var stringChars = new char[17];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = ops[Rnd.Next(0, ops.Length)];
            }

            return new string(stringChars);
        }

        public string GetRandomCountry()
        {
            return Countries[Rnd.Next(0, Countries.Count - 0)];
        }

        public string GetRandomPostalCode(int appendGuidDigits = 0)
        {
            return string.Format("{0}{1}", Rnd.Next(3, 9), GetRandomDigits(4)) + GetGuidSubString(appendGuidDigits);
        }

        public string GetRandomEmailAddress(int appendGuidDigits = 0)
        {
            return GetRandomEmailAddress(GetRandomFirstName(), GetRandomLastName()) + GetGuidSubString(appendGuidDigits);
        }

        public bool GetRandomBoolean()
        {
            return Rnd.Next(0, 2) == 1;
        }

        public string GetRandomEmailAddress(string firstName, string lastName)
        {
            string company = GetRandomCompanyName().Replace(" ", "").Replace("-", "").Replace(".", "").Replace(",", "");
            string email = string.Format("{0}.{1}@{2}{3}{4}", firstName.Replace(".", "").Replace("'", ""), lastName.Replace(".", "").Replace("'", ""), company, Rnd.Next(11, 99), GetRandomDomain());

            return email.Replace("..", ".");
        }

        public string GetRandomDate(int minYear, int maxYear)
        {
            int year = Rnd.Next(minYear, maxYear);

            return GetRandomDate(null, null, year);
        }

        public string GetRandomDate(int? desiredMonth, int? desiredDay, int? desiredYear)
        {
            if (!desiredMonth.HasValue)
                desiredMonth = Rnd.Next(1, 12);

            if (!desiredDay.HasValue)
                desiredDay = Rnd.Next(1, 28);

            if (!desiredYear.HasValue)
                desiredYear = Rnd.Next(1700, DateTime.Now.Year + 25);

            return new DateTime(desiredYear.Value, desiredMonth.Value, desiredDay.Value).ToShortDateString();
        }

        public string GetRandomValueFromArray(List<string> list)
        {
            return list[Rnd.Next(0, list.Count - 1)];
        }

        public string GetRandomPhoneNumber(string desiredAreaCode = null, string desiredPrefix = null, string desiredFourDigits = null)
        {
            if (desiredAreaCode.isNull())
                desiredAreaCode = GetRandomDigits(3);

            if (desiredPrefix.isNull())
                desiredPrefix = GetRandomDigits(3);

            if (desiredFourDigits.isNull())
                desiredFourDigits = GetRandomDigits(4);

            return string.Format("{0}-{1}-{2}", desiredAreaCode, desiredPrefix, desiredFourDigits);
        }

        public string GetRandomDigits(int numberOfDigets)
        {
            string results = "";

            for (int i = 0; i < numberOfDigets; i++)
            {
                results += Rnd.Next(1, 9).ToString();
            }

            return results;
        }

        public string GetRandomDigits(int lowDigits, int highDigets)
        {
            int numberOfDigets = Rnd.Next(lowDigits, highDigets);
            return GetRandomDigits(numberOfDigets);
        }

        public string GetRandomDomain()
        {
            string[] domains = new string[] { ".com", ".net", ".org", ".gov", ".tv" };
            return domains[Rnd.Next(0, domains.Length - 1)];
        }

        public string GetRandomDomainName()
        {
            return GetRandomCompanyName().Replace(" ", "");
        }

        public AdddressData GetRandomAddress()
        {
            var result = new AdddressData()
            {
                City = GetRandomCity(),
                Country = GetRandomCountry(),
                PostalCode = GetRandomPostalCode(),
                State = GetRandomState(),
                Street1 = string.Format("{0} {1} {2}", GetRandomDigits(2, 5), GetRandomLastName(), GetRandomStreetType()),
                Street2 = "# " + Rnd.Next(1, 3000)
            };

            return result;
        }

        private string GetRandomStreetType()
        {
            string result = GetRandomStringFromListOfStrings(new string[] { "Ave", "St", "Cir", "Plc", "Terrace", "Court", "Lane", "Dr" });
            result += " " + GetRandomStringFromListOfStrings(new string[] { "N", "S", "E", "W", "NE", "NW", "SE", "SW" });

            return result;
        }

        public string GetRandomStringFromListOfStrings(string[] sources)
        {
            int randomIndex = Rnd.Next(0, sources.Length - 1);

            return sources[randomIndex];
        }

        public CompanyData GetRandomCompany()
        {
            var result = new CompanyData()
            {
                BillingAddress = GetRandomAddress(),
                CompanyName = GetRandomCompanyName(),
                HQAddress = GetRandomAddress(),
                ShipToAddress = GetRandomAddress(),
            };

            result.WebSite = string.Format("http://www.{0}.{1}", result.CompanyName.Replace(" ", ""), GetRandomDomain());

            return result;
        }

        public PersonData GetRandomPerson()
        {
            int maxYear = DateTime.Now.Year - 10;
            int minYear = maxYear - 50;

            var person = new PersonData()
            {
                DateOfBirth = GetRandomDate(minYear, maxYear).ToDate(),
                DateOfDeath = GetRandomDate(maxYear + 2, maxYear + 8),
                FirstName = GetRandomFirstName(),
                LastName = GetRandomLastName(),
                Employer = GetRandomCompany(),
                EyeColor = GetRandomValueFromArray(new List<string>() { "Brown", "Blue", "Green" }),
                HairColor = GetRandomValueFromArray(new List<string>() { "Red", "Blond", "Brown", "Black", "Dyed" }),
                HeightFeet = Rnd.Next(3, 6),
                HeightInches = Rnd.Next(1, 11),
                HiredDate = GetRandomDate(maxYear + 2, maxYear + 8),
                HomeAddress = GetRandomAddress(),
                HomePhone = GetRandomPhoneNumber(null, "555", null),
                IsMarried = GetRandomBoolean(),
                MiddleName = GetRandomFirstName(),
                Password = "P@ssw0rd!",
                ShippingAddress = GetRandomAddress(),
                WorkPhone = GetRandomPhoneNumber(null, "555", null)
            };

            if (person.Age > 21)
            {
                person.IsMarried = GetRandomBoolean();
                if (person.IsMarried)
                {
                    person.MarriedDate = GetRandomDate(person.DateOfBirth.AddYears(19).Year, person.DateOfBirth.AddYears(21).Year).ToDate();
                }
            }

            person.UserName = string.Format("{0}.{1}{2}", person.FirstName, person.LastName, Rnd.Next(1, 222));
            person.EmailAddress = GetRandomEmailAddress(person.FirstName, person.LastName);

            return person;
        }
    }

    public static class ExtensionMethods
    {
        public static bool isNull(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNotNull(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static string PutInQuotes(this string str, bool condition = true)
        {
            if (condition)
            {
                return "\"" + str + "\"";
            }

            return str;
        }

        public static string PutInSingleQuotes(this string str, bool condition = true)
        {
            if (condition)
            {
                return "'" + str + "'";
            }

            return str;
        }
        public static bool IsNumeric(this string str)
        {
            bool result;
            result = double.TryParse(str, out _);

            if (!result)
            {
                result = long.TryParse(str, out _);
            }

            return result;
        }
        public static DateTime ToDate(this string str)
        {
            return DateTime.Parse(str);
        }

        public static DateTime AddYears(this DateTime dt, int years)
        {
            return dt.AddDays(years * 365);
        }

    }

    public class PersonData
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string HomePhone { get; set; }
        public string WorkPhone { get; set; }
        public string EmailAddress { get; set; }
        public string HiredDate { get; set; }
        public bool IsMarried { get; set; }
        public DateTime MarriedDate { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DateOfDeath { get; set; }
        public int Age
        {
            get
            {
                double days = (DateOfBirth - DateTime.Now).TotalDays;
                return (int)(days * (double)365.4);
            }
        }
        public int HeightFeet { get; set; }
        public int HeightInches { get; set; }
        public string EyeColor { get; set; }
        public string HairColor { get; set; }
        public bool IsMale { get; set; }
        public AdddressData HomeAddress { get; set; }
        public AdddressData ShippingAddress { get; set; }
        public CompanyData Employer { get; set; }
    }

    public class AdddressData
    {
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    public class CompanyData
    {
        public string CompanyName { get; set; }
        public string WebSite { get; set; }
        public AdddressData HQAddress { get; set; }
        public AdddressData BillingAddress { get; set; }
        public AdddressData ShipToAddress { get; set; }

    }

    public class Randomizer
    {
        private Random rnd;

        public Randomizer()
        {
            ResetRnd();
        }

        public int Next(int min, int max = int.MaxValue)
        {
            //rndUseageCounter += 1;
            //if (rndUseageCounter > 5)
            //{
            //    ResetRnd();
            //    rndUseageCounter = 0;
            //}

            return rnd.Next(min, max);
        }

        private void ResetRnd()
        {
            int seed = DateTime.Now.Millisecond * DateTime.Now.Second;
            rnd = new Random(seed);
        }
    }

    public static class ListRandomExtension {
        public static T Random<T>(this List<T> list)
        {
            return list[new Random().Next(0, list.Count)];
        } 
    }
}
