﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace SPTarkov.Server.Core.Helpers
{
    [Injectable]
    public class CounterTrackerHelper
    {
        private Dictionary<MongoId, int> _maxCounts = new();
        private readonly Dictionary<MongoId, int> _trackedCounts = new();

        /// <summary>
        /// Add dictionary of keys and their matching limits to track
        /// </summary>
        /// <param name="maxCounts">Values to store</param>
        public void AddDataToTrack(Dictionary<MongoId, int> maxCounts)
        {
            _maxCounts = maxCounts;
        }

        /// <summary>
        /// Increment the counter for passed in key, get back value determining if max value passed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="countToIncrementBy"></param>
        /// <returns>True = above max count</returns>
        public bool IncrementCount(MongoId key, int countToIncrementBy = 1)
        {
            // Not tracked, skip
            if (!_maxCounts.Any() || !_maxCounts.ContainsKey(key))
            {
                return false;
            }

            _trackedCounts.TryAdd(key, 0);
            _trackedCounts[key] += countToIncrementBy;

            return _trackedCounts[key] > _maxCounts[key];
        }

        public void Clear()
        {
            _trackedCounts.Clear();
            _maxCounts.Clear();
        }
    }
}
