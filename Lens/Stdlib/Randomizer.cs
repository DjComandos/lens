﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Lens.Stdlib
{
	public static class Randomizer
	{
		public static readonly Random m_Random = new Random();

		public static double Random()
		{
			return m_Random.NextDouble();
		}

		public static int RandomMax(int max)
		{
			return m_Random.Next(max);
		}

		public static int RandomMinMax(int min, int max)
		{
			return m_Random.Next(min, max);
		}

		public static T RandomOf<T>(IList<T> src)
		{
			var max = src.Count - 1;
			return src[RandomMax(max)];
		}

		public static T RandomOfWeight<T>(IList<T> src, Func<T, double> weighter)
		{
			var rnd = m_Random.NextDouble();
			var weight = src.Sum(weighter);
			if (weight < 0.000001)
				throw new ArgumentException("src");

			var delta = 1.0/weight;
			var prob = 0.0;
			foreach (var curr in src)
			{
				prob += weighter(curr) * delta;
				if (rnd <= prob)
					return curr;
			}

			throw new ArgumentException("src");
		}
	}
}
