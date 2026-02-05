using System.Collections.Concurrent;

namespace Sunfire.Shared;

public abstract class IdIndexedCache<TCreationData, TDataObject, TReturnInfo> : IdIndexedCache<TCreationData, object, TDataObject, TReturnInfo>
    where TCreationData : notnull
{
    protected abstract TDataObject CreateObject(TCreationData creationData);
    protected override TDataObject CreateObject(TCreationData creationData, object helperData) =>
        CreateObject(creationData);

    protected abstract TDataObject Update(TDataObject dataObject, TCreationData creationData);
    protected override TDataObject Update(TDataObject dataObject, TCreationData creationData, object helperData) =>
        Update(dataObject, creationData);

    public void AddOrUpdate(TCreationData creationData) =>
        AddOrUpdate(creationData);
    public TReturnInfo GetOrAdd(TCreationData creationData) =>
        GetOrAdd(creationData, new());
}

public abstract class IdIndexedCache<TCreationData, THelperData, TDataObject, TReturnInfo>
    where TCreationData : notnull
{
    private static readonly List<TDataObject> data = [];
    private static readonly ConcurrentDictionary<TCreationData, int> index = [];

    protected abstract TDataObject CreateObject(TCreationData creationData, THelperData helperData);
    protected abstract TReturnInfo CreateInfo(int id, TDataObject dataOjbect);

    protected abstract TDataObject Update(TDataObject dataObject, TCreationData creationData, THelperData helperData);

    public void AddOrUpdate(TCreationData creationData, THelperData helperData)
    {
        if(index.TryGetValue(creationData, out var existingId))
        {
            data[existingId] = Update(data[existingId], creationData, helperData);
        }
        else
            Add(creationData, helperData);
    }

    public TReturnInfo GetOrAdd(TCreationData creationData, THelperData helperData)
    {
        if(index.TryGetValue(creationData, out var existingId))
            return CreateInfo(existingId, data[existingId]);

        var (id, dataObject) = Add(creationData, helperData);
        return CreateInfo(id, dataObject);
    }

    private (int id, TDataObject dataObject) Add(TCreationData creationData, THelperData helperData)
    {
        var newId = data.Count;
        var newData = CreateObject(creationData, helperData);
        data.Add(newData);
        index[creationData] = newId;

        return (newId, newData);
    }

    public TDataObject? TryGet(int index) =>
        index > 0 && index < data.Count
            ? data[index]
            : default;

    public TDataObject Get(int index) =>
        data[index];
}
