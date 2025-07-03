

namespace FareSystem
{

    public class Station
    {
        public string Name { get; }
        public List<(Station station, double distance)> Connections { get; } = new();

        public Station(string name)
        {
            Name = name;
        }

        public void AddConnection(Station station, double distance)
        {
            Connections.Add((station, distance));
        }
}


public class Network
{
    private Dictionary<string, Station> stations = new();

    public void AddStation(Station station)
    {
        stations[station.Name] = station;
    }
    public List<string> GetAllStationNames()
    {
        // This method extracts all station names from the network
        // We need to add this because the original code doesn't expose the stations dictionary

        // Create a HashSet to store unique station names
        HashSet<string> stationNames = new HashSet<string>();

        try
        {
            // Try to access a known station to get its connections
            var sampleStation = GetStation("Jurong East (NS)");

            // Add the initial station
            stationNames.Add(sampleStation.Name);

            // Use a queue for breadth-first traversal of the network
            Queue<Station> queue = new Queue<Station>();
            queue.Enqueue(sampleStation);

            // Keep track of visited stations to avoid cycles
            HashSet<string> visited = new HashSet<string>();
            visited.Add(sampleStation.Name);

            // Traverse the network
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var (neighbor, _) in current.Connections)
                {
                    if (!visited.Contains(neighbor.Name))
                    {
                        visited.Add(neighbor.Name);
                        stationNames.Add(neighbor.Name);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building station list: {ex.Message}");
        }

        return stationNames.ToList();
    }

        public void AddConnection(string name1, string name2, double distance)
    {
        var station1 = stations[name1];
        var station2 = stations[name2];
        station1.AddConnection(station2, distance);
        station2.AddConnection(station1, distance);
    }

    public Station GetStation(string name) => stations[name];

    public (List<string> path, double distance) FindShortestPath(string start, string end)
    {
        var distances = stations.ToDictionary(s => s.Key, _ => double.PositiveInfinity);
        var previous = new Dictionary<string, string>();
        var visited = new HashSet<string>();
        var queue = new PriorityQueue<string, double>();

        distances[start] = 0;
        queue.Enqueue(start, 0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);

            if (current == end) break;

            foreach (var (neighbor, distance) in stations[current].Connections)
            {
                var newDist = distances[current] + distance;
                if (newDist < distances[neighbor.Name])
                {
                    distances[neighbor.Name] = newDist;
                    previous[neighbor.Name] = current;
                    queue.Enqueue(neighbor.Name, newDist);
                }
            }
        }

        var path = new List<string>();
        var crawl = end;
        while (previous.ContainsKey(crawl))
        {
            path.Insert(0, crawl);
            crawl = previous[crawl];
        }
        path.Insert(0, start);

        return (path, distances[end]);
    }
}

public static class FareCalculator
{
    public static double CalculateFare(double distance)
    {
        if (distance <= 3) return 0.95;
        if (distance <= 5) return 1.10;
        if (distance <= 10) return 1.29;
        if (distance <= 15) return 1.47;
        if (distance <= 20) return 1.66;
        if (distance <= 25) return 1.84;
        return 2.02;
    }
}

// Unit Tests
public static class Tests
{
    public static void RunAll(Network network)
    {
        TestSameStation(network);
        TestBackwardTravel(network);
        TestInterchangeRoute(network);
        TestCircleLoop(network);
        TestMultiLineRouting(network);
        TestInvalidStations(network);
        TestZeroDistanceTransfers(network);
    }

    private static void TestSameStation(Network network)
    {
        var (_, dist) = network.FindShortestPath("Bishan (NS)", "Bishan (NS)");
        Console.WriteLine("Same Station Test (0km): " + (dist == 0 ? "PASS" : "FAIL"));
    }

    private static void TestBackwardTravel(Network network)
    {
        var (_, dist1) = network.FindShortestPath("Jurong East (EW)", "Paya Lebar (EW)");
        var (_, dist2) = network.FindShortestPath("Paya Lebar (EW)", "Jurong East (EW)");
        Console.WriteLine("Backward Travel Test (symmetry): " + (Math.Abs(dist1 - dist2) < 0.01 ? "PASS" : "FAIL"));
    }

    private static void TestInterchangeRoute(Network network)
    {
        var (_, dist) = network.FindShortestPath("Paya Lebar (EW)", "Bishan (NS)");
        Console.WriteLine("Interchange Route Test (>0km): " + (dist > 0 ? "PASS" : "FAIL"));
    }

    private static void TestCircleLoop(Network network)
    {
        var (_, dist) = network.FindShortestPath("Mountbatten (CC)", "Kent Ridge (CC)");
        Console.WriteLine("Circle Line Loop Test (>10km): " + (dist > 10 ? "PASS" : "FAIL"));
    }

    private static void TestMultiLineRouting(Network network)
    {
        var (_, dist) = network.FindShortestPath("HarbourFront (NE)", "Botanic Gardens (DT)");
        Console.WriteLine("Multi-Line Routing Test (6–10km): " + ((dist >= 6 && dist <= 10) ? "PASS" : "FAIL"));
    }


    private static void TestInvalidStations(Network network)
    {
        try
        {
            network.FindShortestPath("XXX", "Dhoby Ghaut (NS)");
            Console.WriteLine("Invalid Station Test A: FAIL");
        }
        catch { Console.WriteLine("Invalid Station Test A: PASS"); }

        try
        {
            network.FindShortestPath("Dhoby Ghaut (NE)", "YYY");
            Console.WriteLine("Invalid Station Test B: FAIL");
        }
        catch { Console.WriteLine("Invalid Station Test B: PASS"); }
    }

    private static void TestZeroDistanceTransfers(Network network)
    {
        var (_, dist) = network.FindShortestPath("Promenade (CC)", "Promenade (DT)");
        Console.WriteLine("Zero-Distance Transfer Test: " + (dist == 0 ? "PASS" : "FAIL"));
    }

}


// Example Setup (partial data shown)
public static class MRTInitializer
{
    public static Network Init()
    {
        var network = new Network();
        var JurongEast_NS = new Station("Jurong East (NS)");
        var BukitBatok = new Station("Bukit Batok (NS)");
        var BukitGombak = new Station("Bukit Gombak (NS)");
        var ChoaChuKang = new Station("Choa Chu Kang (NS)");
        var YewTee = new Station("Yew Tee (NS)");
        var Kranji = new Station("Kranji (NS)");
        var Marsiling = new Station("Marsiling (NS)");
        var Woodlands = new Station("Woodlands (NS)");
        var Admiralty = new Station("Admiralty (NS)");
        var Sembawang = new Station("Sembawang (NS)");
        var Canberra = new Station("Canberra (NS)");
        var Yishun = new Station("Yishun (NS)");
        var Khatib = new Station("Khatib (NS)");
        var YioChuKang = new Station("Yio Chu Kang (NS)");
        var AngMoKio = new Station("Ang Mo Kio (NS)");
        var Bishan_NS = new Station("Bishan (NS)");
        var Braddell = new Station("Braddell (NS)");
        var ToaPayoh = new Station("Toa Payoh (NS)");
        var Novena = new Station("Novena (NS)");
        var Newton_NS = new Station("Newton (NS)");
        var Orchard = new Station("Orchard (NS)");
        var Somerset = new Station("Somerset (NS)");
        var DhobyGhaut_NS = new Station("Dhoby Ghaut (NS)");
        var CityHall_NS = new Station("City Hall (NS)");
        var RafflesPlace_NS = new Station("Raffles Place (NS)");
        var MarinaBay_NS = new Station("Marina Bay (NS)");
        var MarinaSouthPier = new Station("Marina South Pier (NS)");
        var TuasLink = new Station("Tuas Link (EW)");
        var TuasWestRoad = new Station("Tuas West Road (EW)");
        var TuasCrescent = new Station("Tuas Crescent (EW)");
        var GulCircle = new Station("Gul Circle (EW)");
        var JooKoon = new Station("Joo Koon (EW)");
        var Pioneer = new Station("Pioneer (EW)");
        var BoonLay = new Station("Boon Lay (EW)");
        var Lakeside = new Station("Lakeside (EW)");
        var ChineseGarden = new Station("Chinese Garden (EW)");
        var JurongEast_EW = new Station("Jurong East (EW)");
        var Clementi = new Station("Clementi (EW)");
        var Dover = new Station("Dover (EW)");
        var BuonaVista_EW = new Station("Buona Vista (EW)");
        var Commonwealth = new Station("Commonwealth (EW)");
        var Queenstown = new Station("Queenstown (EW)");
        var Redhill = new Station("Redhill (EW)");
        var TiongBahru = new Station("Tiong Bahru (EW)");
        var OutramPark_EW = new Station("Outram Park (EW)");
        var TanjongPagar = new Station("Tanjong Pagar (EW)");
        var RafflesPlace_EW = new Station("Raffles Place (EW)");
        var CityHall_EW = new Station("City Hall (EW)");
        var Bugis_EW = new Station("Bugis (EW)");
        var Lavender = new Station("Lavender (EW)");
        var Kallang = new Station("Kallang (EW)");
        var Aljunied = new Station("Aljunied (EW)");
        var PayaLebar_EW = new Station("Paya Lebar (EW)");
        var Eunos = new Station("Eunos (EW)");
        var Kembangan = new Station("Kembangan (EW)");
        var Bedok = new Station("Bedok (EW)");
        var TanahMerah_EW = new Station("Tanah Merah (EW)");
        var Simei = new Station("Simei (EW)");
        var Tampines_EW = new Station("Tampines (EW)");
        var PasirRis = new Station("Pasir Ris (EW)");
        var Expo = new Station("Expo (CG)");
        var ChangiAirport = new Station("Changi Airport (CG)");
        var DhobyGhaut_CC = new Station("Dhoby Ghaut (CC)");
        var BrasBasah_CC = new Station("Bras Basah (CC)");
        var Esplanade_CC = new Station("Esplanade (CC)");
        var Promenade_CC = new Station("Promenade (CC)");
        var NicollHighway_CC = new Station("Nicoll Highway (CC)");
        var Stadium_CC = new Station("Stadium (CC)");
        var Mountbatten_CC = new Station("Mountbatten (CC)");
        var Dakota_CC = new Station("Dakota (CC)");
        var PayaLebar_CC = new Station("Paya Lebar (CC)");
        var MacPherson_CC = new Station("MacPherson (CC)");
        var TaiSeng_CC = new Station("Tai Seng (CC)");
        var Bartley_CC = new Station("Bartley (CC)");
        var Serangoon_CC = new Station("Serangoon (CC)");
        var LorongChuan_CC = new Station("Lorong Chuan (CC)");
        var Bishan_CC = new Station("Bishan (CC)");
        var Marymount_CC = new Station("Marymount (CC)");
        var Caldecott_CC = new Station("Caldecott (CC)");
        var BotanicGardens_CC = new Station("Botanic Gardens (CC)");
        var FarrerRoad_CC = new Station("Farrer Road (CC)");
        var HollandVillage_CC = new Station("Holland Village (CC)");
        var BuonaVista_CC = new Station("Buona Vista (CC)");
        var OneNorth_CC = new Station("one-north (CC)");
        var KentRidge_CC = new Station("Kent Ridge (CC)");
        var HawParVilla_CC = new Station("Haw Par Villa (CC)");
        var PasirPanjang_CC = new Station("Pasir Panjang (CC)");
        var LabradorPark_CC = new Station("Labrador Park (CC)");
        var TelokBlangah_CC = new Station("Telok Blangah (CC)");
        var HarbourFront_CC = new Station("HarbourFront (CC)");
        var HarbourFront_NE = new Station("HarbourFront (NE)");
        var OutramPark_NE = new Station("Outram Park (NE)");
        var Chinatown_NE = new Station("Chinatown (NE)");
        var ClarkeQuay_NE = new Station("Clarke Quay (NE)");
        var DhobyGhaut_NE = new Station("Dhoby Ghaut (NE)");
        var LittleIndia_NE = new Station("Little India (NE)");
        var FarrerPark_NE = new Station("Farrer Park (NE)");
        var BoonKeng_NE = new Station("Boon Keng (NE)");
        var PotongPasir_NE = new Station("Potong Pasir (NE)");
        var Woodleigh_NE = new Station("Woodleigh (NE)");
        var Serangoon_NE = new Station("Serangoon (NE)");
        var Kovan_NE = new Station("Kovan (NE)");
        var Hougang_NE = new Station("Hougang (NE)");
        var Buangkok_NE = new Station("Buangkok (NE)");
        var Sengkang_NE = new Station("Sengkang (NE)");
        var Punggol_NE = new Station("Punggol (NE)");
        var BukitPanjang_DT = new Station("Bukit Panjang (DT)");
        var Cashew_DT = new Station("Cashew (DT)");
        var Hillview_DT = new Station("Hillview (DT)");
        var Hume_DT = new Station("Hume (DT)");
        var BeautyWorld_DT = new Station("Beauty World (DT)");
        var KingAlbertPark_DT = new Station("King Albert Park (DT)");
        var SixthAvenue_DT = new Station("Sixth Avenue (DT)");
        var TanKahKee_DT = new Station("Tan Kah Kee (DT)");
        var BotanicGardens_DT = new Station("Botanic Gardens (DT)");
        var Stevens_DT = new Station("Stevens (DT)");
        var Newton_DT = new Station("Newton (DT)");
        var LittleIndia_DT = new Station("Little India (DT)");
        var Rochor_DT = new Station("Rochor (DT)");
        var Bugis_DT = new Station("Bugis (DT)");
        var Promenade_DT = new Station("Promenade (DT)");
        var Bayfront_DT = new Station("Bayfront (DT)");
        var Downtown_DT = new Station("Downtown (DT)");
        var TelokAyer_DT = new Station("Telok Ayer (DT)");
        var Chinatown_DT = new Station("Chinatown (DT)");
        var FortCanning_DT = new Station("Fort Canning (DT)");
        var Bencoolen_DT = new Station("Bencoolen (DT)");
        var JalanBesar_DT = new Station("Jalan Besar (DT)");
        var Bendemeer_DT = new Station("Bendemeer (DT)");
        var GeylangBahru_DT = new Station("Geylang Bahru (DT)");
        var Mattar_DT = new Station("Mattar (DT)");
        var MacPherson_DT = new Station("MacPherson (DT)");
        var Ubi_DT = new Station("Ubi (DT)");
        var KakiBukit_DT = new Station("Kaki Bukit (DT)");
        var BedokNorth_DT = new Station("Bedok North (DT)");
        var BedokReservoir_DT = new Station("Bedok Reservoir (DT)");
        var TampinesWest_DT = new Station("Tampines West (DT)");
        var Tampines_DT = new Station("Tampines (DT)");
        var TampinesEast_DT = new Station("Tampines East (DT)");
        var UpperChangi_DT = new Station("Upper Changi (DT)");
        var Expo_DT = new Station("Expo (DT)");
        var WoodlandsNorth_TE = new Station("Woodlands North (TE)");
        var Woodlands_TE = new Station("Woodlands (TE)");
        var WoodlandsSouth_TE = new Station("Woodlands South (TE)");
        var Springleaf_TE = new Station("Springleaf (TE)");
        var Lentor_TE = new Station("Lentor (TE)");
        var Mayflower_TE = new Station("Mayflower (TE)");
        var BrightHill_TE = new Station("Bright Hill (TE)");
        var UpperThomson_TE = new Station("Upper Thomson (TE)");
        var Caldecott_TE = new Station("Caldecott (TE)");
        var MountPleasant_TE = new Station("Mount Pleasant (TE)");
        var Stevens_TE = new Station("Stevens (TE)");
        var Napier_TE = new Station("Napier (TE)");
        var Orchard_Boulevard_TE = new Station("Orchard Boulevard (TE)");
        var Orchard_TE = new Station("Orchard (TE)");
        var GreatWorld_TE = new Station("Great World (TE)");
        var Havelock_TE = new Station("Havelock (TE)");
        var OutramPark_TE = new Station("Outram Park (TE)");
        var Maxwell_TE = new Station("Maxwell (TE)");
        var ShentonWay_TE = new Station("Shenton Way (TE)");
        var MarinaBay_TE = new Station("Marina Bay (TE)");
        network.AddStation(JurongEast_NS);
        network.AddStation(BukitBatok);
        network.AddStation(BukitGombak);
        network.AddStation(ChoaChuKang);
        network.AddStation(YewTee);
        network.AddStation(Kranji);
        network.AddStation(Marsiling);
        network.AddStation(Woodlands);
        network.AddStation(Admiralty);
        network.AddStation(Sembawang);
        network.AddStation(Canberra);
        network.AddStation(Yishun);
        network.AddStation(Khatib);
        network.AddStation(YioChuKang);
        network.AddStation(AngMoKio);
        network.AddStation(Bishan_NS);
        network.AddStation(Braddell);
        network.AddStation(ToaPayoh);
        network.AddStation(Novena);
        network.AddStation(Newton_NS);
        network.AddStation(Orchard);
        network.AddStation(Somerset);
        network.AddStation(DhobyGhaut_NS);
        network.AddStation(CityHall_NS);
        network.AddStation(RafflesPlace_NS);
        network.AddStation(MarinaBay_NS);
        network.AddStation(MarinaSouthPier);
        network.AddStation(TuasLink);
        network.AddStation(TuasWestRoad);
        network.AddStation(TuasCrescent);
        network.AddStation(GulCircle);
        network.AddStation(JooKoon);
        network.AddStation(Pioneer);
        network.AddStation(BoonLay);
        network.AddStation(Lakeside);
        network.AddStation(ChineseGarden);
        network.AddStation(JurongEast_EW);
        network.AddStation(Clementi);
        network.AddStation(Dover);
        network.AddStation(BuonaVista_EW);
        network.AddStation(Commonwealth);
        network.AddStation(Queenstown);
        network.AddStation(Redhill);
        network.AddStation(TiongBahru);
        network.AddStation(OutramPark_EW);
        network.AddStation(TanjongPagar);
        network.AddStation(RafflesPlace_EW);
        network.AddStation(CityHall_EW);
        network.AddStation(Bugis_EW);
        network.AddStation(Lavender);
        network.AddStation(Kallang);
        network.AddStation(Aljunied);
        network.AddStation(PayaLebar_EW);
        network.AddStation(Eunos);
        network.AddStation(Kembangan);
        network.AddStation(Bedok);
        network.AddStation(TanahMerah_EW);
        network.AddStation(Simei);
        network.AddStation(Tampines_EW);
        network.AddStation(PasirRis);
        network.AddStation(Expo);
        network.AddStation(ChangiAirport);
        network.AddStation(DhobyGhaut_CC);
        network.AddStation(BrasBasah_CC);
        network.AddStation(Esplanade_CC);
        network.AddStation(Promenade_CC);
        network.AddStation(NicollHighway_CC);
        network.AddStation(Stadium_CC);
        network.AddStation(Mountbatten_CC);
        network.AddStation(Dakota_CC);
        network.AddStation(PayaLebar_CC);
        network.AddStation(MacPherson_CC);
        network.AddStation(TaiSeng_CC);
        network.AddStation(Bartley_CC);
        network.AddStation(Serangoon_CC);
        network.AddStation(LorongChuan_CC);
        network.AddStation(Bishan_CC);
        network.AddStation(Marymount_CC);
        network.AddStation(Caldecott_CC);
        network.AddStation(BotanicGardens_CC);
        network.AddStation(FarrerRoad_CC);
        network.AddStation(HollandVillage_CC);
        network.AddStation(BuonaVista_CC);
        network.AddStation(OneNorth_CC);
        network.AddStation(KentRidge_CC);
        network.AddStation(HawParVilla_CC);
        network.AddStation(PasirPanjang_CC);
        network.AddStation(LabradorPark_CC);
        network.AddStation(TelokBlangah_CC);
        network.AddStation(HarbourFront_CC);
        network.AddStation(HarbourFront_NE);
        network.AddStation(OutramPark_NE);
        network.AddStation(Chinatown_NE);
        network.AddStation(ClarkeQuay_NE);
        network.AddStation(DhobyGhaut_NE);
        network.AddStation(LittleIndia_NE);
        network.AddStation(FarrerPark_NE);
        network.AddStation(BoonKeng_NE);
        network.AddStation(PotongPasir_NE);
        network.AddStation(Woodleigh_NE);
        network.AddStation(Serangoon_NE);
        network.AddStation(Kovan_NE);
        network.AddStation(Hougang_NE);
        network.AddStation(Buangkok_NE);
        network.AddStation(Sengkang_NE);
        network.AddStation(Punggol_NE);
        network.AddStation(BukitPanjang_DT);
        network.AddStation(Cashew_DT);
        network.AddStation(Hillview_DT);
        network.AddStation(Hume_DT);
        network.AddStation(BeautyWorld_DT);
        network.AddStation(KingAlbertPark_DT);
        network.AddStation(SixthAvenue_DT);
        network.AddStation(TanKahKee_DT);
        network.AddStation(BotanicGardens_DT);
        network.AddStation(Stevens_DT);
        network.AddStation(Newton_DT);
        network.AddStation(LittleIndia_DT);
        network.AddStation(Rochor_DT);
        network.AddStation(Bugis_DT);
        network.AddStation(Promenade_DT);
        network.AddStation(Bayfront_DT);
        network.AddStation(Downtown_DT);
        network.AddStation(TelokAyer_DT);
        network.AddStation(Chinatown_DT);
        network.AddStation(FortCanning_DT);
        network.AddStation(Bencoolen_DT);
        network.AddStation(JalanBesar_DT);
        network.AddStation(Bendemeer_DT);
        network.AddStation(GeylangBahru_DT);
        network.AddStation(Mattar_DT);
        network.AddStation(MacPherson_DT);
        network.AddStation(Ubi_DT);
        network.AddStation(KakiBukit_DT);
        network.AddStation(BedokNorth_DT);
        network.AddStation(BedokReservoir_DT);
        network.AddStation(TampinesWest_DT);
        network.AddStation(Tampines_DT);
        network.AddStation(TampinesEast_DT);
        network.AddStation(UpperChangi_DT);
        network.AddStation(Expo_DT);
        network.AddStation(WoodlandsNorth_TE);
        network.AddStation(Woodlands_TE);
        network.AddStation(WoodlandsSouth_TE);
        network.AddStation(Springleaf_TE);
        network.AddStation(Lentor_TE);
        network.AddStation(Mayflower_TE);
        network.AddStation(BrightHill_TE);
        network.AddStation(UpperThomson_TE);
        network.AddStation(Caldecott_TE);
        network.AddStation(MountPleasant_TE);
        network.AddStation(Stevens_TE);
        network.AddStation(Napier_TE);
        network.AddStation(Orchard_Boulevard_TE);
        network.AddStation(Orchard_TE);
        network.AddStation(GreatWorld_TE);
        network.AddStation(Havelock_TE);
        network.AddStation(OutramPark_TE);
        network.AddStation(Maxwell_TE);
        network.AddStation(ShentonWay_TE);
        network.AddStation(MarinaBay_TE);
        network.AddConnection("Jurong East (NS)", "Bukit Batok (NS)", 1.8);
        network.AddConnection("Bukit Batok (NS)", "Bukit Gombak (NS)", 1.2);
        network.AddConnection("Bukit Gombak (NS)", "Choa Chu Kang (NS)", 1.5);
        network.AddConnection("Choa Chu Kang (NS)", "Yew Tee (NS)", 1.4);
        network.AddConnection("Yew Tee (NS)", "Kranji (NS)", 1.8);
        network.AddConnection("Kranji (NS)", "Marsiling (NS)", 1.2);
        network.AddConnection("Marsiling (NS)", "Woodlands (NS)", 1.0);
        network.AddConnection("Woodlands (NS)", "Admiralty (NS)", 1.3);
        network.AddConnection("Admiralty (NS)", "Sembawang (NS)", 1.6);
        network.AddConnection("Sembawang (NS)", "Canberra (NS)", 1.2);
        network.AddConnection("Canberra (NS)", "Yishun (NS)", 1.5);
        network.AddConnection("Yishun (NS)", "Khatib (NS)", 1.3);
        network.AddConnection("Khatib (NS)", "Yio Chu Kang (NS)", 2.4);
        network.AddConnection("Yio Chu Kang (NS)", "Ang Mo Kio (NS)", 1.4);
        network.AddConnection("Ang Mo Kio (NS)", "Bishan (NS)", 1.5);
        network.AddConnection("Bishan (NS)", "Braddell (NS)", 1.2);
        network.AddConnection("Braddell (NS)", "Toa Payoh (NS)", 1.0);
        network.AddConnection("Toa Payoh (NS)", "Novena (NS)", 1.2);
        network.AddConnection("Novena (NS)", "Newton (NS)", 1.0);
        network.AddConnection("Newton (NS)", "Orchard (NS)", 1.0);
        network.AddConnection("Orchard (NS)", "Somerset (NS)", 0.9);
        network.AddConnection("Somerset (NS)", "Dhoby Ghaut (NS)", 0.6);
        network.AddConnection("Dhoby Ghaut (NS)", "City Hall (NS)", 1.0);
        network.AddConnection("City Hall (NS)", "Raffles Place (NS)", 0.6);
        network.AddConnection("Raffles Place (NS)", "Marina Bay (NS)", 1.2);
        network.AddConnection("Marina Bay (NS)", "Marina South Pier (NS)", 1.0);
        network.AddConnection("Tuas Link (EW)", "Tuas West Road (EW)", 1.6);
        network.AddConnection("Tuas West Road (EW)", "Tuas Crescent (EW)", 1.7);
        network.AddConnection("Tuas Crescent (EW)", "Gul Circle (EW)", 1.8);
        network.AddConnection("Gul Circle (EW)", "Joo Koon (EW)", 2.3);
        network.AddConnection("Joo Koon (EW)", "Pioneer (EW)", 2.5);
        network.AddConnection("Pioneer (EW)", "Boon Lay (EW)", 1.6);
        network.AddConnection("Boon Lay (EW)", "Lakeside (EW)", 1.8);
        network.AddConnection("Lakeside (EW)", "Chinese Garden (EW)", 1.5);
        network.AddConnection("Chinese Garden (EW)", "Jurong East (EW)", 1.6);
        network.AddConnection("Jurong East (EW)", "Clementi (EW)", 1.9);
        network.AddConnection("Clementi (EW)", "Dover (EW)", 1.4);
        network.AddConnection("Dover (EW)", "Buona Vista (EW)", 1.4);
        network.AddConnection("Buona Vista (EW)", "Commonwealth (EW)", 1.2);
        network.AddConnection("Commonwealth (EW)", "Queenstown (EW)", 1.2);
        network.AddConnection("Queenstown (EW)", "Redhill (EW)", 1.3);
        network.AddConnection("Redhill (EW)", "Tiong Bahru (EW)", 1.1);
        network.AddConnection("Tiong Bahru (EW)", "Outram Park (EW)", 1.2);
        network.AddConnection("Outram Park (EW)", "Tanjong Pagar (EW)", 1.0);
        network.AddConnection("Tanjong Pagar (EW)", "Raffles Place (EW)", 1.0);
        network.AddConnection("Raffles Place (EW)", "City Hall (EW)", 0.9);
        network.AddConnection("City Hall (EW)", "Bugis (EW)", 1.2);
        network.AddConnection("Bugis (EW)", "Lavender (EW)", 1.0);
        network.AddConnection("Lavender (EW)", "Kallang (EW)", 1.2);
        network.AddConnection("Kallang (EW)", "Aljunied (EW)", 1.3);
        network.AddConnection("Aljunied (EW)", "Paya Lebar (EW)", 1.3);
        network.AddConnection("Paya Lebar (EW)", "Eunos (EW)", 1.2);
        network.AddConnection("Eunos (EW)", "Kembangan (EW)", 1.2);
        network.AddConnection("Kembangan (EW)", "Bedok (EW)", 1.3);
        network.AddConnection("Bedok (EW)", "Tanah Merah (EW)", 1.9);
        network.AddConnection("Tanah Merah (EW)", "Simei (EW)", 1.6);
        network.AddConnection("Simei (EW)", "Tampines (EW)", 1.4);
        network.AddConnection("Tampines (EW)", "Pasir Ris (EW)", 1.8);
        network.AddConnection("Tanah Merah (EW)", "Expo (CG)", 3.0);
        network.AddConnection("Expo (CG)", "Changi Airport (CG)", 1.9);
        network.AddConnection("Jurong East (NS)", "Jurong East (EW)", 0);
        network.AddConnection("Raffles Place (NS)", "Raffles Place (EW)", 0);
        network.AddConnection("City Hall (NS)", "City Hall (EW)", 0);
        network.AddConnection("Dhoby Ghaut (NS)", "Dhoby Ghaut (CC)", 0);
        network.AddConnection("Paya Lebar (EW)", "Paya Lebar (CC)", 0);
        network.AddConnection("Bishan (NS)", "Bishan (CC)", 0);
        network.AddConnection("Buona Vista (EW)", "Buona Vista (CC)", 0);
        network.AddConnection("Dhoby Ghaut (CC)", "Bras Basah (CC)", 0.8);
        network.AddConnection("Bras Basah (CC)", "Esplanade (CC)", 0.7);
        network.AddConnection("Esplanade (CC)", "Promenade (CC)", 0.9);
        network.AddConnection("Promenade (CC)", "Nicoll Highway (CC)", 1.2);
        network.AddConnection("Nicoll Highway (CC)", "Stadium (CC)", 1.0);
        network.AddConnection("Stadium (CC)", "Mountbatten (CC)", 1.1);
        network.AddConnection("Mountbatten (CC)", "Dakota (CC)", 1.3);
        network.AddConnection("Dakota (CC)", "Paya Lebar (CC)", 1.5);
        network.AddConnection("Paya Lebar (CC)", "MacPherson (CC)", 1.0);
        network.AddConnection("MacPherson (CC)", "Tai Seng (CC)", 1.2);
        network.AddConnection("Tai Seng (CC)", "Bartley (CC)", 1.3);
        network.AddConnection("Bartley (CC)", "Serangoon (CC)", 1.4);
        network.AddConnection("Serangoon (CC)", "Lorong Chuan (CC)", 1.1);
        network.AddConnection("Lorong Chuan (CC)", "Bishan (CC)", 1.6);
        network.AddConnection("Bishan (CC)", "Marymount (CC)", 1.7);
        network.AddConnection("Marymount (CC)", "Caldecott (CC)", 1.8);
        network.AddConnection("Caldecott (CC)", "Botanic Gardens (CC)", 2.0);
        network.AddConnection("Botanic Gardens (CC)", "Farrer Road (CC)", 1.2);
        network.AddConnection("Farrer Road (CC)", "Holland Village (CC)", 1.3);
        network.AddConnection("Holland Village (CC)", "Buona Vista (CC)", 0.9);
        network.AddConnection("Buona Vista (CC)", "one-north (CC)", 1.1);
        network.AddConnection("one-north (CC)", "Kent Ridge (CC)", 1.0);
        network.AddConnection("Kent Ridge (CC)", "Haw Par Villa (CC)", 1.5);
        network.AddConnection("Haw Par Villa (CC)", "Pasir Panjang (CC)", 1.4);
        network.AddConnection("Pasir Panjang (CC)", "Labrador Park (CC)", 1.3);
        network.AddConnection("Labrador Park (CC)", "Telok Blangah (CC)", 1.2);
        network.AddConnection("Telok Blangah (CC)", "HarbourFront (CC)", 1.0);
        network.AddConnection("HarbourFront (CC)", "HarbourFront (NE)", 0);
        network.AddConnection("Outram Park (EW)", "Outram Park (NE)", 0);
        network.AddConnection("Dhoby Ghaut (CC)", "Dhoby Ghaut (NE)", 0);
        network.AddConnection("Dhoby Ghaut (NS)", "Dhoby Ghaut (NE)", 0);
        network.AddConnection("Serangoon (CC)", "Serangoon (NE)", 0);
        network.AddConnection("HarbourFront (NE)", "Outram Park (NE)", 1.3);
        network.AddConnection("Outram Park (NE)", "Chinatown (NE)", 0.8);
        network.AddConnection("Chinatown (NE)", "Clarke Quay (NE)", 1.0);
        network.AddConnection("Clarke Quay (NE)", "Dhoby Ghaut (NE)", 0.9);
        network.AddConnection("Dhoby Ghaut (NE)", "Little India (NE)", 1.5);
        network.AddConnection("Little India (NE)", "Farrer Park (NE)", 1.1);
        network.AddConnection("Farrer Park (NE)", "Boon Keng (NE)", 1.2);
        network.AddConnection("Boon Keng (NE)", "Potong Pasir (NE)", 1.3);
        network.AddConnection("Potong Pasir (NE)", "Woodleigh (NE)", 1.4);
        network.AddConnection("Woodleigh (NE)", "Serangoon (NE)", 1.0);
        network.AddConnection("Serangoon (NE)", "Kovan (NE)", 1.6);
        network.AddConnection("Kovan (NE)", "Hougang (NE)", 1.2);
        network.AddConnection("Hougang (NE)", "Buangkok (NE)", 1.5);
        network.AddConnection("Buangkok (NE)", "Sengkang (NE)", 1.8);
        network.AddConnection("Sengkang (NE)", "Punggol (NE)", 2.0);
        network.AddConnection("Botanic Gardens (CC)", "Botanic Gardens (DT)", 0);
        network.AddConnection("Newton (NS)", "Newton (DT)", 0);
        network.AddConnection("Little India (NE)", "Little India (DT)", 0);
        network.AddConnection("Bugis (EW)", "Bugis (DT)", 0);
        network.AddConnection("Promenade (CC)", "Promenade (DT)", 0);
        network.AddConnection("Chinatown (NE)", "Chinatown (DT)", 0);
        network.AddConnection("MacPherson (CC)", "MacPherson (DT)", 0);
        network.AddConnection("Tampines (EW)", "Tampines (DT)", 0);
        network.AddConnection("Expo (CG)", "Expo (DT)", 0);
        network.AddConnection("Bukit Panjang (DT)", "Cashew (DT)", 1.2);
        network.AddConnection("Cashew (DT)", "Hillview (DT)", 1.5);
        network.AddConnection("Hillview (DT)", "Hume (DT)", 1.0);
        network.AddConnection("Hume (DT)", "Beauty World (DT)", 0.9);
        network.AddConnection("Beauty World (DT)", "King Albert Park (DT)", 1.1);
        network.AddConnection("King Albert Park (DT)", "Sixth Avenue (DT)", 1.3);
        network.AddConnection("Sixth Avenue (DT)", "Tan Kah Kee (DT)", 1.4);
        network.AddConnection("Tan Kah Kee (DT)", "Botanic Gardens (DT)", 0.8);
        network.AddConnection("Botanic Gardens (DT)", "Stevens (DT)", 1.6);
        network.AddConnection("Stevens (DT)", "Newton (DT)", 1.2);
        network.AddConnection("Newton (DT)", "Little India (DT)", 1.3);
        network.AddConnection("Little India (DT)", "Rochor (DT)", 1.0);
        network.AddConnection("Rochor (DT)", "Bugis (DT)", 0.7);
        network.AddConnection("Bugis (DT)", "Promenade (DT)", 1.1);
        network.AddConnection("Promenade (DT)", "Bayfront (DT)", 0.9);
        network.AddConnection("Bayfront (DT)", "Downtown (DT)", 0.8);
        network.AddConnection("Downtown (DT)", "Telok Ayer (DT)", 0.7);
        network.AddConnection("Telok Ayer (DT)", "Chinatown (DT)", 0.9);
        network.AddConnection("Chinatown (DT)", "Fort Canning (DT)", 1.0);
        network.AddConnection("Fort Canning (DT)", "Bencoolen (DT)", 0.8);
        network.AddConnection("Bencoolen (DT)", "Jalan Besar (DT)", 1.2);
        network.AddConnection("Jalan Besar (DT)", "Bendemeer (DT)", 1.1);
        network.AddConnection("Bendemeer (DT)", "Geylang Bahru (DT)", 1.3);
        network.AddConnection("Geylang Bahru (DT)", "Mattar (DT)", 1.0);
        network.AddConnection("Mattar (DT)", "MacPherson (DT)", 0.9);
        network.AddConnection("MacPherson (DT)", "Ubi (DT)", 1.2);
        network.AddConnection("Ubi (DT)", "Kaki Bukit (DT)", 1.1);
        network.AddConnection("Kaki Bukit (DT)", "Bedok North (DT)", 1.4);
        network.AddConnection("Bedok North (DT)", "Bedok Reservoir (DT)", 1.3);
        network.AddConnection("Bedok Reservoir (DT)", "Tampines West (DT)", 1.2);
        network.AddConnection("Tampines West (DT)", "Tampines (DT)", 1.0);
        network.AddConnection("Tampines (DT)", "Tampines East (DT)", 1.3);
        network.AddConnection("Tampines East (DT)", "Upper Changi (DT)", 1.5);
        network.AddConnection("Upper Changi (DT)", "Expo (DT)", 1.7);
        network.AddConnection("Woodlands (NS)", "Woodlands (TE)", 0);
        network.AddConnection("Caldecott (CC)", "Caldecott (TE)", 0);
        network.AddConnection("Stevens (DT)", "Stevens (TE)", 0);
        network.AddConnection("Orchard (NS)", "Orchard (TE)", 0);
        network.AddConnection("Outram Park (EW)", "Outram Park (TE)", 0);
        network.AddConnection("Outram Park (NE)", "Outram Park (TE)", 0);
        network.AddConnection("Marina Bay (NS)", "Marina Bay (TE)", 0);
        network.AddConnection("Woodlands North (TE)", "Woodlands (TE)", 1.3);
        network.AddConnection("Woodlands (TE)", "Woodlands South (TE)", 1.1);
        network.AddConnection("Woodlands South (TE)", "Springleaf (TE)", 1.6);
        network.AddConnection("Springleaf (TE)", "Lentor (TE)", 1.4);
        network.AddConnection("Lentor (TE)", "Mayflower (TE)", 1.2);
        network.AddConnection("Mayflower (TE)", "Bright Hill (TE)", 1.5);
        network.AddConnection("Bright Hill (TE)", "Upper Thomson (TE)", 1.3);
        network.AddConnection("Upper Thomson (TE)", "Caldecott (TE)", 1.7);
        network.AddConnection("Caldecott (TE)", "Mount Pleasant (TE)", 1.4);
        network.AddConnection("Mount Pleasant (TE)", "Stevens (TE)", 1.1);
        network.AddConnection("Stevens (TE)", "Napier (TE)", 1.3);
        network.AddConnection("Napier (TE)", "Orchard Boulevard (TE)", 0.9);
        network.AddConnection("Orchard Boulevard (TE)", "Orchard (TE)", 0.8);
        network.AddConnection("Orchard (TE)", "Great World (TE)", 1.2);
        network.AddConnection("Great World (TE)", "Havelock (TE)", 1.0);
        network.AddConnection("Havelock (TE)", "Outram Park (TE)", 1.4);
        network.AddConnection("Outram Park (TE)", "Maxwell (TE)", 1.1);
        network.AddConnection("Maxwell (TE)", "Shenton Way (TE)", 0.9);
        network.AddConnection("Shenton Way (TE)", "Marina Bay (TE)", 1.3);
        network.AddConnection("Bayfront (DT)", "Promenade (CC)", 0);
        network.AddConnection("MacPherson (CC)", "MacPherson (DT)", 0); 
        network.AddConnection("Paya Lebar (EW)", "Paya Lebar (CC)", 0);

        return network;
    }
}

    public class FareSystem
    {
        public static void Initializer()
        {
            var network = MRTInitializer.Init();
        }

        public static double Calculate(string Entry, string Exit, Network network)
        {
            {
                var (_, dist) = network.FindShortestPath(Entry, Exit);
                double fare = FareCalculator.CalculateFare(dist);
                return fare;
            }
        }
    }

}
