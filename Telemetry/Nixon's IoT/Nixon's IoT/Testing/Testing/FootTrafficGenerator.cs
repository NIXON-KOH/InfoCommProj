
using Bogus;
using FareSystem;
using Blockchain;
using System.Text.Json;


namespace FootTraffic
{
    public class FootTrafficSimulator
    {
        // --- Random Number Generator ---
        private Random _random = new Random();

        // --- Parameters ---
        // Adjust these values to match your desired traffic patterns

        // Base traffic level (constant minimum traffic)
        private double baseTraffic = 200;

        // Morning Peak Parameters (Amplitude, Mean Time, Standard Deviation)
        private double aMorning = 30;
        private double muMorning = 8.0;  // 8:00 AM
        private double sigmaMorning = 1.0;

        // Lunch Peak Parameters
        private double aLunch = 20;
        private double muLunch = 13.0; // 1:00 PM
        private double sigmaLunch = 0.6;

        // Evening Peak Parameters
        private double aEvening = 40;
        private double muEvening = 18.5; // 6:30 PM
        private double sigmaEvening = 1.2;

        // --- Noise Parameter ---
        // Controls the maximum magnitude of random noise added to the traffic value
        double noiseMagnitude = 0; // Adjust for more or less randomness

        /// <summary>
        /// Sets the magnitude of the random noise.
        /// </summary>
        /// <param name="magnitude">The new noise magnitude.</param>
        public void SetNoiseMagnitude(double magnitude)
        {
            if (magnitude < 0) magnitude = 0;
            noiseMagnitude = magnitude;
        }


        /// <summary>
        /// Calculates the value of a Gaussian function at time t.
        /// </summary>
        /// <param name="t">Time (e.g., in hours).</param>
        /// <param name="amplitude">The height of the peak.</param>
        /// <param name="mean">The center time of the peak.</param>
        /// <param name="stdDev">The standard deviation (width) of the peak.</param>
        /// <returns>The value of the Gaussian function.</returns>
        private double Gaussian(double t, double amplitude, double mean, double stdDev)
        {
            // Handle case where stdDev is zero to avoid division by zero (though unlikely for realistic peaks)
            if (stdDev == 0)
            {
                return (t == mean) ? amplitude : 0.0;
            }
            double exponent = -Math.Pow(t - mean, 2) / (2 * Math.Pow(stdDev, 2));
            return amplitude * Math.Exp(exponent);
        }

        /// <summary>
        /// Simulates the total foot traffic at a specific time point, including random noise.
        /// </summary>
        /// <param name="timeInHours">The time in hours from midnight (0-24).</param>
        /// <returns>The simulated traffic level with noise.</returns>
        public double SimulateTraffic(double timeInHours)
        {
            // Calculate the smooth traffic value from base and peaks
            double morningPeak = Gaussian(timeInHours, aMorning, muMorning, sigmaMorning);
            double lunchPeak = Gaussian(timeInHours, aLunch, muLunch, sigmaLunch);
            double eveningPeak = Gaussian(timeInHours, aEvening, muEvening, sigmaEvening);

            double smoothTraffic = baseTraffic + morningPeak + lunchPeak + eveningPeak;

            // --- Add Random Noise ---
            // _random.NextDouble() returns a double between 0.0 and 1.0
            // (_random.NextDouble() * 2.0 - 1.0) returns a double between -1.0 and 1.0
            // Multiply by noiseMagnitude to get noise between -noiseMagnitude and +noiseMagnitude
            double randomNoise = (_random.NextDouble() * 2.0 - 1.0) * noiseMagnitude;

            double noisyTraffic = smoothTraffic + randomNoise;

            // Traffic cannot be negative, so clamp the value at 0.0
            return Math.Max(0.0, noisyTraffic);
        }

        /// <summary>
        /// Generates a time series of simulated foot traffic values over a specified duration, including noise.
        /// </summary>
        /// <param name="startHour">The starting hour (e.g., 0 for midnight).</param>
        /// <param name="endHour">The ending hour (e.g., 24 for midnight the next day).</param>
        /// <param name="timeStepHours">The interval between time points (e.g., 0.1 for every 6 minutes).</param>
        /// <returns>A list of tuples containing each time point and its simulated traffic value with noise.</returns>
        public List<(double Time, double Value)> GenerateTimeSeries(double startHour, double endHour, double timeStepHours)
        {
            List<(double Time, double Value)> timeSeries = new List<(double Time, double Value)>();

            if (timeStepHours <= 0)
            {
                throw new ArgumentException("Time step must be positive.", nameof(timeStepHours));
            }

            // Loop through time points
            for (double t = startHour; t <= endHour + double.Epsilon; t += timeStepHours)
            {
                double currentTime = Math.Min(t, endHour);
                double traffic = SimulateTraffic(currentTime); // This now includes noise
                timeSeries.Add((currentTime, traffic));
            }

            return timeSeries;
        }


    }

    // Define the BlockchainEvent class

    // DataGenerator class using Bogus to create fake data
    public class DataGenerator
    {
        private readonly Faker<BlockChain.Card> _cardFaker;
        private readonly Faker _faker; // Use a base faker for general data generation

        public DataGenerator()
        {
            _faker = new Faker(); // Initialize base faker

            // Define rules for generating fake Card data
            _cardFaker = new Faker<BlockChain.Card>()
                .RuleFor(c => c.Id, f => Guid.NewGuid()) // Generate a new unique Guid for the card ID
                .RuleFor(c => c.Balance, f => (double)f.Finance.Amount(min: 0, max: 10)); // Generate a random decimal balance between 0 and 1000
        }


        // Method to generate multiple fake Cards
        public List<BlockChain.Card> GenerateMultipleCards(int count)
        {
            return _cardFaker.Generate(count);
        }

        // Method to generate a sequence of realistic events for a list of cards
        // Ensures TapOut follows TapIn and links hashes sequentially per card
        public List<BlockChain.BlockchainEvent> GenerateRealisticEvents(List<BlockChain.Card> cards, int eventsPerCard, Network network, List<BlockChain.BlockchainEvent> Events)
        {
            var allEvents = new List<BlockChain.BlockchainEvent>();
            var allStations = network.GetAllStationNames();
            int stationCount = allStations.Count;

            // Initialize card states
            var cardState = new Dictionary<Guid, bool>();          // Tracks tap-in status
            var cardEntryStation = new Dictionary<Guid, string>(); // Remembers where card tapped in
            var lastEventHash = new Dictionary<Guid, string>();    // Maintains blockchain hashes
            var lastEventTimestamp = new Dictionary<Guid, DateTime>();

            // 1. Create initial TopUp events for all cards
            if (Events.Count == 0) {
                foreach (var card in cards)
                {
                    var eventTime = DateTime.UtcNow.AddSeconds(allEvents.Count * 10);
                    var initialTopUp = new BlockChain.BlockchainEvent
                    {
                        Id = Guid.NewGuid(),
                        CardId = card.Id,
                        EventType = "TopUp",
                        FareCharged = card.Balance,
                        Station = null,
                        Timestamp = eventTime,
                        PrevHash = "Genesis"
                    };

                    initialTopUp.Hash = BlockChain.ComputeHash(initialTopUp);

                    // Initialize state tracking
                    cardState[card.Id] = false;
                    lastEventHash[card.Id] = initialTopUp.Hash;
                    lastEventTimestamp[card.Id] = initialTopUp.Timestamp;

                    allEvents.Add(initialTopUp);
                }
            }
            else
            {
                // Group by CardId for latest state tracking
                var latestEvents = Events
                    .GroupBy(e => e.CardId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(e => e.Timestamp).First()
                    );

                foreach (var card in cards)
                {
                    if (latestEvents.TryGetValue(card.Id, out var lastEvent))
                    {
                        lastEventHash[card.Id] = lastEvent.Hash;
                        lastEventTimestamp[card.Id] = lastEvent.Timestamp;

                        // Determine if card is currently tapped in
                        var tapIn = Events
                            .Where(e => e.CardId == card.Id && (e.EventType == "TapIn" || e.EventType == "TapOut"))
                            .OrderBy(e => e.Timestamp)
                            .ToList();

                        bool isTappedIn = false;
                        string lastEntryStation = null;

                        foreach (var evt in tapIn)
                        {
                            if (evt.EventType == "TapIn")
                            {
                                isTappedIn = true;
                                lastEntryStation = evt.Station;
                            }
                            else if (evt.EventType == "TapOut")
                            {
                                isTappedIn = false;
                                lastEntryStation = null;
                            }
                        }

                        cardState[card.Id] = isTappedIn;
                        if (isTappedIn && lastEntryStation != null)
                        {
                            cardEntryStation[card.Id] = lastEntryStation;
                        }
                    }
                    else
                    {
                        // No prior events for this card
                        cardState[card.Id] = false;
                        lastEventHash[card.Id] = "Genesis";
                        lastEventTimestamp[card.Id] = DateTime.UtcNow;
                    }
                }
            }


            // 2. Generate remaining events in random card order
            int totalEvents = cards.Count * eventsPerCard;
            Random rng = new Random();
            for (int i = 0; i < totalEvents; i++)
            {
                // Randomly select a card
                var card = _faker.PickRandom(cards);
                Guid cardId = card.Id;


                var possibleEvents = new List<string>();

                if ((card.Balance < 10 && rng.NextDouble() > 0.7) || card.Balance < 2)
                {
                    possibleEvents.Add("TopUp");
                }
                else
                {
                    // If balance is not > 10, add either TapOut or TapIn based on cardState
                    possibleEvents.Add(cardState[cardId] ? "TapOut" : "TapIn");
                }
                var eventTime = DateTime.Now;
                // Create new event
                var newEvent = new BlockChain.BlockchainEvent
                {
                    Id = Guid.NewGuid(),
                    CardId = cardId,
                    EventType = _faker.PickRandom(possibleEvents),
                    PrevHash = lastEventHash[cardId],
                    Timestamp = eventTime
                };
                newEvent.Hash = BlockChain.ComputeHash(newEvent);
                // Handle event-specific logic
                switch (newEvent.EventType)
                {
                    case "TopUp":
                        newEvent.FareCharged = (double)_faker.Finance.Amount(2, 20);
                        newEvent.Station = "None";
                        card.Balance += newEvent.FareCharged;
                        break;

                    case "TapIn":
                        newEvent.Station = allStations[_faker.Random.Int(0, stationCount - 1)];
                        cardEntryStation[cardId] = newEvent.Station;
                        cardState[cardId] = true;
                        newEvent.FareCharged = 0;
                        break;

                    case "TapOut":
                        var exitStation = allStations[_faker.Random.Int(0, stationCount - 1)];
                        var (_, distance) = network.FindShortestPath(cardEntryStation[cardId], exitStation);
                        newEvent.FareCharged = FareCalculator.CalculateFare(distance);
                        newEvent.Station = exitStation;
                        card.Balance -= newEvent.FareCharged;
                        cardState[cardId] = false;
                        break;
                }

                // Finalize event
                newEvent.Hash = BlockChain.ComputeHash(newEvent);
                lastEventHash[cardId] = newEvent.Hash;
                lastEventTimestamp[cardId] = newEvent.Timestamp;
                allEvents.Add(newEvent);
            }

            return allEvents.OrderBy(e => e.Timestamp).ToList();
        }

        // --- Example Usage ---
        public class FootTrafficGenerator
        {
            public static async Task Main()

                
            {
                var simulator = new FootTrafficSimulator();
                var network = MRTInitializer.Init();
                // --- Configuration ---
                double startHour = 0;      // Start of the day (midnight)
                double endHour = 24;       // End ofthe day (midnight next day)
                double timeStep = 0.003472222;
                double desiredNoiseMagnitude = 10; // Set the desired level of noise

                // Apply the desired noise magnitude
                simulator.SetNoiseMagnitude(desiredNoiseMagnitude);
                List<(double Time, double Value)> noisyResults = simulator.GenerateTimeSeries(startHour, endHour, timeStep);
                DateTime now = DateTime.Now;
                DateTime rounded = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    now.Hour,
                    (now.Minute / 5) * 5,
                    0
                );
                int minutesSinceMidnight = (rounded.Hour * 60) + rounded.Minute;
                int intervalCount = minutesSinceMidnight / 5;

                int traffic = (int)Math.Round(noisyResults[intervalCount].Value);
                var dataGenerator = new DataGenerator();

                // Generate a few fake cards
                int numberOfCardsToGenerate = 5;
                var cards = dataGenerator.GenerateMultipleCards(numberOfCardsToGenerate);
                foreach (var card in cards)
                {
                    // Explicitly call ToString() to ensure detailed output
                    Console.WriteLine(card.ToString());
                }
                List<BlockChain.BlockchainEvent> Events = new List<BlockChain.BlockchainEvent>() { };
                while (true)
                {
                    // Generate a sequence of realistic events for these cards
                    int eventsPerCardToGenerate = (int)Math.Round((decimal)traffic / numberOfCardsToGenerate);
                    Console.WriteLine(eventsPerCardToGenerate);
                    
                    var realisticEvents = dataGenerator.GenerateRealisticEvents(cards, eventsPerCardToGenerate, network, Events);

                    string JsonFormat = "{\"FootTraffic_" + DateTime.Now.ToString("h:mm:ss tt") + "\" : " + JsonSerializer.Serialize(realisticEvents) + "}";


                    string mySecretKey = "thisisasecretkeythatisatleast16byteslong"; //Keep this in ENV
                    string myIssuer = "Device01"; 
                    string myAudience = "StationTerminal";
                    Console.WriteLine("Writing to IoT Hub...");
                    JwtHelper jwtHelper = new JwtHelper(mySecretKey, myIssuer, myAudience);
                    //JWT Encode
                    string token = jwtHelper.CreateTokenFromJsonClaims(JsonFormat, 30);
                    await SimulatedDevice.Program.SendMsg(token);

                    Events = realisticEvents.ToList();
                    //System.Threading.Thread.Sleep(600000); // 10 Minutes
                    Thread.Sleep(10000); // 10 Seconds (FOR TESTING ONLY. DO NOT KILL THE IOTHUB)

                }

            }
         
        }
    }
}