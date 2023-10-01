using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrossinformTest.Chimpoesh.Tripleter
{
    /// <summary>
    /// Поиск триплетов в тексте.
    /// </summary>
    public class TripletSearcher
    {
        /// <summary>
        /// Длина триплета.
        /// </summary>
        private const int TRIPLET_LENGTH = 3;

        /// <summary>
        /// Количество строк в чанке.
        /// </summary>
        private const int CHUNK_LENGTH = 1000;

        /// <summary>
        /// Символы-разделители слов в тексте.
        /// </summary>
        private readonly char[] Separators = new char[] { ' ', ',', '.', ':', '!', '?', '"', ';', '*', '(', ')', '»', '«', '…', '–', '-', '\'', '\n', '\t', '\r'};

        /// <summary>
        /// Получить частоту триплетов в тексте в один поток.
        /// </summary>
        /// <param name="sourceText">Исходный текст.</param>
        /// <param name="caseSensitive">Учитывать регистр.</param>
        /// <returns>Частоту триплетов в тексте.</returns>
        /// <exception cref="ArgumentNullException">Возникает при передаче пустого текста.</exception>
        public Dictionary<string, int> GetTripletsFrequencyOneThread(string sourceText, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentNullException(nameof(sourceText), "Передана пустая строка.");
            }

            return TrimTriplets(caseSensitive
                ? sourceText
                : sourceText.ToLower())
                .Where(s => s.All(ch => char.IsLetter(ch)))
                .GroupBy(s => s)
                .OrderByDescending(s => s.Count())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Получить частоту триплетов в тексте используя Parallel.For.
        /// </summary>
        /// <param name="sourceText">Исходный текст.</param>
        /// <param name="caseSensitive">Учитывать регистр.</param>
        /// <returns>Частоту триплетов в тексте.</returns>
        /// <exception cref="ArgumentNullException">Возникает при передаче пустого текста.</exception>
        public Dictionary<string, int> GetTripletsFrequencyParallelFor(string sourceText, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentNullException(nameof(sourceText), "Передана пустая строка.");
            }

            var result = new Dictionary<string, int>();

            Parallel.ForEach(Partitioner.Create(SplitIntoWords(caseSensitive
                ? sourceText
                : sourceText.ToLower())),
                word =>
                {
                    var triplets = TrimTriplets(word);

                    foreach (var triplet in triplets)
                    {
                        if (result.ContainsKey(triplet))
                        {
                            result[triplet]++;

                            continue;
                        }

                        result.Add(triplet, 1);
                    }
                });

            return result;
        }

        /// <summary>
        /// Получить частоту триплетов в тексте используя очередь ThreadPool.
        /// </summary>
        /// <param name="sourceText">Исходный текст.</param>
        /// <param name="caseSensitive">Учитывать регистр.</param>
        /// <returns>Частоту триплетов в тексте.</returns>
        /// <exception cref="ArgumentNullException">Возникает при передаче пустого текста.</exception>
        public Dictionary<string, int> GetTripletsFrequencyThreadPool(string sourceText, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentNullException(nameof(sourceText), "Передана пустая строка.");
            }

            var result = new Dictionary<string, int>();

            foreach (var word in SplitIntoWords(caseSensitive
                ? sourceText
                : sourceText.ToLower()))
            {
                ThreadPool.QueueUserWorkItem(callBack =>
                {
                    var triplets = TrimTriplets(word);

                    foreach (var triplet in triplets)
                    {
                        if (result.ContainsKey(triplet))
                        {
                            result[triplet]++;

                            continue;
                        }

                        result.Add(triplet, 1);
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Получить частоту триплетов в тексте используя Parallel.For.
        /// </summary>
        /// <param name="filePath">Путь к текстовому файлу.</param>
        /// <returns>Частоту триплетов в тексте.</returns>
        /// <exception cref="FileNotFoundException">Возникает, если файла по указанному пути не существует.</exception>
        public Dictionary<string, int> GetTripletsFrequencyParallelForV2(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            var result = new Dictionary<string, int>();

            Parallel.ForEach(Partitioner.Create(TrimTextFileToChunks(filePath)), chunk => 
            {
                var triplets = TrimTriplets(chunk);

                foreach (var triplet in triplets)
                {
                    if (result.ContainsKey(triplet))
                    {
                        result[triplet]++;

                        continue;
                    }

                    result.Add(triplet, 1);
                }
            });

            return result;
        }

        /// <summary>
        /// Разделить текст по указанному пути на чанки равной длины.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <returns>Коллекцию чанков строк из файла.</returns>
        private IEnumerable<string> TrimTextFileToChunks(string filePath)
        {
            var counter = 0;
            var chunk = new StringBuilder();

            foreach (var line in File.ReadLines(filePath))
            {
                counter++;
                chunk.Append(line);

                if (counter == CHUNK_LENGTH)
                {
                    counter = 0;

                    yield return chunk.ToString();

                    chunk.Clear();
                }
            }

            if (counter > 0)
            {
                yield return chunk.ToString();
            }
        }

        /// <summary>
        /// Разделить текст на слова.
        /// </summary>
        /// <param name="sourceText">Исходный текст.</param>
        /// <returns>Массив слов текста.</returns>
        public IEnumerable<string> SplitIntoWords(string sourceText)
        {
            return sourceText.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Разделить текст на триплеты.
        /// </summary>
        /// <param name="source">Исходный текст.</param>
        /// <returns>Массив триплетов текста.</returns>
        public IEnumerable<string> TrimTriplets(string sourceText)
        {
            for (var position = TRIPLET_LENGTH; position <= sourceText.Length; position++)
            {
                var triplet = sourceText.Substring(position - TRIPLET_LENGTH, TRIPLET_LENGTH);

                if (!triplet.All(Char.IsLetter))
                {
                    continue;
                }

                yield return triplet;
            }
        }
    }
}
