using System;
using System.IO;
using System.Text;

class GantryDataGenerator
{
    static void Min()
    {
        int totalCycles = 500;
        var rand = new Random();
        var sb = new StringBuilder();
        sb.AppendLine("Cycle,MotorAmp,OpenCloseMs,SuccessRate,ErrorCount,Temperature,Humidity,FaceConfidence,WillFail");

        for (int i = 0; i < totalCycles; i++)
        {
            double degradation = (double)i / totalCycles;
            double motorAmp = 1.0 + degradation * 0.3 + RandomNoise(rand, 0.02);
            double openClose = 2000 + degradation * 300 + RandomNoise(rand, 50);
            double successRate = 98 - degradation * 5 + RandomNoise(rand, 0.5);
            int errorCount = 1 + (int)(rand.NextDouble() * degradation * 10);
            double temp = 30 + RandomNoise(rand, 1);
            double humidity = 70 + RandomNoise(rand, 5);
            double faceConf = 0.9 - degradation * 0.3 + RandomNoise(rand, 0.01);
            int willFail = (degradation > 0.8 && rand.NextDouble() > 0.6) ? 1 : 0;

            sb.AppendLine($"{i},{motorAmp:F4},{openClose:F2},{successRate:F2},{errorCount},{temp:F2},{humidity:F2},{faceConf:F4},{willFail}");
        }

        File.WriteAllText("..\\..\\..\\gantry_sensor_data.csv", sb.ToString());
        Console.WriteLine("Data generated to gantry_sensor_data.csv");
    }

    static double RandomNoise(Random rand, double stdDev)
    {
        // Box-Muller transform
        double u1 = 1.0 - rand.NextDouble();
        double u2 = 1.0 - rand.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return stdDev * randStdNormal;
    }
    static void Man()
    {
        var lines = File.ReadAllLines("..\\..\\..\\gantry_sensor_data.csv").Skip(1);
        double thresholdHealth = 0.7;
        int currentCycle = 0;
        double predictedRUL = 0;

        foreach (var line in lines)
        {
            currentCycle++;
            var values = line.Split(',');
            double motorAmp = double.Parse(values[1]);
            double openClose = double.Parse(values[2]);
            double successRate = double.Parse(values[3]);
            double faceConf = double.Parse(values[7]);

            // Simple health score (normalize and weight)
            double health = 1.0
                - Normalize(motorAmp, 1.0, 1.3) * 0.3
                - Normalize(openClose, 2000, 2300) * 0.2
                - (1.0 - Normalize(successRate, 93, 98)) * 0.2
                - (1.0 - Normalize(faceConf, 0.6, 0.9)) * 0.3;

            if (health < thresholdHealth)
            {
                predictedRUL = 500 - currentCycle;
                Console.WriteLine($"Predicted RUL: ~{predictedRUL} cycles remaining (threshold crossed at cycle {currentCycle})");
                break;
            }
        }

        if (predictedRUL == 0)
            Console.WriteLine("System is healthy. No degradation threshold crossed.");
    }

    static double Normalize(double value, double min, double max)
    {
        return Math.Clamp((value - min) / (max - min), 0, 1);
    }
}
