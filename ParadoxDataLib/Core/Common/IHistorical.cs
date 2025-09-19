using System;
using System.Collections.Generic;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Core.Common
{
    public interface IHistorical
    {
        List<HistoricalEntry> HistoricalEntries { get; }
        void AddHistoricalEntry(HistoricalEntry entry);
        HistoricalEntry GetEntryAtDate(DateTime date);
        void ApplyHistoryUpToDate(DateTime date);
    }
}