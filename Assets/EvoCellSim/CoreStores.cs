using System;
using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public abstract class FlatStore<T> where T : struct, IIdentifiable
    {
        private readonly List<T> records = new List<T>();
        private readonly Dictionary<int, int> indexById = new Dictionary<int, int>();

        public int Count => records.Count;

        public int Add(in T record)
        {
            if (indexById.ContainsKey(record.Id))
            {
                throw new InvalidOperationException($"Record with ID {record.Id} already exists.");
            }

            var index = records.Count;
            records.Add(record);
            indexById[record.Id] = index;
            return index;
        }

        public T Get(int index)
        {
            return records[index];
        }

        public T GetById(int id)
        {
            return records[indexById[id]];
        }

        public bool TryGetById(int id, out T record)
        {
            if (indexById.TryGetValue(id, out var index))
            {
                record = records[index];
                return true;
            }

            record = default;
            return false;
        }

        public void Set(int index, in T record)
        {
            var existing = records[index];
            if (existing.Id != record.Id)
            {
                throw new InvalidOperationException("Cannot change record ID when setting by index.");
            }

            records[index] = record;
        }

        public void SetById(int id, in T record)
        {
            if (!indexById.TryGetValue(id, out var index))
            {
                throw new KeyNotFoundException($"Record with ID {id} does not exist.");
            }

            if (record.Id != id)
            {
                throw new InvalidOperationException("Record ID must match the lookup ID when updating.");
            }

            records[index] = record;
        }

        public bool ContainsId(int id)
        {
            return indexById.ContainsKey(id);
        }

        public void Clear()
        {
            records.Clear();
            indexById.Clear();
        }
    }

    public sealed class CellStore : FlatStore<CellRecord> { }
    public sealed class GenomeStore : FlatStore<GenomeRecord> { }
    public sealed class ModuleStore : FlatStore<ModuleRecord> { }
    public sealed class BondStore : FlatStore<BondRecord> { }
    public sealed class ClusterStore : FlatStore<ClusterRecord> { }
    public sealed class FieldStore : FlatStore<FieldRecord> { }
    public sealed class SignalStore : FlatStore<SignalRecord> { }
    public sealed class IntentQueue : FlatStore<IntentRecord> { }
}