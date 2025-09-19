using System;
using System.Collections.Generic;

namespace ParadoxDataLib.Utils
{
    public class BatchChangeEventArgs : EventArgs
    {
        public IReadOnlyCollection<FileChange> Changes { get; }
        public DateTime Timestamp { get; }
        public string? RootPath { get; }
        public int ChangeCount => Changes.Count;

        public BatchChangeEventArgs(IEnumerable<FileChange> changes, string? rootPath = null)
        {
            Changes = new List<FileChange>(changes ?? throw new ArgumentNullException(nameof(changes)));
            Timestamp = DateTime.UtcNow;
            RootPath = rootPath;
        }

        public BatchChangeEventArgs(IReadOnlyCollection<FileChange> changes, string? rootPath = null)
        {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            Timestamp = DateTime.UtcNow;
            RootPath = rootPath;
        }
    }
}