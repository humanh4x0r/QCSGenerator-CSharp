using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace RANDOM_LIB_VULN
{
    public class QCSGenerator
    {
        private Complex[] state;
        private static readonly string allCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly int lengthOfAllCharacters = allCharacters.Length;

        public QCSGenerator(int? seed = null)
        {
            if (!seed.HasValue)
            {
                seed = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % (1L << 32));
            }
            seed = Math.Abs(seed.Value) % lengthOfAllCharacters; // Ensure seed is within bounds
            state = new Complex[lengthOfAllCharacters];
            state[seed.Value] = new Complex(1, 0);
        }

        public int GenerateRandom()
        {
            var hMatrix = new Complex[,]
            {
                { 1 / Math.Sqrt(2), 1 / Math.Sqrt(2) },
                { 1 / Math.Sqrt(2), -1 / Math.Sqrt(2) }
            };

            for (int i = 0; i < state.Length; i += 2)
            {
                if (i == state.Length / 2) continue;
                var slice = new[] { state[i], state[i + 1] };
                var transformed = ApplyMatrix(hMatrix, slice);
                state[i] = transformed[0];
                state[i + 1] = transformed[1];
            }

            var measurementProbs = state.Select(c => c.Magnitude * c.Magnitude).ToArray();
            int randValue = SampleFromDistribution(measurementProbs);

            for (int i = 0; i < state.Length; i += 2)
            {
                if (i == state.Length / 2) continue;
                var slice = new[] { state[i], state[i + 1] };
                var transformed = ApplyMatrix(ConjugateTranspose(hMatrix), slice);
                state[i] = transformed[0];
                state[i + 1] = transformed[1];
            }

            return randValue;
        }

        public void WriteStringToFile(string str)
        {
            File.AppendAllText("strings.txt", str + Environment.NewLine);
        }

        public string GenerateString()
        {
            var random = new Random();
            int length = random.Next(25, 31); // Genera una lunghezza casuale tra 25 e 30 inclusi
            var randomCharacters = new char[length];

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(allCharacters.Length);
                randomCharacters[i] = allCharacters[index];
            }

            return new string(randomCharacters);
        }


        public void GenerateStrings(int count = 50000)
        {
            for (int i = 0; i < count; i++)
            {
                var generatedString = GenerateString();
                WriteStringToFile(generatedString);
            }
        }

        private Complex[] ApplyMatrix(Complex[,] matrix, Complex[] vector)
        {
            var result = new Complex[vector.Length];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                result[i] = 0;
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    result[i] += matrix[i, j] * vector[j];
                }
            }
            return result;
        }

        private Complex[,] ConjugateTranspose(Complex[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var result = new Complex[cols, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j, i] = Complex.Conjugate(matrix[i, j]);
                }
            }
            return result;
        }

        private int SampleFromDistribution(double[] probabilities)
        {
            double sum = probabilities.Sum();
            double rnd = new Random().NextDouble() * sum;
            double cumulative = 0.0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (rnd < cumulative)
                {
                    return i;
                }
            }
            return probabilities.Length - 1;
        }
    }
}
