using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CrossinformTest.Chimpoesh.Tripleter;

namespace CrossinformTest.Chimpoesh.TripleterDemo
{
    public class Program
    {
        /// <summary>
        /// Путь к файлу маленького размера.
        /// </summary>
        private static readonly string filePath = "D:\\текст1.txt";

        /// <summary>
        /// Путь к файлу большого размера.
        /// </summary>
        private static readonly string largeTextFilePath = "D:\\текст.txt";

        static void Main(string[] args)
        {
            RunDemoV2();
        }

        /// <summary>
        /// Запуск демонстрации.
        /// </summary>
        private static void RunDemo()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            var source = File.ReadAllText(filePath);
            var stopwatch = new Stopwatch();
            var tripleter = new TripletSearcher();

            stopwatch.Start();
            var triplets = tripleter
                .GetTripletsFrequencyOneThread(source, false)
                .OrderByDescending(v => v.Value)
                .Take(10);
            var count = triplets.Count();
            stopwatch.Stop();
            Console.WriteLine("Один поток");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс\n");


            stopwatch.Restart();
            triplets = tripleter
                .GetTripletsFrequencyParallelFor(source, false)
                .OrderByDescending(v => v.Value)
                .Take(10);
            count = triplets.Count();
            stopwatch.Stop();
            Console.WriteLine("Parallel");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс\n");


            stopwatch.Restart();
            triplets = tripleter
                .GetTripletsFrequencyThreadPool(source, false)
                .OrderByDescending(v => v.Value)
                .Take(10);
            count = triplets.Count();
            stopwatch.Stop();
            Console.WriteLine("ThreadPool");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds}  мс\n");

            foreach (var triplet in triplets)
            {
                Console.WriteLine($"{triplet.Key} - {triplet.Value}");
            }
        }

        /// <summary>
        /// Запускает демонстрацию обработки большого файла.
        /// </summary>
        private static void RunDemoV2()
        {
            if (!File.Exists(largeTextFilePath))
            {
                throw new FileNotFoundException(largeTextFilePath);
            }

            var stopwatch = new Stopwatch();
            var tripleter = new TripletSearcher();

            stopwatch.Start();
            var triplets = tripleter
                .GetTripletsFrequencyParallelForV2(largeTextFilePath)
                .OrderByDescending(v => v.Value)
                .Take(10);
            var count = triplets.Count();
            stopwatch.Stop();
            Console.WriteLine("Параллельная обработка большого файла");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс\n");

            stopwatch.Restart();
            triplets = tripleter
                .GetTripletsFrequencyParallelForV2(filePath)
                .OrderByDescending(v => v.Value)
                .Take(10);
            count = triplets.Count();
            stopwatch.Stop();
            Console.WriteLine("Обработка файла из прошлых тестов методом Parallel");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds}  мс\n");
        }
    }
}
