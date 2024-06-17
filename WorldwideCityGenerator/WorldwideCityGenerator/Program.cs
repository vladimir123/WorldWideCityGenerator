using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class City
{
    public string Name { get; set; }
    public string Country { get; set; }
}

public class GeoNamesResponse
{
    [JsonProperty("geonames")]
    public List<GeoNamesCity> Geonames { get; set; }
}

public class GeoNamesCity
{

    [JsonProperty("asciiName")]
    public string asciiName { get; set; }

    [JsonProperty("countryName")]
    public string Country { get; set; }
}

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string Username = "ksuhiyp";  // GeoNames username
    private const string Url = "http://api.geonames.org/searchJSON";
    private const string FilePath = "cities.csv";  // filepath

    static async Task Main(string[] args)
    {
        Console.Write("Country: ");
        string country = Console.ReadLine();
        Console.Write("Username: ");
        string username = Console.ReadLine();
        /*
            cities1000.zip           : all cities with a population > 1000 or seats of adm div down to PPLA3 (ca 130.000), see 'geoname' table for columns
            cities5000.zip           : all cities with a population > 5000 or PPLA (ca 50.000), see 'geoname' table for columns
            cities15000.zip          : all cities with a population > 15000 or capitals (ca 25.000), see 'geoname' table for columns
         */
        Console.Write("Choose city-filter (cities1000, cities5000, cities15000): ");
        string cityFilter = Console.ReadLine();

        List<GeoNamesCity> cities = await GetCitiesAsync(country, username, cityFilter);
        SaveCitiesToFile(cities, FilePath);
        Console.WriteLine($"data saved into {FilePath}");

        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
    }

    public static async Task<List<GeoNamesCity>> GetCitiesAsync(string country, string username, string cityFilter)
    {
        var cities = new List<GeoNamesCity>();
        int maxRows = 1000;
        int startRow = 0;

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "HttpClient");
        client.DefaultRequestHeaders.Add("Accept", "application/json; charset=UTF-8");

        while (true)
        {
            var requestUri = $"{Url}?username={(String.IsNullOrEmpty(username) ? Username : username)}&maxRows={maxRows}&startRow={startRow}&style=full&country={country}&cities={cityFilter}";
            var response = await client.GetAsync(requestUri);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Console.WriteLine("Access forbidden. Check your GeoNames username and request headers.");
                break;
            }

            try
            {
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GeoNamesResponse>(responseBody);

                if (data.Geonames == null || data.Geonames.Count == 0)
                {
                    break;
                }

                cities.AddRange(data.Geonames);
                startRow += maxRows;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                break;
            }

        }

        return cities;
    }

    public static void SaveCitiesToFile(List<GeoNamesCity> cities, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("City");

            foreach (var city in cities)
            {
                writer.WriteLine($"{city.asciiName}");
            }
        }
    }
}