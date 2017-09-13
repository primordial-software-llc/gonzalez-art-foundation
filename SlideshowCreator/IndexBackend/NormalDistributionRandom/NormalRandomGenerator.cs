using System.Linq;

namespace IndexBackend.NormalDistributionRandom
{
    public class NormalRandomGenerator : IRandomGenerator
    {
        private int Min { get; set; }
        private int Max { get; set; }
        public double StandardDeviation { get; set; }
        public double Mean { get; set; }

        private double _maxToGenerateForProbability;
		private double _minToGenerateForProbability;
        
		private UniformRandomGenerator _rGen = new UniformRandomGenerator();
        
        private System.Collections.Generic.Dictionary<int, double> probabilities = new System.Collections.Generic.Dictionary<int, double>();

        public NormalRandomGenerator(int min, int max)
        {
            this.Min = min;
            this.Max = max;
            this.Mean = ((max - min) / 2) + min;

            int xMinusMyuSquaredSum = 0;
            for (int i = min; i < max; i++)
            {
                xMinusMyuSquaredSum += (int)System.Math.Pow(i - this.Mean, 2);
            }

            this.StandardDeviation = System.Math.Sqrt(xMinusMyuSquaredSum / (max - min + 1));
            this.StandardDeviation *= (0.5);

            for (int i = min; i < max; i++)
            {
                probabilities[i] = calculatePdf(i);
				if (i - 1 >= min)
				{
					probabilities[i] += probabilities[i - 1];
				}
			}

			this._minToGenerateForProbability = this.probabilities.Values.Min();
            this._maxToGenerateForProbability = this.probabilities.Values.Max();
        }

        /// <summary>
        /// Formula from Wikipedia: http://en.wikipedia.org/wiki/Normal_distribution
        /// </summary>
        public double calculatePdf(int x)
        {
            double negativeXMinusMyuSquared = -(x - this.Mean) * (x - this.Mean);
            double variance = StandardDeviation * StandardDeviation;
            double twoSigmaSquared = 2 * variance;
            double twoPiSigmaSquared = System.Math.PI * twoSigmaSquared;

            double eExponent = negativeXMinusMyuSquared / twoSigmaSquared;
            double top = System.Math.Pow(System.Math.E, eExponent);
            double bottom = System.Math.Sqrt(twoPiSigmaSquared);

            return top / bottom;
        }

        public int Next()
        {
            double pickedProb = this._rGen.NextDouble() * (this._maxToGenerateForProbability - this._minToGenerateForProbability);
			pickedProb -= this._minToGenerateForProbability;

            for (int i = this.Min; i < this.Max; i++)
            {
                if (pickedProb <= this.probabilities[i])
                {
                    return i;
                }
            }

            throw new System.InvalidOperationException("Internal error: your algorithm is flawed, young Jedi.");
        }
    }
}
